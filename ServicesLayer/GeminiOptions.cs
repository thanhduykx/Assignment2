namespace ServicesLayer;

public sealed record GeminiOptions(
    bool Enabled,
    string ApiKey,
    string ChatModel,
    string EmbeddingModel,
    int TimeoutSeconds,
    string ChatBaseUrl,
    string EmbeddingBaseUrl);
