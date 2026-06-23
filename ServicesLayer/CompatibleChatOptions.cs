namespace ServicesLayer;

public sealed record CompatibleChatOptions(
    bool Enabled,
    string ApiKey,
    string Model,
    int TimeoutSeconds,
    string BaseUrl);
