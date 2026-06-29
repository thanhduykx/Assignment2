namespace ServicesLayer;

public interface IWebPageTextExtractor
{
    Task<WebPageExtractionResult> ExtractAsync(string url, CancellationToken cancellationToken = default);
}

public sealed record WebPageExtractionResult(string Title, string SourceUrl, string Text)
{
    public bool UsedBrowserRenderer { get; init; }
}

public sealed class WebPageTextExtractor : IWebPageTextExtractor
{
    private readonly HttpClient _httpClient;

    public WebPageTextExtractor(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WebPageExtractionResult> ExtractAsync(string url, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        return new WebPageExtractionResult("Web Page", url, text);
    }
}