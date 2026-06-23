using System.Text.RegularExpressions;
using DataAccessLayer;
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
    private static readonly Regex TermRegex = new(@"\b[A-Z][A-Za-z0-9]{2,}\b|\b[A-Za-z]{4,}\b", RegexOptions.Compiled);

    public CourseWorkspaceModel(
        ILogger<HomePageModelBase> logger,
        IKnowledgeRepository repository,
        IDocumentIndexingService indexingService,
        IWebPageTextExtractor webPageTextExtractor,
        IRagChatService chatService,
        IUserAccountStore users,
        IWebHostEnvironment environment,
        IDocumentIndexJobQueue indexJobQueue)
        : base(logger, repository, indexingService, webPageTextExtractor, chatService, users, environment, indexJobQueue)
    {
    }

    public CourseSubject Subject { get; private set; } = new();
    public IReadOnlyList<IndexedDocument> Documents { get; private set; } = Array.Empty<IndexedDocument>();
    public IReadOnlyList<CourseChapterLearningViewModel> Chapters { get; private set; } = Array.Empty<CourseChapterLearningViewModel>();
    public IReadOnlyList<LearningQuizItemViewModel> Quiz { get; private set; } = Array.Empty<LearningQuizItemViewModel>();
    public IReadOnlyList<FlashcardViewModel> Flashcards { get; private set; } = Array.Empty<FlashcardViewModel>();
    public IReadOnlyList<string> SummaryBullets { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Checklist { get; private set; } = Array.Empty<string>();
    public CourseAnalyticsViewModel Analytics { get; private set; } = CourseAnalyticsViewModel.Empty;
    public bool CanManageCourse { get; private set; }
    public bool CanSeeAnalytics { get; private set; }
    public string? LoadErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var role = CurrentRole();
            var documentScope = BuildDocumentAccessScope(role == AppRoles.Student
                ? DocumentAccessMode.Chat
                : DocumentAccessMode.DocumentUi);
            var documents = await _repository.GetDocumentsAsync(documentScope, null, cancellationToken);
            var catalog = await _repository.GetCourseCatalogAsync(cancellationToken);
            var visibleCatalog = role == AppRoles.Student
                ? BuildSynchronizedCourseCatalogForView(catalog, documents)
                : BuildSynchronizedCourseCatalogForView(FilterCourseCatalogForCurrentUser(catalog), documents);
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
            if (role == AppRoles.Student && subjectDocuments.Count == 0)
            {
                return NotFound();
            }

            var subjectLabels = subjectDocuments
                .Select(document => document.Subject)
                .Append(subject.DisplayName)
                .Append(subject.Code)
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var chunks = await _repository.GetChunksAsync(documentScope, subjectLabels, cancellationToken);
            chunks = chunks
                .Where(chunk => SubjectMatchesFilter(chunk.Subject, subject.DisplayName)
                                || SubjectMatchesFilter(chunk.Subject, subject.Code))
                .OrderBy(chunk => chunk.Chapter)
                .ThenBy(chunk => chunk.ChunkIndex)
                .ToList();

            Subject = subject;
            Documents = subjectDocuments;
            Chapters = BuildChapterLearning(subject, subjectDocuments, chunks);
            SummaryBullets = BuildSummary(chunks, subjectDocuments);
            Flashcards = BuildFlashcards(chunks, subject);
            Quiz = BuildQuiz(chunks, subject);
            Checklist = BuildChecklist(Chapters, subjectDocuments);
            CanManageCourse = await CanManageSubjectAsync(subject.Id, cancellationToken);
            CanSeeAnalytics = role is AppRoles.Admin or AppRoles.Lecturer;
            Analytics = CanSeeAnalytics
                ? await BuildAnalyticsAsync(subject, subjectDocuments, chunks, cancellationToken)
                : BuildStudentAnalytics(subjectDocuments, chunks);

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

    private static IReadOnlyList<string> BuildSummary(IReadOnlyList<DocumentChunk> chunks, IReadOnlyList<IndexedDocument> documents)
    {
        var bullets = chunks
            .Select(chunk => FirstUsefulSentence(chunk.Text))
            .Where(sentence => !string.IsNullOrWhiteSpace(sentence))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        if (bullets.Count > 0)
        {
            return bullets!;
        }

        return documents.Count == 0
            ? new[] { "No indexed document is available for this course yet." }
            : documents.Take(5).Select(document => $"{document.FileName} is available in {document.Chapter}.").ToList();
    }

    private static IReadOnlyList<FlashcardViewModel> BuildFlashcards(IReadOnlyList<DocumentChunk> chunks, CourseSubject subject)
    {
        var cards = new List<FlashcardViewModel>
        {
            new("Course", subject.DisplayName)
        };

        foreach (var chunk in chunks.Take(12))
        {
            var front = string.IsNullOrWhiteSpace(chunk.SectionTitle)
                ? ExtractTerm(chunk.Text)
                : chunk.SectionTitle.Trim();
            var back = FirstUsefulSentence(chunk.Text);
            if (!string.IsNullOrWhiteSpace(front) && !string.IsNullOrWhiteSpace(back)
                && !cards.Any(card => card.Front.Equals(front, StringComparison.OrdinalIgnoreCase)))
            {
                cards.Add(new FlashcardViewModel(front, back));
            }

            if (cards.Count >= 8)
            {
                break;
            }
        }

        return cards;
    }

    private static IReadOnlyList<LearningQuizItemViewModel> BuildQuiz(IReadOnlyList<DocumentChunk> chunks, CourseSubject subject)
    {
        var quiz = new List<LearningQuizItemViewModel>();
        foreach (var chunk in chunks.Take(10))
        {
            var answer = ExtractTerm(chunk.Text);
            var sentence = FirstUsefulSentence(chunk.Text);
            if (string.IsNullOrWhiteSpace(answer) || string.IsNullOrWhiteSpace(sentence))
            {
                continue;
            }

            quiz.Add(new LearningQuizItemViewModel(
                $"Which term is connected to this note: {TrimTo(sentence, 90)}?",
                answer,
                chunk.Chapter));
            if (quiz.Count >= 5)
            {
                break;
            }
        }

        if (quiz.Count == 0)
        {
            quiz.Add(new LearningQuizItemViewModel($"What is {subject.Code} about?", subject.DisplayName, "Overview"));
        }

        return quiz;
    }

    private static IReadOnlyList<string> BuildChecklist(
        IReadOnlyList<CourseChapterLearningViewModel> chapters,
        IReadOnlyList<IndexedDocument> documents)
    {
        var checklist = chapters.Take(8).Select(chapter => $"Review {chapter.Title} and ask one question about it.").ToList();
        if (documents.Count > 0)
        {
            checklist.Insert(0, $"Read {documents.Count} indexed document(s) for this course.");
        }

        checklist.Add("Use the course chat to clarify weak points before moving on.");
        return checklist;
    }

    private async Task<CourseAnalyticsViewModel> BuildAnalyticsAsync(
        CourseSubject subject,
        IReadOnlyList<IndexedDocument> documents,
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken)
    {
        var sessions = await _repository.GetSessionsAsync(cancellationToken);
        var questionStats = ExtractQuestionStats(subject, sessions);
        var learners = questionStats
            .Where(item => !string.IsNullOrWhiteSpace(item.OwnerName) || !string.IsNullOrWhiteSpace(item.OwnerEmail))
            .GroupBy(item => string.IsNullOrWhiteSpace(item.OwnerEmail) ? item.OwnerName : item.OwnerEmail, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CourseLearnerViewModel(
                group.First().OwnerName,
                group.First().OwnerEmail,
                group.Count(),
                group.Max(item => item.AskedAt)))
            .OrderByDescending(item => item.QuestionCount)
            .ThenByDescending(item => item.LastAskedAt)
            .Take(12)
            .ToList();

        var popularQuestions = questionStats
            .GroupBy(item => NormalizeQuestion(item.Question), StringComparer.OrdinalIgnoreCase)
            .Select(group => new PopularQuestionViewModel(group.First().Question, group.Count(), group.Max(item => item.AskedAt)))
            .OrderByDescending(item => item.Count)
            .ThenByDescending(item => item.LastAskedAt)
            .Take(8)
            .ToList();

        var weakChapters = Chapters
            .Select(chapter => new CoverageGapViewModel(
                chapter.Title,
                chapter.DocumentCount,
                chapter.ChunkCount,
                questionStats.Count(question => question.Chapter.Equals(chapter.Title, StringComparison.OrdinalIgnoreCase)),
                chapter.DocumentCount == 0 || chapter.ChunkCount < 2 ? "Needs more indexed material" : "Covered"))
            .Where(item => item.DocumentCount == 0 || item.ChunkCount < 2 || item.QuestionCount == 0)
            .Take(10)
            .ToList();

        return new CourseAnalyticsViewModel(
            sessions.Count,
            questionStats.Count,
            learners.Count,
            documents.Count(document => document.Status != DocumentIndexStatus.Indexed),
            popularQuestions,
            learners,
            weakChapters);
    }

    private CourseAnalyticsViewModel BuildStudentAnalytics(IReadOnlyList<IndexedDocument> documents, IReadOnlyList<DocumentChunk> chunks)
    {
        return new CourseAnalyticsViewModel(
            0,
            0,
            0,
            documents.Count(document => document.Status != DocumentIndexStatus.Indexed),
            Array.Empty<PopularQuestionViewModel>(),
            Array.Empty<CourseLearnerViewModel>(),
            Chapters
                .Where(chapter => chapter.DocumentCount == 0 || chapter.ChunkCount == 0)
                .Select(chapter => new CoverageGapViewModel(chapter.Title, chapter.DocumentCount, chapter.ChunkCount, 0, "Not indexed yet"))
                .ToList());
    }

    private static IReadOnlyList<CourseQuestionStat> ExtractQuestionStats(CourseSubject subject, IReadOnlyList<ChatSession> sessions)
    {
        var stats = new List<CourseQuestionStat>();
        foreach (var session in sessions)
        {
            var messages = session.Messages.OrderBy(message => message.CreatedAt).ToList();
            for (var index = 0; index < messages.Count; index++)
            {
                var message = messages[index];
                if (!message.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var assistant = messages.Skip(index + 1).FirstOrDefault(item => item.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase));
                var citation = assistant?.Citations.FirstOrDefault(item => SubjectMatchesFilter(item.Subject, subject.DisplayName)
                                                                           || SubjectMatchesFilter(item.Subject, subject.Code));
                if (citation is null && !QuestionMentionsSubject(message.Content, subject))
                {
                    continue;
                }

                stats.Add(new CourseQuestionStat(
                    message.Content,
                    citation?.Chapter ?? string.Empty,
                    session.OwnerName,
                    session.OwnerEmail,
                    message.CreatedAt));
            }
        }

        return stats;
    }

    private static bool QuestionMentionsSubject(string question, CourseSubject subject)
    {
        return question.Contains(subject.Code, StringComparison.OrdinalIgnoreCase)
               || (!string.IsNullOrWhiteSpace(subject.Name) && question.Contains(subject.Name, StringComparison.OrdinalIgnoreCase));
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

    private static string ExtractTerm(string text)
    {
        return TermRegex.Matches(text ?? string.Empty)
            .Select(match => match.Value.Trim())
            .Where(term => term.Length >= 4 && !term.Equals("This", StringComparison.OrdinalIgnoreCase))
            .GroupBy(term => term, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenByDescending(group => group.Key.Length)
            .Select(group => group.Key)
            .FirstOrDefault() ?? string.Empty;
    }

    private static string NormalizeQuestion(string question)
    {
        return Regex.Replace((question ?? string.Empty).Trim().ToLowerInvariant(), @"\s+", " ");
    }

    private static string TrimTo(string value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength].TrimEnd() + "...";
    }

    private sealed record CourseQuestionStat(string Question, string Chapter, string OwnerName, string OwnerEmail, DateTimeOffset AskedAt);
}

public sealed record CourseChapterLearningViewModel(string Title, int DocumentCount, int ChunkCount, string Summary);
public sealed record LearningQuizItemViewModel(string Question, string Answer, string Chapter);
public sealed record FlashcardViewModel(string Front, string Back);
public sealed record PopularQuestionViewModel(string Question, int Count, DateTimeOffset LastAskedAt);
public sealed record CourseLearnerViewModel(string Name, string Email, int QuestionCount, DateTimeOffset LastAskedAt);
public sealed record CoverageGapViewModel(string Chapter, int DocumentCount, int ChunkCount, int QuestionCount, string Recommendation);
public sealed record CourseAnalyticsViewModel(
    int SessionCount,
    int QuestionCount,
    int LearnerCount,
    int NonIndexedDocumentCount,
    IReadOnlyList<PopularQuestionViewModel> PopularQuestions,
    IReadOnlyList<CourseLearnerViewModel> Learners,
    IReadOnlyList<CoverageGapViewModel> CoverageGaps)
{
    public static CourseAnalyticsViewModel Empty { get; } = new(
        0,
        0,
        0,
        0,
        Array.Empty<PopularQuestionViewModel>(),
        Array.Empty<CourseLearnerViewModel>(),
        Array.Empty<CoverageGapViewModel>());
}
