using DataAccessLayer;

namespace ServicesLayer;

public sealed record ChatChunkRerankResult(int CandidateNumber, double Score, string Reason);

public enum ChatQueryIntent
{
    SmallTalk,
    DocumentQuestion,
    ExternalQuestion,
    Unsafe
}

public sealed record QueryIntentDecision(ChatQueryIntent Intent, double Confidence, string Reason);

public sealed record GroundingDecision(bool IsGrounded, double Confidence, string Reason);

public interface ILocalChatCompletionService
{
    bool IsEnabled { get; }

    Task<QueryIntentDecision> ClassifyQuestionAsync(
        string question,
        IReadOnlyList<ChatMessage> history,
        string language,
        CancellationToken cancellationToken = default);

    Task<string> RewriteQuestionAsync(
        string question,
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> RewriteQueriesAsync(
        string question,
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatChunkRerankResult>> RerankChunksAsync(
        string question,
        IReadOnlyList<DocumentChunk> chunks,
        string language,
        CancellationToken cancellationToken = default);

    Task<string?> GenerateAnswerAsync(
        string question,
        string subject,
        IReadOnlyList<ChatMessage> history,
        IReadOnlyList<DocumentChunk> chunks,
        string language,
        CancellationToken cancellationToken = default);

    Task<GroundingDecision?> ValidateGroundingAsync(
        string question,
        string answer,
        IReadOnlyList<DocumentChunk> chunks,
        string language,
        CancellationToken cancellationToken = default);
}

internal static class ChatPromptBuilder
{
    public static string BuildVietnameseSystemPrompt(string subject)
    {
        var subjectName = string.IsNullOrWhiteSpace(subject) ? "mon hoc" : subject.Trim();
        return $"""
            Ban la chatbot ho tro sinh vien mon {subjectName}.
            Chi tra loi bang thong tin co trong cac document chunks duoc cung cap.
            Khong dung kien thuc ngoai tai lieu, khong doan, khong them so lieu hoac quy dinh khong co trong nguon.
            Neu chunks khong du can cu de tra loi truc tiep, chi tra loi dung cau:
            "Minh khong du du lieu trong tai lieu de tra loi cau hoi nay."
            Tra loi bang tieng Viet, ngan gon, ro y, co the dien giai lai thay vi chep nguyen van.
            """;
    }
}
