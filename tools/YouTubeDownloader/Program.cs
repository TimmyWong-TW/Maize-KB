using Jurassic;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using YouTube;

const int ChunkSize = 10 << 20;

string playlistId = Environment.GetEnvironmentVariable("PLAYLIST_ID") ??
#if DEBUG
    "PLe-YK1dmFUsLP91e4OhcDqnqK_0smrmaL";
#else
    throw new InvalidOperationException("No playlist ID.");
#endif
string? basePath = Environment.GetEnvironmentVariable("OUT_DIR");

CancellationTokenSource cts = new();
Console.CancelKeyPress += (s, e) =>
{
    cts.Cancel();
    e.Cancel = true;
};
Console.OutputEncoding = Encoding.UTF8;
using HttpClient http = new()
{
    BaseAddress = new("https://www.youtube.com")
};
#region Signature
Func<string, string> sign;
{
    ScriptEngine scriptEngine = new();
    scriptEngine.Execute("g={};" +
        "navigator={};" +
        "location={hostname:'www.youtube.com'};" +
        "XMLHttpRequest={prototype:{fetch:function(){}}};" +
        "document={referrer:''};"
    );
    var js = await http.GetStringAsync($"/s/player/{Patterns.Player().Match(await http.GetStringAsync("/iframe_api", cts.Token)).Value}/player_ias.vflset/en_US/base.js");
    var scope = js[js.IndexOf('{', js.IndexOf(';') + 1)..js.LastIndexOf(')', js.LastIndexOf("_yt_player") - 1)];
    var decipher = Patterns.SDecipher().Match(scope).Value;
    var ema = Patterns.NDecipher().Match(scope).Value;
    scriptEngine.Execute(scope);
    sign = cipher =>
    {
        var p = HttpUtility.ParseQueryString(cipher);
        UriBuilder u = new(p.GetValues("url")?.LastOrDefault() ?? throw new ArgumentException("No URL in cipher.", nameof(cipher)));
        var q = HttpUtility.ParseQueryString(u.Query);
        if (q.GetValues("n")?.LastOrDefault() is { } n)
        {
            q.Set("n", (string)scriptEngine.Evaluate($"{ema}('{n}')"));
        }
        q.Set(p.GetValues("sp")?.LastOrDefault() ?? throw new ArgumentException("No signature parameter in cipher."),
            (string)scriptEngine.Evaluate($"{decipher}('{p.GetValues("s")?.LastOrDefault() ?? throw new ArgumentException("No signature in cipher.")}')"));
        u.Query = q.ToString();
        return u.ToString();
    };
}
#endregion

await foreach (var videoId in EnumeratePlaylistAsync(http, playlistId, cts.Token))
{
    await DownloadAsync(http, videoId, sign, basePath, cts.Token);
}

