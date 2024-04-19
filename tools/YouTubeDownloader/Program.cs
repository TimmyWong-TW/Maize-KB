using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using YouTube;

string playlistId = Environment.GetEnvironmentVariable("PLAYLIST_ID") ??
#if DEBUG
    "PLe-YK1dmFUsLP91e4OhcDqnqK_0smrmaL";
#else
    throw new InvalidOperationException("No playlist ID.");
#endif

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

// TODO: enumerate videos in a playlist
var html = await http.GetStringAsync($"/playlist?list={playlistId}");
var playlist = JsonSerializer.Deserialize(Patterns.YtInitialData().Match(html).Value, AppJsonContext.Default.PlaylistResponse) ??
    throw new InvalidOperationException("Failed to fetch playlist initially.");
var csi = playlist.ResponseContext.ServiceTrackingParams.First(p => p.Service == "CSI").Params.ToDictionary(p => p.Key, p => p.Value);
;

// TODO: decipher signature for non-embeddable
static async Task DownloadEmbeddableAsync(HttpClient http, string videoId, string? basePath = null, CancellationToken cancellationToken = default)
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
            var format = formats
                .Where(f => f is { MimeType: ['a', 'u', 'd', 'i', 'o', '/', ..] })
                .OrderByDescending(f => f.AudioQuality)
                .ThenBy(f => f.ContentLength)
                .First();
            var url = format.Url; // TODO decipher
            var type = format.MimeType[6..format.MimeType.IndexOf(';', 7)];
            type = type switch
            {
                "webm" => "weba",
                "mp4" => "m4a",
                _ => type
            };
            var path = Path.Join(basePath, $"{filename}.{type}");
            var t = path + '~';
            {
                await using var file = File.Create(t);
                await using var stream = await http.GetStreamAsync(url, cancellationToken);
                await stream.CopyToAsync(file, cancellationToken);
            }
            File.Delete(path);
            File.Move(t, path);
        }, cancellationToken)
    );
}

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
sealed record PlaylistVideoListRenderer(ICollection<PlaylistVideoRenderer> Contents);
sealed record ItemSectionContent(PlaylistVideoListRenderer PlaylistVideoListRenderer);
sealed record ItemSectionRenderer(ICollection<ItemSectionContent> Contents);
sealed record WebCommandMetadata(bool SendPost, string ApiUrl);
sealed record CommandMetadata(WebCommandMetadata WebCommandMetadata);
sealed record ContinuationCommand(string Token);
sealed record ContinuationEndpoint(CommandMetadata CommandMetadata, ContinuationCommand ContinuationCommand);
sealed record ContinuationItemRenderer(ContinuationEndpoint ContinuationEndpoint);
sealed record SectionListContent()
{
    public ItemSectionRenderer? ItemSectionRenderer { get; init; }
    public ContinuationItemRenderer? ContinuationItemRenderer { get; init; }
}
sealed record SectionListRenderer(ICollection<SectionListContent> Contents);
sealed record TabContent(SectionListRenderer SectionListRenderer);
sealed record TabRenderer(TabContent Content);
sealed record Tab(TabRenderer TabRenderer);
sealed record TwoColumnBrowseResultsRenderer(ICollection<Tab> Tabs);
sealed record PlaylistContent(TwoColumnBrowseResultsRenderer TwoColumnBrowseResultsRenderer);
sealed record PlaylistResponse(PlaylistResponseContext ResponseContext, PlaylistContent Contents);

sealed record PlayerRequestContextClient(string ClientName = "WEB_EMBEDDED_PLAYER", string ClientVersion = "1.20240415.01.00");
sealed record PlayerRequestContext(PlayerRequestContextClient Client)
{
    public PlayerRequestContext() : this(new PlayerRequestContextClient()) { }
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
sealed record AdaptiveFormat(int Itag, string MimeType,
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
[JsonSerializable(typeof(PlaylistResponse))]
[JsonSerializable(typeof(PlayerRequest))]
[JsonSerializable(typeof(PlayerResponse))]
sealed partial class AppJsonContext : JsonSerializerContext { }

static partial class Patterns
{
    [GeneratedRegex(@"(?<=\bytInitialData\s*=\s*){.+?}(?=;?\s*<\/script>)", RegexOptions.Singleline)]
    internal static partial Regex YtInitialData();
}