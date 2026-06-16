using System.Net;
using System.Text;
using ServicesLayer;

namespace ServicesLayer.Tests;

public sealed class OpenAICompatibleServiceTests
{
    [Fact]
    public async Task ChatService_ParseChoicesMessageContent()
    {
        var service = new OpenAICompatibleChatCompletionService(
            new HttpClient(new StaticHttpMessageHandler(
                HttpStatusCode.OK,
                """{"choices":[{"message":{"role":"assistant","content":"Answer from HF"}}]}""")),
            CreateOptions());

        var answer = await service.GenerateAnswerAsync(
            "Question?",
            "DBA103",
            Array.Empty<DataAccessLayer.ChatMessage>(),
            new[]
            {
                new DataAccessLayer.DocumentChunk
                {
                    FileName = "doc.txt",
                    Subject = "DBA103",
                    Chapter = "Intro",
                    Text = "Evidence"
                }
            },
            "en");

        Assert.Equal("Answer from HF", answer);
    }

    [Fact]
    public async Task ChatService_ReturnsNullOnHttpFailure()
    {
        var service = new OpenAICompatibleChatCompletionService(
            new HttpClient(new StaticHttpMessageHandler(HttpStatusCode.BadGateway, "{}")),
            CreateOptions());

        var answer = await service.GenerateAnswerAsync(
            "Question?",
            "DBA103",
            Array.Empty<DataAccessLayer.ChatMessage>(),
            new[] { new DataAccessLayer.DocumentChunk { Text = "Evidence" } },
            "en");

        Assert.Null(answer);
    }

    [Fact]
    public async Task EmbeddingService_ParsesAndNormalizesVector()
    {
        var service = new OpenAICompatibleEmbeddingService(
            new HttpClient(new StaticHttpMessageHandler(HttpStatusCode.OK, "[3,4]")),
            CreateOptions());

        var vector = await service.EmbedAsync("hello");

        Assert.Equal("Qwen/Qwen3-Embedding-0.6B", service.ModelName);
        Assert.Equal(1024, service.Dimensions);
        Assert.Equal(0.6, vector[0], precision: 6);
        Assert.Equal(0.8, vector[1], precision: 6);
    }

    [Fact]
    public async Task EmbeddingService_ThrowsOnHttpFailure()
    {
        var service = new OpenAICompatibleEmbeddingService(
            new HttpClient(new StaticHttpMessageHandler(HttpStatusCode.Forbidden, "{}")),
            CreateOptions());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EmbedAsync("hello"));
    }

    private static OpenAICompatibleOptions CreateOptions()
    {
        return new OpenAICompatibleOptions(
            true,
            "test-token",
            "Qwen/Qwen2.5-7B-Instruct:fastest",
            "Qwen/Qwen3-Embedding-0.6B",
            5,
            "https://example.test/v1/chat/completions",
            "https://example.test/models");
    }

    private sealed class StaticHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public StaticHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            });
        }
    }
}