static async IAsyncEnumerable<string> EnumeratePlaylistAsync(HttpClient http, string playlistId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var html = await http.GetStringAsync($"/playlist?list={playlistId}", cancellationToken);
    if (JsonSerializer.Deserialize(Patterns.YtInitialData().Match(
            await http.GetStringAsync($"/playlist?list={playlistId}", cancellationToken)
        ).Value, AppJsonContext.Default.PlaylistResponse) is not
        {
            ResponseContext.ServiceTrackingParams: { Count: > 0 } stp,
            Contents.TwoColumnBrowseResultsRenderer.Tabs: [
                {
                    TabRenderer.Content.SectionListRenderer.Contents: [
                    { ItemSectionRenderer.Contents: { } items },
                    {
                        ContinuationItemRenderer.ContinuationEndpoint:
                        {
                            CommandMetadata.WebCommandMetadata.ApiUrl: { },
                            ContinuationCommand.Token: { }
                        } continuation
                    }]
                }
            ]
        })
    {
        throw new InvalidDataException("Failed to fetch playlist initially.");
    }
    string? clientName = null, clientVersion = null;
    foreach (var p in stp.First(p => p.Service == "CSI").Params)
    {
        switch (p.Key)
        {
            case "c":
                clientName = p.Value;
                break;
            case "cver":
                clientVersion = p.Value;
                break;
        }
    }
    if (clientName is null || clientVersion is null)
    {
        throw new InvalidDataException("Missing params in CSI.");
    }
    Stack<ContinuationEndpoint> continuations = new();
    continuations.Push(continuation);
    for (var contents = items.SelectMany(i => i.PlaylistVideoListRenderer.Contents); ;)
    {
        if (contents is not null)
        {
            foreach (var c in contents)
            {
                if (c is { PlaylistVideoRenderer.VideoId: { } v })
                {
                    yield return v;
                }
                else if (c is
                {
                    ContinuationItemRenderer.ContinuationEndpoint:
                    {
                        CommandMetadata.WebCommandMetadata.ApiUrl: { },
                        ContinuationCommand.Token: { }
                    } e
                })
                {
                    continuations.Push(e);
                }
            }
        }
        if (!continuations.TryPop(out continuation))
        {
            break;
        }
        using var response = await http.PostAsJsonAsync(continuation.CommandMetadata.WebCommandMetadata.ApiUrl,
            new(new(new(clientName, clientVersion)), continuation.ContinuationCommand.Token),
            AppJsonContext.Default.PlaylistRequest, cancellationToken);
        var playlist = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync(AppJsonContext.Default.PlaylistResponse, cancellationToken) ??
            throw new InvalidOperationException("Failed to continue fetching playlist.")
            {
                HelpLink = continuation.CommandMetadata.WebCommandMetadata.ApiUrl
            };
        contents = playlist.OnResponseReceivedActions?.SelectMany(a => a.AppendContinuationItemsAction.ContinuationItems);
    }
}

static async Task DownloadAsync(HttpClient http, string videoId, Func<string, string> sign, string? basePath = null, CancellationToken cancellationToken = default)
{
    using var response = await http.PostAsJsonAsync("/youtubei/v1/player?prettyPrint=false", new PlayerRequest(videoId), AppJsonContext.Default.PlayerRequest, cancellationToken);
    using var content = response.EnsureSuccessStatusCode().Content;
    var player = await content.ReadFromJsonAsync(AppJsonContext.Default.PlayerResponse, cancellationToken: cancellationToken);
    if (player is not { VideoDetails.Title: { } title })
    {
        throw new InvalidOperationException("Failed to fetch video details.");
    }
    title = title.Replace(':', '：').Replace('/', '／');
    var filename = $"{videoId}={title}";
    await Task.WhenAll(
        player is { Captions.PlayerCaptionsTracklistRenderer.CaptionTracks: { Count: > 0 } tracks } &&
        tracks.FirstOrDefault(t => t.LanguageCode == "zh-TW") is { BaseUrl: { } trackUrl }
        ? Task.Run(async () =>
        {
            string tsv;
            {
                await using var stream = await http.GetStreamAsync(trackUrl, cancellationToken);
                tsv = Transcript.Deserialize(stream)?.ToTSV() ??
                    throw new InvalidDataException("No transcript for a caption track.")
                    {
                        HelpLink = trackUrl
                    };
            }
            var path = Path.Join(basePath, $"{filename}.tsv");
            var t = path + '~';
            await File.WriteAllTextAsync(t, tsv, cancellationToken);
            File.Delete(path);
            File.Move(t, path);
        }, cancellationToken)
        : Task.CompletedTask,
        Task.Run(async () =>
        {
            if (player is not { StreamingData.AdaptiveFormats: { Count: > 0 } formats })
            {
                throw new InvalidDataException("No audio for a video.");
            }
            var (format, type, _) = formats
                .Where(f => f is { MimeType: { } m } && m.StartsWith("audio/", StringComparison.Ordinal))
                .Select(f =>
                {
                    var a = f.MimeType[6..f.MimeType.IndexOf(';', 7)];
                    return (f, a, i: a switch
                    {
                        "mp4" => 0,
                        "webm" => 1,
                        _ => -1
                    });
                })
                .Aggregate((max, current) =>
                    current.f.AudioQuality > max.f.AudioQuality ||
                    current.f.AudioQuality == max.f.AudioQuality && (
                        current.i > max.i ||
                        current.i == max.i && current.f.Bitrate > max.f.Bitrate
                    ) ? current : max
                );
            var url = format.Url ?? sign(format.SignatureCipher!);
            var path = Path.Join(basePath, $"{filename}.{type switch
            {
                "webm" => "weba",
                "mp4" => "m4a",
                _ => type
            }}");
            var t = path + '~';
            {
                await using var file = File.Create(t);
                var size = format.ContentLength;
                foreach (var task in Enumerable.Range(0, Math.DivRem(size, ChunkSize, out var r) is { } chunks && r != 0 ? chunks + 1 : chunks)
                    .Select(i => http.GetByteArrayAsync($"{url}&range={i * ChunkSize}-{Math.Min((i + 1) * ChunkSize - 1, size)}", cancellationToken)).ToList())
                {
                    await file.WriteAsync(await task, cancellationToken);
                }
            }
            File.Delete(path);
            // TODO: convert weba to opus?
            File.Move(t, path);
        }, cancellationToken)
    );
}

