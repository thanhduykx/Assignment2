using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServicesLayer;

public sealed class GeminiEmbeddingService : IEmbeddingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    public GeminiEmbeddingService(HttpClient httpClient, GeminiOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public string ModelName => string.IsNullOrWhiteSpace(_options.EmbeddingModel)
        ? "gemini-embedding-2"
        : _options.EmbeddingModel.Trim();

    public int Dimensions => NormalizeDimensions(_options.EmbeddingDimensions);

    public async Task<Dictionary<int, double>> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Dictionary<int, double>();
        }

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is required for embeddings. Set Gemini:ApiKey in appsettings.json before indexing documents.");
        }

        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var request = CreateEmbeddingRequest(text);
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await ReadErrorBodyAsync(response, cancellationToken);
                    var detail = string.IsNullOrWhiteSpace(errorBody) ? response.ReasonPhrase : errorBody;
                    if (IsTransientStatusCode(response.StatusCode) && attempt < maxAttempts)
                    {
                        await DelayBeforeRetryAsync(attempt, cancellationToken);
                        continue;
                    }

                    throw new InvalidOperationException($"Gemini embedding request failed with HTTP {(int)response.StatusCode}. {detail}");
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var payload = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var values = ExtractEmbeddingVector(payload.RootElement);
                if (values.Count == 0)
                {
                    throw new InvalidOperationException("Gemini embedding response did not contain vector values.");
                }

                return EmbeddingVector.NormalizeDenseEmbedding(values);
            }
            catch (HttpRequestException) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                await DelayBeforeRetryAsync(attempt, cancellationToken);
                continue;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < maxAttempts)
            {
                await DelayBeforeRetryAsync(attempt, cancellationToken);
                continue;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException("Gemini embedding request timed out. Check Gemini:ApiKey in appsettings.json, network, provider status, or increase Gemini:TimeoutSeconds.");
            }
            catch (HttpRequestException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException($"Gemini embedding request failed due to a network error after {maxAttempts} attempts. {ex.Message}", ex);
            }
        }

        throw new InvalidOperationException("Gemini embedding request failed.");
    }

    public double CosineSimilarity(IReadOnlyDictionary<int, double> left, IReadOnlyDictionary<int, double> right)
    {
        if (left.Count == 0 || right.Count == 0)
        {
            return 0;
        }

        var smaller = left.Count < right.Count ? left : right;
        var larger = ReferenceEquals(smaller, left) ? right : left;
        return smaller.Sum(item => larger.TryGetValue(item.Key, out var value) ? item.Value * value : 0);
    }

    private string ResolveEmbeddingUrl()
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.EmbeddingBaseUrl)
            ? "https://generativelanguage.googleapis.com/v1beta"
            : _options.EmbeddingBaseUrl.Trim().TrimEnd('/');
        var model = ModelName.Trim().Trim('/');
        return $"{baseUrl}/models/{model}:embedContent";
    }

    private HttpRequestMessage CreateEmbeddingRequest(string text)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, ResolveEmbeddingUrl())
        {
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = JsonContent.Create(
                new GeminiEmbeddingRequest(
                    $"models/{ModelName.Trim().TrimStart('/')}",
                    new GeminiContent([new GeminiPart(PrepareRetrievalText(text))]),
                    Dimensions),
                options: JsonOptions)
        };
        request.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey.Trim());
        request.Headers.ConnectionClose = true;
        return request;
    }

    private static Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        return Task.Delay(TimeSpan.FromMilliseconds(500 * attempt * attempt), cancellationToken);
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static async Task<string> ReadErrorBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        body = body.ReplaceLineEndings(" ").Trim();
        return body.Length <= 500 ? body : body[..500];
    }

    private static IReadOnlyList<double> ExtractEmbeddingVector(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var propertyName in new[] { "values", "embedding", "embeddings", "vector" })
            {
                if (element.TryGetProperty(propertyName, out var property))
                {
                    var vector = ExtractEmbeddingVector(property);
                    if (vector.Count > 0)
                    {
                        return vector;
                    }
                }
            }

            return Array.Empty<double>();
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<double>();
        }

        var direct = new List<double>();
        var nestedVectors = new List<IReadOnlyList<double>>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out var value))
            {
                direct.Add(value);
                continue;
            }

            var nested = ExtractEmbeddingVector(item);
            if (nested.Count > 0)
            {
                nestedVectors.Add(nested);
            }
        }

        if (direct.Count > 0)
        {
            return direct;
        }

        return nestedVectors.Count == 0 ? Array.Empty<double>() : nestedVectors[0];
    }

    private static string PrepareRetrievalText(string text)
    {
        var normalized = string.Join(" ", text.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        return $"task: search result | query: {normalized}";
    }

    private static int NormalizeDimensions(int dimensions)
    {
        return dimensions is >= 128 and <= 3072 ? dimensions : 768;
    }

    private sealed record GeminiEmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("content")] GeminiContent Content,
        [property: JsonPropertyName("outputDimensionality")] int OutputDimensionality);

    private sealed record GeminiContent([property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiPart([property: JsonPropertyName("text")] string Text);
}
