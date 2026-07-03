using System.Text.RegularExpressions;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;

namespace PresentationLayer.Pages.Home;

[Authorize(Policy = AuthorizationPolicies.ChatAccess)]
public sealed class CourseWorkspaceModel : HomePageModelBase
{
    private static readonly Regex SentenceRegex = new(@"(?<=[.!?。])\s+|\r?\n+", RegexOptions.Compiled);
    public CourseWorkspaceModel(
        ILogger<HomePageModelBase> logger,
        IKnowledgeService knowledge,
        IDocumentIndexingService indexingService,
        IWebPageTextExtractor webPageTextExtractor,
        IRagChatService chatService,
        IUserAccountStore users,
        IWebHostEnvironment environment,
        IDocumentIndexJobQueue indexJobQueue)
        : base(logger, knowledge, indexingService, webPageTextExtractor, chatService, users, environment, indexJobQueue)
    {
    }

    public CourseSubject Subject { get; private set; } = new();
    public IReadOnlyList<IndexedDocument> Documents { get; private set; } = Array.Empty<IndexedDocument>();
    public IReadOnlyList<CourseChapterLearningViewModel> Chapters { get; private set; } = Array.Empty<CourseChapterLearningViewModel>();
    public bool CanManageCourse { get; private set; }
    public string? LoadErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var role = CurrentRole();
            if (role == AppRoles.Student)
            {
                return Forbid();
            }

            var documentScope = BuildDocumentAccessScope(DocumentAccessMode.DocumentUi);
            var documents = await _knowledge.GetDocumentsAsync(documentScope, null, cancellationToken);
            var catalog = await _knowledge.GetCourseCatalogAsync(cancellationToken);
            var visibleCatalog = BuildSynchronizedCourseCatalogForView(FilterCourseCatalogForCurrentUser(catalog), documents);
            var subject = visibleCatalog.FirstOrDefault(item => item.Id == id);
            if (subject is null)
            {
                return NotFound();
            }

            var subjectDocuments = documents
                .Where(document => SubjectMatchesFilter(document.Subject, subject.DisplayName)
                                   || SubjectMatchesFilter(document.Subject, subject.Code))
                .OrderBy(document => document.Chapter)
                .ThenByDescending(document => document.UploadedAt)
                .ToList();
            var subjectLabels = subjectDocuments
                .Select(document => document.Subject)
                .Append(subject.DisplayName)
                .Append(subject.Code)
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var chunks = await _knowledge.GetChunksAsync(documentScope, subjectLabels, cancellationToken);
            chunks = chunks
                .Where(chunk => SubjectMatchesFilter(chunk.Subject, subject.DisplayName)
                                || SubjectMatchesFilter(chunk.Subject, subject.Code))
                .OrderBy(chunk => chunk.Chapter)
                .ThenBy(chunk => chunk.ChunkIndex)
                .ToList();

            Subject = subject;
            Documents = subjectDocuments;
            Chapters = BuildChapterLearning(subject, subjectDocuments, chunks);
            CanManageCourse = await CanManageSubjectAsync(subject.Id, cancellationToken);

            return Page();
        }
        catch (Exception ex) when (IsDataAccessTimeout(ex))
        {
            _logger.LogWarning(ex, "Course workspace could not load because the database was unavailable.");
            LoadErrorMessage = "Database unavailable/timeout. Course workspace could not be loaded.";
            return Page();
        }
    }

    private static IReadOnlyList<CourseChapterLearningViewModel> BuildChapterLearning(
        CourseSubject subject,
        IReadOnlyList<IndexedDocument> documents,
        IReadOnlyList<DocumentChunk> chunks)
    {
        var chapterNames = subject.Chapters
            .Select(chapter => chapter.Title)
            .Concat(documents.Select(document => document.Chapter))
            .Concat(chunks.Select(chunk => chunk.Chapter))
            .Where(chapter => !string.IsNullOrWhiteSpace(chapter))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(chapter => chapter)
            .ToList();

        return chapterNames.Select(chapter => new CourseChapterLearningViewModel(
            chapter,
            documents.Count(document => document.Chapter.Equals(chapter, StringComparison.OrdinalIgnoreCase)),
            chunks.Count(chunk => chunk.Chapter.Equals(chapter, StringComparison.OrdinalIgnoreCase)),
            FirstUsefulSentence(chunks.FirstOrDefault(chunk => chunk.Chapter.Equals(chapter, StringComparison.OrdinalIgnoreCase))?.Text) ?? "No indexed summary yet."))
            .ToList();
    }

    private static string? FirstUsefulSentence(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return SentenceRegex.Split(text.Trim())
            .Select(sentence => sentence.Trim())
            .FirstOrDefault(sentence => sentence.Length >= 35) is { } found
            ? TrimTo(found, 180)
            : TrimTo(text.Trim(), 180);
    }

    private static string TrimTo(string value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength].TrimEnd() + "...";
    }

}

public sealed record CourseChapterLearningViewModel(string Title, int DocumentCount, int ChunkCount, string Summary);
