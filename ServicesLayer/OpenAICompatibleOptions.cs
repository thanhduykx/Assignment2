namespace ServicesLayer;

public sealed record OpenAICompatibleOptions(
    bool Enabled,
    string Token,
    string ChatModel,
    string EmbeddingModel,
    int TimeoutSeconds,
    string ChatBaseUrl,
    string EmbeddingBaseUrl);
