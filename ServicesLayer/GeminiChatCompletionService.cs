namespace ServicesLayer;

public sealed class GeminiChatCompletionService : ILocalChatCompletionService
{
    private readonly CompatibleChatCompletionService _inner;

    public GeminiChatCompletionService(HttpClient httpClient, GeminiOptions options)
    {
        _inner = new CompatibleChatCompletionService(
            httpClient,
            new CompatibleChatOptions(
                options.Enabled,
                options.ApiKey,
                options.ChatModel,
                options.TimeoutSeconds,
                options.ChatBaseUrl));
    }

    public bool IsEnabled => _inner.IsEnabled;

    public Task<QueryIntentDecision> ClassifyQuestionAsync(
        string question,
        IReadOnlyList<DataAccessLayer.ChatMessage> history,
        string language,
        CancellationToken cancellationToken = default)
    {
        return _inner.ClassifyQuestionAsync(question, history, language, cancellationToken);
    }

    public Task<string> RewriteQuestionAsync(
        string question,
        IReadOnlyList<DataAccessLayer.ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        return _inner.RewriteQuestionAsync(question, history, cancellationToken);
    }

    public Task<IReadOnlyList<string>> RewriteQueriesAsync(
        string question,
        IReadOnlyList<DataAccessLayer.ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        return _inner.RewriteQueriesAsync(question, history, cancellationToken);
    }

    public Task<IReadOnlyList<ChatChunkRerankResult>> RerankChunksAsync(
        string question,
        IReadOnlyList<DataAccessLayer.DocumentChunk> chunks,
        string language,
        CancellationToken cancellationToken = default)
    {
        return _inner.RerankChunksAsync(question, chunks, language, cancellationToken);
    }

    public Task<string?> GenerateAnswerAsync(
        string question,
        string subject,
        IReadOnlyList<DataAccessLayer.ChatMessage> history,
        IReadOnlyList<DataAccessLayer.DocumentChunk> chunks,
        string language,
        CancellationToken cancellationToken = default)
    {
        return _inner.GenerateAnswerAsync(question, subject, history, chunks, language, cancellationToken);
    }

    public Task<GroundingDecision?> ValidateGroundingAsync(
        string question,
        string answer,
        IReadOnlyList<DataAccessLayer.DocumentChunk> chunks,
        string language,
        CancellationToken cancellationToken = default)
    {
        return _inner.ValidateGroundingAsync(question, answer, chunks, language, cancellationToken);
    }

    public Task<string?> GenerateChunkRetrievalHintsAsync(
        string chunkText,
        string fileName,
        string subject,
        string chapter,
        string sectionTitle,
        CancellationToken cancellationToken = default)
    {
        return _inner.GenerateChunkRetrievalHintsAsync(chunkText, fileName, subject, chapter, sectionTitle, cancellationToken);
    }
}
