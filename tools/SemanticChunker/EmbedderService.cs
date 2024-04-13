using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SemanticChunker;

internal sealed class EmbedderService(IHttpClientFactory httpFactory)
{
    private async Task<IList<Relevance>> RerankAsyncCore(IEnumerable<Tuple<string, string>> pairs, CancellationToken cancellationToken = default)
    {
        using var http = httpFactory.CreateClient("embedder");
        using var response = await http.PostAsJsonAsync("v1/relevances", new RerankRequest(pairs), EmbedderJsonContext.Default.RerankRequest, cancellationToken);
        var list = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync(EmbedderJsonContext.Default.DataListRelevance, cancellationToken) ??
            throw new InvalidDataException("No relevances.");
        return list.Data;
    }

    public async Task<IEnumerable<float>> RerankAsync(IEnumerable<Tuple<string, string>> pairs, CancellationToken cancellationToken = default)
    => (await RerankAsyncCore(pairs, cancellationToken)).OrderBy(r => r.Index).Select(r => r.Score);

    public async Task<IList<string>> RerankAsync(string question, IList<string> answers, CancellationToken cancellationToken = default)
    => (await RerankAsyncCore(answers.Select(a => Tuple.Create(question, a)), cancellationToken))
        .OrderByDescending(r => r.Score).Select(r => answers[r.Index]).ToArray();

    public async Task<IEnumerable<IList<int>>> RerankAsync(IList<string> headings, IList<string> sentences, CancellationToken cancellationToken = default)
    {
        var scores = (await RerankAsync(sentences.SelectMany(p => headings, (p, h) => Tuple.Create(h, p)), cancellationToken)).ToArray();
        return Enumerable.Range(0, sentences.Count)
            .Select(i => scores.Skip(i * headings.Count).Take(headings.Count)
                .Select((s, i) => (i, s))
                .OrderByDescending(p => p.s)
                .Select(p => p.i)
                .ToArray()
            );
    }
}

internal sealed record RerankRequest(IEnumerable<Tuple<string, string>> Pairs);

internal sealed record Relevance(float Score, int Index);

internal sealed record DataList<T>(IList<T> Data);


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(RerankRequest))]
[JsonSerializable(typeof(DataList<Relevance>))]
internal sealed partial class EmbedderJsonContext : JsonSerializerContext { }