using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ServicesLayer;

public interface IEmbeddingService
{
    string ModelName { get; }
    int Dimensions { get; }
    Task<Dictionary<int, double>> EmbedAsync(string text, CancellationToken cancellationToken = default);
    double CosineSimilarity(IReadOnlyDictionary<int, double> left, IReadOnlyDictionary<int, double> right);
}

public sealed class FallbackEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingService _primary;
    private readonly IEmbeddingService _fallback;

    public FallbackEmbeddingService(IEmbeddingService primary, IEmbeddingService fallback)
    {
        _primary = primary;
        _fallback = fallback;
    }

    public string ModelName => _primary.ModelName;
    public int Dimensions => _primary.Dimensions;

    public async Task<Dictionary<int, double>> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.EmbedAsync(text, cancellationToken);
        }
        catch (Exception ex) when (ShouldFallback(ex, cancellationToken))
        {
            return await _fallback.EmbedAsync(text, cancellationToken);
        }
    }

    public double CosineSimilarity(IReadOnlyDictionary<int, double> left, IReadOnlyDictionary<int, double> right)
    {
        return _fallback.CosineSimilarity(left, right);
    }

    private static bool ShouldFallback(Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        return exception is InvalidOperationException or HttpRequestException or TaskCanceledException;
    }
}

public sealed class HashingEmbeddingService : IEmbeddingService
{
    private const int DimensionsValue = 512;

    private static readonly Regex TokenRegex = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "as", "at", "be", "by", "for", "from", "in", "is", "it", "of", "on", "or", "that", "the", "to",
        "va", "la", "cua", "cho", "trong", "khi", "voi", "mot", "cac", "nhung", "duoc", "tu", "theo", "nay", "do", "thi", "o"
    };

    string IEmbeddingService.ModelName => "hashing-embedding-512";
    int IEmbeddingService.Dimensions => DimensionsValue;

    public Task<Dictionary<int, double>> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(EmbedWithHashing(text));
    }

    public Dictionary<int, double> EmbedWithHashing(string text)
    {
        var vector = new Dictionary<int, double>();
        foreach (var token in Tokenize(text))
        {
            var hash = BitConverter.ToUInt32(SHA256.HashData(Encoding.UTF8.GetBytes(token)), 0);
            var index = (int)(hash % DimensionsValue);
            vector[index] = vector.GetValueOrDefault(index) + 1d;
        }

        var norm = Math.Sqrt(vector.Values.Sum(value => value * value));
        if (norm == 0)
        {
            return vector;
        }

        foreach (var key in vector.Keys.ToList())
        {
            vector[key] /= norm;
        }

        return vector;
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

    private static IEnumerable<string> Tokenize(string text)
    {
        var normalized = RemoveDiacritics(text).ToLowerInvariant();
        foreach (Match match in TokenRegex.Matches(normalized))
        {
            var token = match.Value.Trim();
            if (token.Length >= 2 && !StopWords.Contains(token))
            {
                yield return token;
            }
        }
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character == '\u0111' || character == '\u0110' ? 'd' : character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

internal static class EmbeddingVector
{
    public static Dictionary<int, double> NormalizeDenseEmbedding(IReadOnlyList<double> embedding)
    {
        var norm = Math.Sqrt(embedding.Sum(value => value * value));
        var vector = new Dictionary<int, double>(embedding.Count);
        if (norm == 0)
        {
            return vector;
        }

        for (var index = 0; index < embedding.Count; index++)
        {
            var value = embedding[index] / norm;
            if (Math.Abs(value) > 0)
            {
                vector[index] = value;
            }
        }

        return vector;
    }
}
