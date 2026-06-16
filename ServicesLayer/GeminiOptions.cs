namespace ServicesLayer;

public sealed record GeminiOptions(
    bool Enabled,
    string ApiKey,
    string ChatModel,
    string EmbeddingModel,
    int EmbeddingDimensions,
    int TimeoutSeconds,
    string ChatBaseUrl,
    string EmbeddingBaseUrl);
