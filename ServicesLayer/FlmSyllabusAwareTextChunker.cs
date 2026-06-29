namespace ServicesLayer;

public sealed class FlmSyllabusAwareTextChunker : ITextChunker
{
    public string StrategyName => "syllabus-aware-flm-v1";

    public Task<TextChunkingResult> CreateChunkingResultAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(new TextChunkingResult(Array.Empty<TextChunk>()));
        }

        const int chunkSize = 950;
        const int overlap = 50;
        var chunks = new List<TextChunk>();

        for (var start = 0; start < text.Length; start += chunkSize - overlap)
        {
            var end = Math.Min(start + chunkSize, text.Length);
            var chunkText = text[start..end];

            chunks.Add(new TextChunk(
                chunks.Count,
                chunkText,
                string.Empty,
                start,
                end));
        }

        return Task.FromResult(new TextChunkingResult(chunks));
    }
}