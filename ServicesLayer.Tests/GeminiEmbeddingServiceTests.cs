using System.Net;
using System.Text;
using ServicesLayer;

namespace ServicesLayer.Tests;

public sealed class GeminiEmbeddingServiceTests
{
    [Fact]
    public async Task EmbedAsync_SendsGeminiRequestAndParsesVector()
    {
        var handler = new CaptureHttpMessageHandler(
            HttpStatusCode.OK,
            """{"embedding":{"values":[3,4]}}""");
        var service = new GeminiEmbeddingService(
            new HttpClient(handler),
            new GeminiOptions(
                true,
                "test-key",
                "gemini-3.5-flash",
                "gemini-embedding-2",
                768,
                5,
                "https://example.test/openai/chat/completions",
                "https://example.test/v1beta"));

        var vector = await service.EmbedAsync("hello world");

        Assert.Equal("https://example.test/v1beta/models/gemini-embedding-2:embedContent", handler.RequestUri);
        Assert.Equal("test-key", handler.ApiKeyHeader);
        Assert.Contains("\"output_dimensionality\":768", handler.Body);
        Assert.Contains("task: search result | query: hello world", handler.Body);
        Assert.Equal(768, service.Dimensions);
        Assert.Equal(0.6, vector[0], precision: 6);
        Assert.Equal(0.8, vector[1], precision: 6);
    }

    [Fact]
    public async Task EmbedAsync_ParsesEmbeddingsArrayShape()
    {
        var service = new GeminiEmbeddingService(
            new HttpClient(new CaptureHttpMessageHandler(
                HttpStatusCode.OK,
                """{"embeddings":[{"values":[0,5]}]}""")),
            new GeminiOptions(
                true,
                "test-key",
                "gemini-3.5-flash",
                "gemini-embedding-2",
                768,
                5,
                "https://example.test/openai/chat/completions",
                "https://example.test/v1beta"));

        var vector = await service.EmbedAsync("hello");

        Assert.Equal(0, vector.GetValueOrDefault(0));
        Assert.Equal(1, vector[1], precision: 6);
    }

    private sealed class CaptureHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public CaptureHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        public string RequestUri { get; private set; } = string.Empty;

        public string ApiKeyHeader { get; private set; } = string.Empty;

        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString() ?? string.Empty;
            ApiKeyHeader = request.Headers.TryGetValues("x-goog-api-key", out var values)
                ? values.FirstOrDefault() ?? string.Empty
                : string.Empty;
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            };
        }
    }
}
