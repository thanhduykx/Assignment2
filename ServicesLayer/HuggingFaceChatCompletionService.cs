using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataAccessLayer;

namespace ServicesLayer;

public sealed class HuggingFaceChatCompletionService : ILocalChatCompletionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly HuggingFaceOptions _options;

    public HuggingFaceChatCompletionService(HttpClient httpClient, HuggingFaceOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public bool IsEnabled => _options.Enabled && !string.IsNullOrWhiteSpace(_options.Token);

    public async Task<QueryIntentDecision> ClassifyQuestionAsync(
        string question,
        IReadOnlyList<ChatMessage> history,
        string language,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(question))
        {
            return new QueryIntentDecision(ChatQueryIntent.DocumentQuestion, 0, "classifier-disabled");
        }

        var response = await CallChatAsync(
            "You classify messages for a document-grounded learning chatbot. Return only valid JSON.",
            $$"""
            Classify this message into SmallTalk, DocumentQuestion, ExternalQuestion, or Unsafe.
            Return JSON only: {"intent":"DocumentQuestion","confidence":0.95,"reason":"short reason"}
            UI language: {{language}}

            Recent history:
            {{BuildHistoryText(history)}}

            Message:
            {{question.Trim()}}
            """,
            0.1,
            256,
            cancellationToken);

        return ParseIntentDecision(response);
    }

    public async Task<string> RewriteQuestionAsync(
        string question,
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(question))
        {
            return question;
        }

        var response = await CallChatAsync(
            "Rewrite follow-up questions for document retrieval. Return only the rewritten question.",
            $$"""
            Rewrite the student's message into a standalone search question.
            Keep course codes exactly. Do not add facts. If already clear, return it unchanged.

            Recent history:
            {{BuildHistoryText(history)}}

            Message:
            {{question.Trim()}}
            """,
            0.1,
            256,
            cancellationToken);

        return string.IsNullOrWhiteSpace(response) ? question : response.Trim().Trim('"');
    }

    public async Task<IReadOnlyList<string>> RewriteQueriesAsync(
        string question,
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(question))
        {
            return new[] { question };
        }

        var response = await CallChatAsync(
            "Rewrite student messages into retrieval queries. Return only valid JSON.",
            $$"""
            Rewrite the message into 2 to 4 independent search queries for document retrieval.
            Keep course codes exactly. Include Vietnamese/English variants only when useful.
            Return JSON only: {"queries":["query 1","query 2"]}

            Recent history:
            {{BuildHistoryText(history)}}

            Message:
            {{question.Trim()}}
            """,
            0.1,
            512,
            cancellationToken);

        var queries = ParseRewriteQueries(response);
        return queries.Count == 0 ? new[] { question } : queries;
    }

    public async Task<IReadOnlyList<ChatChunkRerankResult>> RerankChunksAsync(
        string question,
        IReadOnlyList<DocumentChunk> chunks,
        string language,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(question) || chunks.Count == 0)
        {
            return Array.Empty<ChatChunkRerankResult>();
        }

        var response = await CallChatAsync(
            "You are a strict retrieval quality judge. Do not answer the question. Return only valid JSON.",
            BuildRerankPrompt(question, chunks, language),
            0.1,
            1024,
            cancellationToken);

        return ParseRerankResponse(response, chunks.Count);
    }

    public async Task<string?> GenerateAnswerAsync(
        string question,
        string subject,
        IReadOnlyList<ChatMessage> history,
        IReadOnlyList<DocumentChunk> chunks,
        string language,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(question) || chunks.Count == 0)
        {
            return null;
        }

        return await CallChatAsync(
            BuildSystemPrompt(subject, language),
            BuildAnswerPrompt(question, history, chunks, language),
            0.2,
            1024,
            cancellationToken);
    }

    public async Task<GroundingDecision?> ValidateGroundingAsync(
        string question,
        string answer,
        IReadOnlyList<DocumentChunk> chunks,
        string language,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(answer) || chunks.Count == 0)
        {
            return null;
        }

        var response = await CallChatAsync(
            "You verify whether an answer is fully supported by evidence chunks. Return only valid JSON.",
            BuildGroundingPrompt(question, answer, chunks, language),
            0.1,
            512,
            cancellationToken);

        return ParseGroundingDecision(response);
    }

    private async Task<string?> CallChatAsync(
        string system,
        string prompt,
        double temperature,
        int maxTokens,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ResolveChatUrl());
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
            request.Content = JsonContent.Create(
                new HuggingFaceChatRequest(
                    ModelName,
                    [
                        new HuggingFaceChatMessage("system", system),
                        new HuggingFaceChatMessage("user", prompt)
                    ],
                    temperature,
                    maxTokens),
                options: JsonOptions);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<HuggingFaceChatResponse>(JsonOptions, cancellationToken);
            var content = payload?.Choices?
                .FirstOrDefault()?
                .Message?
                .Content;
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private string ModelName => string.IsNullOrWhiteSpace(_options.ChatModel)
        ? "Qwen/Qwen2.5-7B-Instruct:fastest"
        : _options.ChatModel.Trim();

    private string ResolveChatUrl()
    {
        return string.IsNullOrWhiteSpace(_options.ChatBaseUrl)
            ? "https://router.huggingface.co/v1/chat/completions"
            : _options.ChatBaseUrl.Trim();
    }

    private static QueryIntentDecision ParseIntentDecision(string? response)
    {
        var json = ExtractJsonObject(response);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new QueryIntentDecision(ChatQueryIntent.DocumentQuestion, 0, "invalid-classifier-json");
        }

        try
        {
            var payload = JsonSerializer.Deserialize<IntentResponse>(json, JsonOptions);
            return new QueryIntentDecision(ParseIntent(payload?.Intent), Math.Clamp(payload?.Confidence ?? 0, 0, 1), payload?.Reason ?? string.Empty);
        }
        catch (JsonException)
        {
            return new QueryIntentDecision(ChatQueryIntent.DocumentQuestion, 0, "invalid-classifier-json");
        }
    }

    private static ChatQueryIntent ParseIntent(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "smalltalk" or "small_talk" or "small talk" => ChatQueryIntent.SmallTalk,
            "externalquestion" or "external_question" or "external" => ChatQueryIntent.ExternalQuestion,
            "unsafe" => ChatQueryIntent.Unsafe,
            _ => ChatQueryIntent.DocumentQuestion
        };
    }

    private static IReadOnlyList<string> ParseRewriteQueries(string? response)
    {
        var json = ExtractJsonObject(response);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            var payload = JsonSerializer.Deserialize<RewriteResponse>(json, JsonOptions);
            return payload?.Queries is null
                ? Array.Empty<string>()
                : payload.Queries
                    .Where(query => !string.IsNullOrWhiteSpace(query))
                    .Select(query => query.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(4)
                    .ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static IReadOnlyList<ChatChunkRerankResult> ParseRerankResponse(string? response, int candidateCount)
    {
        var json = ExtractJsonObject(response);
        if (string.IsNullOrWhiteSpace(json) || candidateCount <= 0)
        {
            return Array.Empty<ChatChunkRerankResult>();
        }

        try
        {
            var payload = JsonSerializer.Deserialize<RerankResponse>(json, JsonOptions);
            return payload?.Selected is null
                ? Array.Empty<ChatChunkRerankResult>()
                : payload.Selected
                    .Where(item => item.Candidate >= 1 && item.Candidate <= candidateCount)
                    .Select(item => new ChatChunkRerankResult(
                        item.Candidate,
                        Math.Clamp(item.Score, 0, 1),
                        item.Reason ?? string.Empty))
                    .GroupBy(item => item.CandidateNumber)
                    .Select(group => group.OrderByDescending(item => item.Score).First())
                    .OrderByDescending(item => item.Score)
                    .Take(6)
                    .ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<ChatChunkRerankResult>();
        }
    }

    private static GroundingDecision? ParseGroundingDecision(string? response)
    {
        var json = ExtractJsonObject(response);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<GroundingResponse>(json, JsonOptions);
            return payload is null
                ? null
                : new GroundingDecision(payload.Grounded, Math.Clamp(payload.Confidence, 0, 1), payload.Reason ?? string.Empty);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string BuildSystemPrompt(string subject, string language)
    {
        if (language != "en")
        {
            return ChatPromptBuilder.BuildVietnameseSystemPrompt(subject);
        }

        var subjectName = string.IsNullOrWhiteSpace(subject) ? "the course" : subject.Trim();
        return $"""
            You are a learning assistant for {subjectName}.
            Answer only from the supplied document chunks. Do not add outside knowledge.
            If the chunks do not directly support the answer, reply exactly:
            "I do not have enough data in the documents to answer this question."
            Keep the answer concise and natural.
            """;
    }

    private static string BuildAnswerPrompt(
        string question,
        IReadOnlyList<ChatMessage> history,
        IReadOnlyList<DocumentChunk> chunks,
        string language)
    {
        var builder = new StringBuilder();
        builder.AppendLine(language == "en" ? "Documents:" : "Tai lieu:");
        for (var index = 0; index < chunks.Count; index++)
        {
            var chunk = chunks[index];
            builder.AppendLine($"[{index + 1}] {chunk.FileName} / {chunk.Subject} / {chunk.Chapter} / chunk {chunk.ChunkIndex}:");
            builder.AppendLine(TrimForPrompt(chunk.Text, 1800));
            builder.AppendLine();
        }

        builder.AppendLine("Recent history:");
        builder.AppendLine(BuildHistoryText(history));
        builder.AppendLine();
        builder.AppendLine(language == "en" ? "Student question:" : "Cau hoi cua sinh vien:");
        builder.AppendLine(question.Trim());
        return builder.ToString();
    }

    private static string BuildRerankPrompt(string question, IReadOnlyList<DocumentChunk> chunks, string language)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""Return JSON only: {"selected":[{"candidate":1,"score":0.95,"reason":"direct evidence"}]}""");
        builder.AppendLine("Select at most 6 candidates that directly contain evidence for the question.");
        builder.AppendLine($"Answer language later: {language}");
        builder.AppendLine();
        builder.AppendLine("Question:");
        builder.AppendLine(question.Trim());
        builder.AppendLine();
        for (var index = 0; index < chunks.Count; index++)
        {
            var chunk = chunks[index];
            builder.AppendLine($"Candidate {index + 1}: {chunk.Subject} / {chunk.Chapter} / chunk {chunk.ChunkIndex}");
            builder.AppendLine(TrimForPrompt(chunk.Text, 900));
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildGroundingPrompt(
        string question,
        string answer,
        IReadOnlyList<DocumentChunk> chunks,
        string language)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""Return JSON only: {"grounded":true,"confidence":0.95,"reason":"all facts are in evidence"}""");
        builder.AppendLine("grounded must be false if any factual claim is not supported by evidence.");
        builder.AppendLine($"Language: {language}");
        builder.AppendLine();
        builder.AppendLine("Question:");
        builder.AppendLine(question.Trim());
        builder.AppendLine("Answer:");
        builder.AppendLine(answer.Trim());
        builder.AppendLine("Evidence:");
        foreach (var chunk in chunks)
        {
            builder.AppendLine($"{chunk.Subject} / {chunk.Chapter}: {TrimForPrompt(chunk.Text, 1000)}");
        }

        return builder.ToString();
    }

    private static string BuildHistoryText(IReadOnlyList<ChatMessage> history)
    {
        if (history.Count == 0)
        {
            return "(none)";
        }

        return string.Join(
            "\n",
            history
                .TakeLast(6)
                .Select(message => $"{message.Role}: {TrimForPrompt(message.Content, 600)}"));
    }

    private static string ExtractJsonObject(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed.Trim('`').Trim();
            if (trimmed.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[4..].Trim();
            }
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        return start >= 0 && end > start ? trimmed[start..(end + 1)] : string.Empty;
    }

    private static string TrimForPrompt(string text, int maxLength)
    {
        var compact = string.Join(" ", (text ?? string.Empty).Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= maxLength ? compact : compact[..maxLength] + "...";
    }

    private sealed record HuggingFaceChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<HuggingFaceChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);

    private sealed record HuggingFaceChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record HuggingFaceChatResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<HuggingFaceChoice>? Choices);

    private sealed record HuggingFaceChoice(
        [property: JsonPropertyName("message")] HuggingFaceChatMessage? Message);

    private sealed record IntentResponse(
        [property: JsonPropertyName("intent")] string? Intent,
        [property: JsonPropertyName("confidence")] double Confidence,
        [property: JsonPropertyName("reason")] string? Reason);

    private sealed record RewriteResponse([property: JsonPropertyName("queries")] IReadOnlyList<string>? Queries);

    private sealed record RerankResponse([property: JsonPropertyName("selected")] IReadOnlyList<RerankItem>? Selected);

    private sealed record RerankItem(
        [property: JsonPropertyName("candidate")] int Candidate,
        [property: JsonPropertyName("score")] double Score,
        [property: JsonPropertyName("reason")] string? Reason);

    private sealed record GroundingResponse(
        [property: JsonPropertyName("grounded")] bool Grounded,
        [property: JsonPropertyName("confidence")] double Confidence,
        [property: JsonPropertyName("reason")] string? Reason);
}
