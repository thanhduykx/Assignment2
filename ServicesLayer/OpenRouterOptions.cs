namespace ServicesLayer;

public sealed record OpenRouterOptions(
    bool Enabled,
    string ApiKey,
    string ChatModel,
    int TimeoutSeconds,
    string ChatBaseUrl,
    string Referer,
    string Title);