sealed record RequestContextClient(string ClientName = "TVHTML5_SIMPLY_EMBEDDED_PLAYER", string ClientVersion = "2.0");
sealed record WebCommandMetadata(bool SendPost, string ApiUrl);
sealed record CommandMetadata(WebCommandMetadata WebCommandMetadata);
sealed record ContinuationCommand(string Token);
sealed record ContinuationEndpoint(CommandMetadata CommandMetadata, ContinuationCommand ContinuationCommand);
sealed record ContinuationItemRenderer(ContinuationEndpoint ContinuationEndpoint);

sealed record PlaylistRequestContext(RequestContextClient Client);
sealed record PlaylistRequest(PlaylistRequestContext Context, string Continuation);
sealed record Param(string Key, string Value);
sealed record ServiceTrackingParams(string Service, ICollection<Param> Params);
sealed record PlaylistResponseContext(ICollection<ServiceTrackingParams> ServiceTrackingParams);
sealed record Run(string Text)
{
    public override string ToString() => Text;
}
sealed record Title(ICollection<Run> Runs)
{
    public override string ToString() => string.Join(null, Runs);
}
sealed record PlaylistVideoRenderer(string VideoId, Title Title);
sealed record PlaylistVideoListContent
{
    public PlaylistVideoRenderer? PlaylistVideoRenderer { get; init; }
    public ContinuationItemRenderer? ContinuationItemRenderer { get; init; }
}
sealed record PlaylistVideoListRenderer(ICollection<PlaylistVideoListContent> Contents);
sealed record ItemSectionContent(PlaylistVideoListRenderer PlaylistVideoListRenderer);
sealed record ItemSectionRenderer(ICollection<ItemSectionContent> Contents);
sealed record SectionListContent()
{
    public ItemSectionRenderer? ItemSectionRenderer { get; init; }
    public ContinuationItemRenderer? ContinuationItemRenderer { get; init; }
}
sealed record SectionListRenderer(IList<SectionListContent> Contents);
sealed record TabContent(SectionListRenderer SectionListRenderer);
sealed record TabRenderer(TabContent Content);
sealed record Tab(TabRenderer TabRenderer);
sealed record TwoColumnBrowseResultsRenderer(IList<Tab> Tabs);
sealed record PlaylistContent(TwoColumnBrowseResultsRenderer TwoColumnBrowseResultsRenderer);
sealed record AppendContinuationItemsAction(ICollection<PlaylistVideoListContent> ContinuationItems);
sealed record OnResponseReceivedAction(AppendContinuationItemsAction AppendContinuationItemsAction);
sealed record PlaylistResponse(PlaylistResponseContext ResponseContext, PlaylistContent Contents, ICollection<OnResponseReceivedAction> OnResponseReceivedActions);

