using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
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
using var apiHttp = new HttpClient()
{
    BaseAddress = new("https://api.vimeo.com")
};
async Task NextTokenAsync(CancellationToken cancellationToken = default)
{
    using var tokenHttp = new HttpClient()
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
async IAsyncEnumerable<KeyValuePair<string, string>> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken)
{
    for (var nextProfileSection = $"/users/user{userId}/profile_sections?fields=uri&page={profileSectionPage}&per_page=100"; nextProfileSection != null;)
    {
        Console.WriteLine($"Current page: {nextProfileSection}");
        var profileSections = await GetAsync(nextProfileSection, AppJsonContext.Default.PagedDataProfileSection, cancellationToken) ??
            throw new InvalidOperationException("Failed to fetch a page of profile sections.")
            {
                HelpLink = nextProfileSection
            };
        foreach (var p in profileSections.Data)
        {
            for (var nextVideos = $"{p.Uri}/videos?fields=clip.name%2Cclip.config_url&page={videosPage}&per_page=100"; nextVideos != null;)
            {
                Console.WriteLine($"Current page: {nextVideos}");
                var videos = await GetAsync(nextVideos, AppJsonContext.Default.PagedDataClip_, cancellationToken) ??
                    throw new InvalidOperationException("Failed to fetch a page of videos.")
                    {
                        HelpLink = nextVideos
                    };
                foreach (var v in videos.Data)
                {
                    var id = Patterns.VideoId().Match(v.Clip.ConfigUrl).Value;
                    var title = v.Clip.Name.Replace(':', '：').Replace('/', '／');
                    var filename = $"{id}={title}";
                    if (File.Exists(filename + ".vtt") || File.Exists(filename) || File.Exists(filename + ".m4a"))
                    {
                        Console.WriteLine($"Skip {filename}");
                        continue;
                    }
                    var config = await GetAsync(v.Clip.ConfigUrl, AppJsonContext.Default.Config, cancellationToken) ??
                        throw new InvalidOperationException("Failed to fetch config for a video.")
                        {
                            HelpLink = v.Clip.ConfigUrl
                        };
                    var profile = config.Request.Files.Progressive.MaxBy(p => p.Profile) ??
                        throw new InvalidDataException("No progressive profile for a video.")
                        {
                            HelpLink = v.Clip.ConfigUrl
                        };
                    if (config is { Request.TextTracks: { } textTracks } && textTracks.FirstOrDefault(t => t is
                        {
                            Lang: "zh-TW",
                            Kind: "captions"
                        }) is { Url: { } vtt })
                    {
                        yield return new($"{filename}.vtt", vtt);
                    }
                    yield return new(filename, profile.Url);
                }
                nextVideos = videos.Paging.Next;
            }
        }
        nextProfileSection = profileSections.Paging.Next;
    }
}
using var playerHttp = new HttpClient()
{
    BaseAddress = new("https://player.vimeo.com")
};
await Parallel.ForEachAsync(EnumerateAsync(cts.Token), new ParallelOptions
{
    CancellationToken = cts.Token,
    MaxDegreeOfParallelism = Math.Min(8, Environment.ProcessorCount) // TODO: estimate base on available memory and bandwidth
}, async (p, cancellationToken) =>
{
    if (File.Exists(p.Key + ".m4a"))
    {
        Console.WriteLine($"Found {p.Key}.m4a");
    }
    else if (File.Exists(p.Key))
    {
        Console.WriteLine($"Found {p.Key}");
    }
    else
    {
        Console.WriteLine($"Downloading {p.Key} from {p.Value}");
        var t = '~' + p.Key;
        bool isVideo = false;
        try
        {
            {
                await using var file = File.Create(t);
                using var response = await playerHttp.SendAsync(new(HttpMethod.Get, p.Value), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                await using var stream = await response.EnsureSuccessStatusCode().Content.ReadAsStreamAsync(cancellationToken);
                await stream.CopyToAsync(file, CancellationToken.None);
                isVideo = response.Content.Headers.ContentType is { MediaType: "video/mp4" };
            }
            File.Move(t, p.Key);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error downloading {p.Key}{Environment.NewLine}{ex}");
            File.Delete(t);
        }
        if (isVideo)
        {
            try
            {
                t = p.Key + ".m4a";
                using var ffmpeg = Process.Start(new ProcessStartInfo("ffmpeg", ["-i", p.Key, "-vn", "-c", "copy", t])
                {
                    UseShellExecute = false
                });
                await ffmpeg!.WaitForExitAsync(CancellationToken.None);
                if (ffmpeg.ExitCode == 0)
                {

                    File.Delete(p.Key);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error removing video from {p.Key}{Environment.NewLine}{ex}");
                File.Delete(t);
            }
        }
        Console.WriteLine("Complete");
    }
});

sealed record class JWT(string Token);
sealed record class Paging(string First, string Last, string? Next, string? Previous);
sealed record class ProfileSection(string Uri);
sealed record class Clip(string Name, string ConfigUrl);
sealed record class Clip_(Clip Clip);
sealed record class PagedData<T>(ICollection<T> Data, int Page, Paging Paging, int PerPage, int Total);
sealed record class Progressive(string Profile, string Url);
sealed record class Files(ICollection<Progressive> Progressive);
sealed record class TextTrack(string Lang, string Url, string Kind);
sealed record class Request(Files Files, ICollection<TextTrack>? TextTracks = null);
sealed record class Video(int Id, string Title);
sealed record class Config(Request Request, Video Video);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(JWT))]
[JsonSerializable(typeof(PagedData<ProfileSection>))]
[JsonSerializable(typeof(PagedData<Clip_>))]
[JsonSerializable(typeof(Config))]
sealed partial class AppJsonContext : JsonSerializerContext { }

static partial class Patterns
{
    [GeneratedRegex(@"(?<=/videos?/)\d+(?=/.+$)?")]
    internal static partial Regex VideoId();
}