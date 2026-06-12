using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServicesLayer;

public sealed class HuggingFaceEmbeddingService : IEmbeddingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly HuggingFaceOptions _options;

    public HuggingFaceEmbeddingService(HttpClient httpClient, HuggingFaceOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public string ModelName => string.IsNullOrWhiteSpace(_options.EmbeddingModel)
        ? "Qwen/Qwen3-Embedding-0.6B"
        : _options.EmbeddingModel.Trim();

    public int Dimensions => InferDimensions(ModelName);

    public async Task<Dictionary<int, double>> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Dictionary<int, double>();
        }

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Token))
        {
            throw new InvalidOperationException("HuggingFace token is required for embeddings.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ResolveEmbeddingUrl());
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
            request.Content = JsonContent.Create(
                new HuggingFaceEmbeddingRequest(text, new HuggingFaceEmbeddingOptions(true)),
                options: JsonOptions);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"HuggingFace embedding request failed with HTTP {(int)response.StatusCode}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var payload = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var values = ExtractEmbeddingVector(payload.RootElement);
            if (values.Count == 0)
            {
                throw new InvalidOperationException("HuggingFace embedding response did not contain vector values.");
            }

            return EmbeddingVector.NormalizeDenseEmbedding(values);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("HuggingFace embedding request timed out. Check HF_TOKEN, network, provider status, or increase HuggingFace:TimeoutSeconds.");
        }
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
            ? "https://router.huggingface.co/hf-inference/models"
            : _options.EmbeddingBaseUrl.Trim().TrimEnd('/');
        return $"{baseUrl}/{ModelName.Trim('/')}/pipeline/feature-extraction";
    }

    private static IReadOnlyList<double> ExtractEmbeddingVector(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var propertyName in new[] { "embedding", "embeddings", "vector" })
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

            if (element.TryGetProperty("data", out var data))
            {
                var vector = ExtractEmbeddingVector(data);
                if (vector.Count > 0)
                {
                    return vector;
                }
            }

            return Array.Empty<double>();
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<double>();
        }

        var direct = new List<double>();
        var rows = new List<IReadOnlyList<double>>();
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
                rows.Add(nested);
            }
        }

        if (direct.Count > 0)
        {
            return direct;
        }

        if (rows.Count == 0)
        {
            return Array.Empty<double>();
        }

        if (rows.Count == 1)
        {
            return rows[0];
        }

        var width = rows.Min(row => row.Count);
        if (width == 0)
        {
            return Array.Empty<double>();
        }

        var averaged = new double[width];
        foreach (var row in rows)
        {
            for (var index = 0; index < width; index++)
            {
                averaged[index] += row[index];
            }
        }

        for (var index = 0; index < width; index++)
        {
            averaged[index] /= rows.Count;
        }

        return averaged;
    }

    private static int InferDimensions(string modelName)
    {
        if (modelName.Contains("Embedding-8B", StringComparison.OrdinalIgnoreCase))
        {
            return 4096;
        }

        if (modelName.Contains("Embedding-4B", StringComparison.OrdinalIgnoreCase))
        {
            return 2560;
        }

        return 1024;
    }

    private sealed record HuggingFaceEmbeddingRequest(
        [property: JsonPropertyName("inputs")] string Inputs,
        [property: JsonPropertyName("options")] HuggingFaceEmbeddingOptions Options);

    private sealed record HuggingFaceEmbeddingOptions([property: JsonPropertyName("wait_for_model")] bool WaitForModel);
}
