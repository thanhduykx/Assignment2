namespace ServicesLayer;

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

    public async Task<Dictionary<int, double>> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.EmbedAsync(text, cancellationToken);
        }
        catch (Exception exception) when (ShouldFallback(exception, cancellationToken))
        {
            return await _fallback.EmbedAsync(text, cancellationToken);
        }
    }

    public double CosineSimilarity(
        IReadOnlyDictionary<int, double> left,
        IReadOnlyDictionary<int, double> right)
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
