namespace ServicesLayer;

public sealed record HuggingFaceOptions(
    bool Enabled,
    string Token,
    string ChatModel,
    string EmbeddingModel,
    int TimeoutSeconds,
    string ChatBaseUrl,
    string EmbeddingBaseUrl);