sealed record ThirdParty(string EmbedUrl = "https://google.com");
sealed record PlayerRequestContext(RequestContextClient Client, ThirdParty ThirdParty)
{
    public PlayerRequestContext() : this(new(), new()) { }
}
sealed record PlayerRequest(string VideoId, PlayerRequestContext Context)
{
    public PlayerRequest(string videoId) : this(videoId, new()) { }
}
sealed record CaptionTrack(string BaseUrl, string LanguageCode);
sealed record PlayerCaptionsTracklistRenderer(ICollection<CaptionTrack> CaptionTracks);
sealed record Captions(PlayerCaptionsTracklistRenderer PlayerCaptionsTracklistRenderer);
enum AudioQuality
{
    AUDIO_QUALITY_LOW,
    AUDIO_QUALITY_MEDIUM,
    AUDIO_QUALITY_HIGH
}
sealed record AdaptiveFormat(int Itag, string MimeType, int Bitrate, int AverageBitrate,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)] int ContentLength,
    AudioQuality AudioQuality
)
{
    public string? Url { get; init; }
    public string? SignatureCipher { get; init; }
}
sealed record StreamingData(ICollection<AdaptiveFormat> AdaptiveFormats);
sealed record VideoDetails(string Title);
sealed record PlayerResponse(StreamingData StreamingData, VideoDetails VideoDetails)
{
    public Captions? Captions { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(PlaylistRequest))]
[JsonSerializable(typeof(PlaylistResponse))]
[JsonSerializable(typeof(PlayerRequest))]
[JsonSerializable(typeof(PlayerResponse))]
sealed partial class AppJsonContext : JsonSerializerContext { }

static partial class Patterns
{
    [GeneratedRegex("""(?<=\bytInitialData\s*=\s*){.+?}(?=;?\s*<\/script>)""", RegexOptions.Singleline)]
    internal static partial Regex YtInitialData();

    [GeneratedRegex("""(?<=\bplayer\\?/)[^\\/]+""")]
    internal static partial Regex Player();

    [GeneratedRegex("""(?<=\b[cs]\s*&&\s*[adf]\.set\([^,]+\s*,\s*encodeURIComponent\s*\(\s*)[a-zA-Z0-9$]+(?=\()|(?<=\b[a-zA-Z0-9]+\s*&&\s*[a-zA-Z0-9]+\.set\([^,]+\s*,\s*encodeURIComponent\s*\(\s*)[a-zA-Z0-9$]+(?=\()|(?<=\bm=)[a-zA-Z0-9$]{2,}(?=\(decodeURIComponent\(h\.s\)\))|(?<=\bc&&\(c=)[a-zA-Z0-9$]{2,}(?=\(decodeURIComponent\(c\)\))|(?<=\b|[^a-zA-Z0-9$])[a-zA-Z0-9$]{2,}(?=\s*=\s*function\(\s*a\s*\)\s*{\s*a\s*=\s*a\.split\(\s*""\s*\)(?:;[a-zA-Z0-9$]{2}\.[a-zA-Z0-9$]{2}\(a,\d+\))?)|[a-zA-Z0-9$]+(?=\s*=\s*function\(\s*a\s*\)\s*{\s*a\s*=\s*a\.split\(\s*""\s*\))|(?<=("|')signature\1\s*,\s*)[a-zA-Z0-9$]+(?=\()|(?<=\.sig\|\|)[a-zA-Z0-9$]+(?=\()|(?<=yt\.akamaized\.net/\)\s*\|\|\s*.*?\s*[cs]\s*&&\s*[adf]\.set\([^,]+\s*,\s*(?:encodeURIComponent\s*\()?\s*)[a-zA-Z0-9$]+(?=\()|(?<=\b[cs]\s*&&\s*[adf]\.set\([^,]+\s*,\s*)[a-zA-Z0-9$]+(?=\()|(?<=\b[a-zA-Z0-9]+\s*&&\s*[a-zA-Z0-9]+\.set\([^,]+\s*,\s*)[a-zA-Z0-9$]+(?=\()|(?<=\bc\s*&&\s*[a-zA-Z0-9]+\.set\([^,]+\s*,\s*\([^)]*\)\s*\(\s*)[a-zA-Z0-9$]+(?=\()""")]
    internal static partial Regex SDecipher();

    [GeneratedRegex("""(?<=\.get\("n"\)\)&&\(b=)[a-zA-Z0-9$]+(?:\[\d+\])?(?=\([a-zA-Z0-9]\))""")]
    internal static partial Regex NDecipher();
}