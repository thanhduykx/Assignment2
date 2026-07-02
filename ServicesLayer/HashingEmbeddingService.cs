namespace ServicesLayer;

public sealed class HashingEmbeddingService : IEmbeddingService
{
    public string ModelName => "hashing-v1";
    public int Dimensions => 256;

    public Task<Dictionary<int, double>> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<int, double>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(result);
        }

        for (var i = 0; i < text.Length; i++)
        {
            var key = i % Dimensions;
            result[key] = result.TryGetValue(key, out var value) ? value + (text[i] * 0.01) : text[i] * 0.01;
        }

        var magnitude = Math.Sqrt(result.Values.Sum(v => v * v));
        if (magnitude > 0)
        {
            foreach (var key in result.Keys.ToList())
            {
                result[key] /= magnitude;
            }
        }

        return Task.FromResult(result);
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
}