using SemanticChunker;
using System.Text;
using System.Text.Json;

using CancellationTokenSource cts = new();
Console.CancelKeyPress += (s, e) =>
{
    cts.Cancel();
    e.Cancel = true;
};

foreach (var file in new DirectoryInfo("/data/whisperx").EnumerateFiles("*.json", new EnumerationOptions
{
    IgnoreInaccessible = true,
    MatchCasing = MatchCasing.CaseInsensitive,
    MatchType = MatchType.Simple,
    RecurseSubdirectories = true,
    ReturnSpecialDirectories = false
}))
{
    var tsv = $"/data/tsv{file.FullName.AsSpan("/data/whisperx".Length..^"json".Length)}tsv";
    if (!File.Exists(tsv))
    {
        Directory.CreateDirectory(Path.GetDirectoryName(tsv)!);
        var tmp = tsv + '~';
        await using var stream = file.OpenRead();
        var json = await JsonSerializer.DeserializeAsync(stream, WhisperXJsonContext.Default.WhisperXJson, cts.Token) ??
            throw new InvalidDataException();
        json.PatchSegments();
        var segments = json.ResegmentChineseSentences().ToList();
        await File.WriteAllTextAsync(tmp, (json with { Segments = segments }).ToTSV(), Encoding.UTF8, CancellationToken.None);
        File.Move(tmp, tsv);
    }
}