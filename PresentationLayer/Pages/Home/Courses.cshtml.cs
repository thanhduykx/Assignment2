using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;

namespace PresentationLayer.Pages.Home;

[Authorize(Policy = AuthorizationPolicies.ChatAccess)]
public sealed class CoursesModel : HomePageModelBase
{
    public CoursesModel(
        ILogger<HomePageModelBase> logger,
        IKnowledgeService repository,
        IDocumentIndexingService indexingService,
        IWebPageTextExtractor webPageTextExtractor,
        IRagChatService chatService,
        IUserAccountStore users,
        IWebHostEnvironment environment,
        IDocumentIndexJobQueue indexJobQueue)
        : base(logger, repository, indexingService, webPageTextExtractor, chatService, users, environment, indexJobQueue)
    {
    }

    public IReadOnlyList<CourseWorkspaceCardViewModel> Courses { get; private set; } = Array.Empty<CourseWorkspaceCardViewModel>();
    public string? LoadErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (CurrentRole() == AppRoles.Student)
        {
            return Forbid();
        }

        try
        {
            var scope = BuildDocumentAccessScope(DocumentAccessMode.DocumentUi);
            var documents = await _repository.GetDocumentsAsync(scope, null, cancellationToken);
            var catalog = await _repository.GetCourseCatalogAsync(cancellationToken);
            var visibleCatalog = BuildSynchronizedCourseCatalogForView(FilterCourseCatalogForCurrentUser(catalog), documents);

            Courses = visibleCatalog
                .Select(subject =>
                {
                    var subjectDocuments = documents
                        .Where(document => SubjectMatchesFilter(document.Subject, subject.DisplayName)
                                           || SubjectMatchesFilter(document.Subject, subject.Code))
                        .ToList();
                    return new CourseWorkspaceCardViewModel(
                        subject.Id,
                        subject.Code,
                        subject.DisplayName,
                        subject.Description,
                        string.IsNullOrWhiteSpace(subject.OwnerName) ? "Unassigned" : subject.OwnerName,
                        subject.Chapters.Count,
                        subjectDocuments.Count,
                        subjectDocuments.Count(document => document.Status == DocumentIndexStatus.Indexed));
                })
                .OrderBy(course => course.Code)
                .ToList();

            return Page();
        }
        catch (Exception ex) when (IsDataAccessTimeout(ex))
        {
            _logger.LogWarning(ex, "Courses page could not load because the database was unavailable.");
            Courses = Array.Empty<CourseWorkspaceCardViewModel>();
            LoadErrorMessage = "Database unavailable/timeout. Course workspaces could not be loaded.";
            return Page();
        }
    }
}

public sealed record CourseWorkspaceCardViewModel(
    Guid Id,
    string Code,
    string DisplayName,
    string Description,
    string OwnerName,
    int ChapterCount,
    int DocumentCount,
    int IndexedDocumentCount);
