using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

string userId = Environment.GetEnvironmentVariable("USER_ID") ?? "2178983",
    profileSectionPage = Environment.GetEnvironmentVariable("PROFILE_SECTION_PAGE") ?? "1",
    videosPage = Environment.GetEnvironmentVariable("VIDEOS_PAGE") ?? "1";

CancellationTokenSource cts = new();
Console.CancelKeyPress += (s, e) =>
{
    cts.Cancel();
    e.Cancel = true;
};
Console.OutputEncoding = Encoding.UTF8;
using HttpClient apiHttp = new()
{
    BaseAddress = new("https://api.vimeo.com")
};
async Task NextTokenAsync(CancellationToken cancellationToken = default)
{
    using HttpClient tokenHttp = new()
    {
        BaseAddress = new("https://vimeo.com"),
        DefaultRequestHeaders =
        {
            { "X-Requested-With", "XMLHttpRequest" }
        }
    };
    if (await tokenHttp.GetFromJsonAsync("/_next/jwt", AppJsonContext.Default.JWT, cts.Token) is not { Token: { } token })
    {
        throw new InvalidOperationException("Failed to fetch a JWT.")
        {
            HelpLink = "https://vimeo.com/_next/jwt"
        };
    }
    apiHttp.DefaultRequestHeaders.Authorization = new("jwt", token);
}
await NextTokenAsync(cts.Token);
async Task<T?> GetAsync<T>(string url, JsonTypeInfo<T> type, CancellationToken cancellationToken = default)
{
    for (; ; )
    {
        using var response = await apiHttp.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                await NextTokenAsync(cts.Token);
                break;
            case HttpStatusCode.TooManyRequests:
                await Task.Delay(5000, cancellationToken);
                break;
            default:
                return await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync(type, cancellationToken);
        }
    }
}
using HttpClient playerHttp = new()
{
    BaseAddress = new("https://player.vimeo.com")
};
for (var nextProfileSection = $"/users/user{userId}/profile_sections?fields=uri&page={profileSectionPage}&per_page=100"; nextProfileSection != null;)
{
    Console.WriteLine($"Current page: {nextProfileSection}");
    var profileSections = await GetAsync(nextProfileSection, AppJsonContext.Default.PagedDataProfileSection, cts.Token) ??
        throw new InvalidOperationException("Failed to fetch a page of profile sections.")
        {
            HelpLink = nextProfileSection
        };
    foreach (var p in profileSections.Data)
    {
        for (var nextVideos = $"{p.Uri}/videos?fields=clip.name%2Cclip.config_url&page={videosPage}&per_page=100"; nextVideos != null;)
        {
            Console.WriteLine($"Current page: {nextVideos}");
            var videos = await GetAsync(nextVideos, AppJsonContext.Default.PagedDataClip_, cts.Token) ??
                throw new InvalidOperationException("Failed to fetch a page of videos.")
                {
                    HelpLink = nextVideos
                };
            foreach (var v in videos.Data)
            {
                var id = Patterns.VideoId().Match(v.Clip.ConfigUrl).Value;
                var title = v.Clip.Name.Replace(':', '：').Replace('/', '／');
                var filename = $"{id}={title}";
                if (File.Exists(filename + ".m4a"))
                {
                    Console.WriteLine($"Skip {filename}");
                    continue;
                }
                var config = await GetAsync(v.Clip.ConfigUrl, AppJsonContext.Default.Config, cts.Token) ??
                    throw new InvalidOperationException("Failed to fetch config for a video.")
                    {
                        HelpLink = v.Clip.ConfigUrl
                    };
                if (!(config is { Request.Files.Dash: { Cdns: { } cdns, DefaultCdn: { } defaultCdn } } &&
                    cdns.TryGetValue(defaultCdn, out var cdn) && cdn.Url is { } masterUrl))
                {
                    throw new InvalidDataException("No default DASH CDN for a video.")
                    {
                        HelpLink = v.Clip.ConfigUrl
                    };
                }
                await Task.WhenAll(!File.Exists(filename + ".vtt") &&
                    config is { Request.TextTracks: { } textTracks } && textTracks.FirstOrDefault(t => t is
                    {
                        Lang: "zh-TW",
                        Kind: "captions"
                    }) is { Url: { } vtt }
                    ? Task.Run(async () =>
                    {
                        var path = $"{filename}.vtt";
                        var tmp = path + '~';
                        {
                            await using var file = File.Create(tmp);
                            using var stream = await playerHttp.GetStreamAsync(vtt, cts.Token);
                            await stream.CopyToAsync(file, CancellationToken.None);
                        }
                        File.Delete(path);
                        File.Move(tmp, path);
                    }, cts.Token)
                    : Task.CompletedTask,
                    Task.Run(async () =>
                    {
                        var master = await playerHttp.GetFromJsonAsync(masterUrl, AppJsonContext.Default.DashMaster, cts.Token) ??
                            throw new InvalidDataException("No DASH master for a video.")
                            {
                                HelpLink = masterUrl
                            };
                        var audio = master.Audio.MaxBy(a => a.AvgBitrate) ??
                            throw new InvalidDataException("No audio for a video.")
                            {
                                HelpLink = masterUrl
                            };
                        var path = $"{filename}.m4a";
                        var tmp = $"{filename}.m4s";
                        {
                            await using var file = File.Create(tmp);
                            await file.WriteAsync(audio.InitSegment, CancellationToken.None);
                            Uri baseUrl = new(new(new(masterUrl), master.BaseUrl), audio.BaseUrl);
                            foreach (var download in audio.Segments.Select(s => playerHttp.GetByteArrayAsync(new Uri(baseUrl, s.Url), cts.Token)).ToList())
                            {
                                await file.WriteAsync(await download, CancellationToken.None);
                            }
                        }
                        try
                        {
                            using Process ffmpeg = new()
                            {
                                StartInfo = new("ffmpeg", ["-y", "-i", tmp, "-vn", "-c", "copy", path])
                                {
                                    UseShellExecute = false,
                                    RedirectStandardError = true,
                                    StandardErrorEncoding = Encoding.UTF8
                                },
                                EnableRaisingEvents = true
                            };
                            StringBuilder sb = new();
                            ffmpeg.ErrorDataReceived += (s, e) => sb.Append(e.Data);
                            _ = ffmpeg.Start();
                            await ffmpeg.WaitForExitAsync(CancellationToken.None);
                            if (ffmpeg.ExitCode == 0)
                            {
                                File.Delete(tmp);
                            }
                            else
                            {
                                throw new InvalidOperationException(sb.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Error extracting audio from {tmp}{Environment.NewLine}{ex}");
                        }
                    }, cts.Token));
            }
            nextVideos = videos.Paging.Next;
        }
    }
    nextProfileSection = profileSections.Paging.Next;
}

sealed record JWT(string Token);
sealed record Paging(string First, string Last, string? Next, string? Previous);
sealed record ProfileSection(string Uri);
sealed record Clip(string Name, string ConfigUrl);
sealed record Clip_(Clip Clip);
sealed record PagedData<T>(ICollection<T> Data, int Page, Paging Paging, int PerPage, int Total);
sealed partial record DashCdn(string AvcUrl, string Origin, string Url);
sealed record DashProfile(string Profile, Guid Id, float Fps, string Quality);
sealed record Dash(Dictionary<string, DashCdn> Cdns, string DefaultCdn, bool SeparateAv, ICollection<DashProfile> Streams, ICollection<DashProfile> StreamsAvc);
sealed record Files(Dash Dash);
sealed record TextTrack(string Lang, string Url, string Kind);
sealed record Request(Files Files, ICollection<TextTrack>? TextTracks = null);
sealed record Video(int Id, string Title);
sealed record Config(Request Request, Video Video);
sealed record DashSegment(double Start, double End, string Url, int Size);
sealed record DashVideoStream(string Id, string AvgId, string BaseUrl, string Format, string MimeType, string Codecs, int Bitrate, int AvgBitrate, double Duration, double Framerate, int Width, int Height, int MaxSegmentDuration, ReadOnlyMemory<byte> InitSegment, IList<DashSegment> Segments);
sealed record DashAudioStream(string Id, string AvgId, string BaseUrl, string Format, string MimeType, string Codecs, int Bitrate, int AvgBitrate, double Duration, int Channels, int MaxSegmentDuration, ReadOnlyMemory<byte> InitSegment, IList<DashSegment> Segments, bool AudioPrimary);
sealed record DashMaster(Guid ClipId, string BaseUrl, ICollection<DashVideoStream> Video, ICollection<DashAudioStream> Audio);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(JWT))]
[JsonSerializable(typeof(PagedData<ProfileSection>))]
[JsonSerializable(typeof(PagedData<Clip_>))]
[JsonSerializable(typeof(Config))]
[JsonSerializable(typeof(DashMaster))]
sealed partial class AppJsonContext : JsonSerializerContext { }

static partial class Patterns
{
    [GeneratedRegex(@"(?<=/videos?/)\d+(?=/.+$)?")]
    internal static partial Regex VideoId();
}