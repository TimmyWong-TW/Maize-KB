using Microsoft.Extensions.DependencyInjection;
using SemanticChunker;

using CancellationTokenSource cts = new();
Console.CancelKeyPress += (s, e) =>
{
    cts.Cancel();
    e.Cancel = true;
};


ServiceCollection services = new();
services.AddHttpClient("embedder", client =>
{
    client.BaseAddress = new(Environment.GetEnvironmentVariable("EMBEDDER_HOST") ??
        throw new InvalidOperationException("Embedder host not configured."));
});
services.AddSingleton<EmbedderService>();
await using var serviceProvider = services.BuildServiceProvider();
var embedder = serviceProvider.GetRequiredService<EmbedderService>();

// TODO: semantic chunking