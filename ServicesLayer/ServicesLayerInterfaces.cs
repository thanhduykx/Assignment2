using BusinessObjects;

namespace ServicesLayer;

public interface IEmbeddingService
{
    string ModelName { get; }
    int Dimensions { get; }
    Task<Dictionary<int, double>> EmbedAsync(string text, CancellationToken cancellationToken = default);
    double CosineSimilarity(IReadOnlyDictionary<int, double> left, IReadOnlyDictionary<int, double> right);
}

public interface IDocumentTextExtractor
{
    Task<string> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
}

public interface ITextChunker
{
    string StrategyName { get; }
    Task<TextChunkingResult> CreateChunkingResultAsync(string text, CancellationToken cancellationToken = default);
}

public interface IChunkRetrievalEnrichmentService
{
    string StrategyName { get; }
    Task<ChunkRetrievalEnrichmentResult> BuildEmbeddingTextAsync(
        TextChunk chunk,
        ChunkRetrievalEnrichmentContext context,
        CancellationToken cancellationToken = default);
}

// Supporting data types

public sealed record TextChunkingResult(IReadOnlyList<TextChunk> Chunks);

public sealed record TextChunk(
    int ChunkIndex,
    string Text,
    string SectionTitle,
    int CharStart,
    int CharEnd);

public sealed record ChunkRetrievalEnrichmentResult(
    string EmbeddingText,
    bool IsTruncated,
    string StrategyName);

public sealed record ChunkRetrievalEnrichmentContext(
    string FileName,
    string Subject,
    string Chapter,
    string SectionTitle);

public static class EmbeddingVector
{
    public static Dictionary<int, double> NormalizeDenseEmbedding(IReadOnlyList<double> values)
    {
        var result = new Dictionary<int, double>(values.Count);
        var magnitude = Math.Sqrt(values.Sum(v => v * v));
        if (magnitude > 0)
        {
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i] / magnitude;
            }
        }
        else
        {
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = 0;
            }
        }
        return result;
    }
}

public static class TextEncodingHelper
{
    public static string NormalizeForIndexing(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return string.Join(" ", text.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }
}