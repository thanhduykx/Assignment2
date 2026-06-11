using System.Text;
using System.Text.RegularExpressions;

namespace ServicesLayer;

public sealed record ChunkRetrievalEnrichmentContext(
    string FileName,
    string Subject,
    string Chapter,
    string SectionTitle);

public sealed record ChunkRetrievalEnrichmentResult(
    string EmbeddingText,
    bool UsedAi,
    string StrategyName);

public interface IChunkRetrievalEnrichmentService
{
    string StrategyName { get; }

    Task<ChunkRetrievalEnrichmentResult> BuildEmbeddingTextAsync(
        TextChunk chunk,
        ChunkRetrievalEnrichmentContext context,
        CancellationToken cancellationToken = default);
}

public sealed class AiChunkRetrievalEnrichmentService : IChunkRetrievalEnrichmentService
{
    private static readonly Regex TokenRegex = new(@"[\p{L}\p{N}]{3,}", RegexOptions.Compiled);

    private static readonly string[] UnsafeHintSignals =
    {
        "ignore previous",
        "disregard previous",
        "system prompt",
        "developer message",
        "hidden instruction",
        "jailbreak",
        "bypass",
        "do not follow",
        "forget all",
        "bo qua",
        "mac ke",
        "quen tat ca",
        "khong can tuan thu",
        "khong can theo",
        "tra loi ngoai tai lieu"
    };

    private readonly ILocalChatCompletionService _chatCompletionService;

    public AiChunkRetrievalEnrichmentService(ILocalChatCompletionService chatCompletionService)
    {
        _chatCompletionService = chatCompletionService;
    }

    public string StrategyName => "ai-retrieval-enrichment-v1";

    public async Task<ChunkRetrievalEnrichmentResult> BuildEmbeddingTextAsync(
        TextChunk chunk,
        ChunkRetrievalEnrichmentContext context,
        CancellationToken cancellationToken = default)
    {
        var deterministicText = BuildDeterministicEmbeddingText(chunk, context);
        var hints = await TryGenerateHintsAsync(chunk, context, cancellationToken);
        if (!IsUsableHint(hints, chunk.Text))
        {
            return new ChunkRetrievalEnrichmentResult(deterministicText, false, StrategyName);
        }

        var embeddingText = new StringBuilder(deterministicText);
        embeddingText.AppendLine();
        embeddingText.AppendLine("AI retrieval hints (non-authoritative, search only):");
        embeddingText.AppendLine(hints!.Trim());
        embeddingText.AppendLine();
        embeddingText.AppendLine("Authority: answer generation must use Original chunk text only.");

        return new ChunkRetrievalEnrichmentResult(embeddingText.ToString(), true, StrategyName);
    }

    private async Task<string?> TryGenerateHintsAsync(
        TextChunk chunk,
        ChunkRetrievalEnrichmentContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _chatCompletionService.GenerateChunkRetrievalHintsAsync(
                chunk.Text,
                context.FileName,
                context.Subject,
                context.Chapter,
                context.SectionTitle,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildDeterministicEmbeddingText(TextChunk chunk, ChunkRetrievalEnrichmentContext context)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Embedding source: original chunk plus retrieval metadata.");
        AppendLineIfPresent(builder, "File", context.FileName);
        AppendLineIfPresent(builder, "Subject", context.Subject);
        AppendLineIfPresent(builder, "Chapter", context.Chapter);
        AppendLineIfPresent(builder, "Section", context.SectionTitle);
        builder.AppendLine($"Chunk index: {chunk.ChunkIndex}");
        builder.AppendLine();
        builder.AppendLine("Original chunk:");
        builder.AppendLine(TextEncodingHelper.NormalizeForIndexing(chunk.Text));
        return builder.ToString();
    }

    private static void AppendLineIfPresent(StringBuilder builder, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendLine($"{label}: {value.Trim()}");
        }
    }

    private static bool IsUsableHint(string? hints, string sourceText)
    {
        if (string.IsNullOrWhiteSpace(hints))
        {
            return false;
        }

        var trimmed = hints.Trim();
        if (trimmed.Length < 20 || trimmed.Length > 1500)
        {
            return false;
        }

        if (trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            return false;
        }

        if (trimmed.Contains("```", StringComparison.Ordinal))
        {
            return false;
        }

        if (UnsafeHintSignals.Any(signal => trimmed.Contains(signal, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return HasVocabularyOverlap(trimmed, sourceText);
    }

    private static bool HasVocabularyOverlap(string hints, string sourceText)
    {
        var sourceTokens = Tokenize(sourceText);
        if (sourceTokens.Count < 3)
        {
            return true;
        }

        var hintTokens = Tokenize(hints);
        if (hintTokens.Count == 0)
        {
            return false;
        }

        var overlapCount = hintTokens.Count(sourceTokens.Contains);
        return overlapCount >= Math.Min(3, Math.Max(1, sourceTokens.Count / 18));
    }

    private static HashSet<string> Tokenize(string text)
    {
        return TokenRegex
            .Matches(text ?? string.Empty)
            .Select(match => match.Value.ToLowerInvariant())
            .Where(token => token.Length >= 3)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
