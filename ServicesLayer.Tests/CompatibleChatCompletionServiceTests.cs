using System.Net;
using System.Text;
using ServicesLayer;

namespace ServicesLayer.Tests;

public sealed class CompatibleChatCompletionServiceTests
{
    [Fact]
    public async Task ChatService_ParseChoicesMessageContent()
    {
        var service = new CompatibleChatCompletionService(
            new HttpClient(new StaticHttpMessageHandler(
                HttpStatusCode.OK,
                """{"choices":[{"message":{"role":"assistant","content":"Answer from Gemini"}}]}""")),
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

        Assert.Equal("Answer from Gemini", answer);
    }

    [Fact]
    public async Task ChatService_ReturnsNullOnHttpFailure()
    {
        var service = new CompatibleChatCompletionService(
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

    private static CompatibleChatOptions CreateOptions()
    {
        return new CompatibleChatOptions(
            true,
            "test-token",
            "gemini-3.5-flash",
            5,
            "https://example.test/v1/chat/completions");
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
