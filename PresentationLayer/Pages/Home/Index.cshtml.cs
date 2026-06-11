using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.Models;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;

namespace PresentationLayer.Pages.Home;

[Authorize(Policy = AuthorizationPolicies.DocumentRead)]
public sealed class IndexModel : HomePageModelBase
{
    public IndexModel(
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

    public IReadOnlyList<IndexedDocument> Documents { get; private set; } = Array.Empty<IndexedDocument>();
    public IReadOnlyList<CourseSubject> CourseCatalog { get; private set; } = Array.Empty<CourseSubject>();
    public IReadOnlyList<UserOptionViewModel> LecturerOptions { get; private set; } = Array.Empty<UserOptionViewModel>();
    public IReadOnlyList<string> DocumentSubjectOptions { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> DocumentChapterOptions { get; private set; } = Array.Empty<string>();
    public string? SubjectFilter { get; private set; }
    public new bool IsAdmin { get; private set; }
    public new bool IsLecturer { get; private set; }
    public int TotalDocumentCount { get; private set; }
    public int TotalChunkCount { get; private set; }
    public long TotalUploadedBytes { get; private set; }
    public int IndexedDocumentCount { get; private set; }
    public int ProcessingDocumentCount { get; private set; }
    public int FailedDocumentCount { get; private set; }
    public double AverageChunksPerIndexedDocument { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? subjectFilter, CancellationToken cancellationToken)
    {
        var allDocuments = await _indexingService.GetDocumentsAsync(cancellationToken);
        if (CanManageDocuments())
        {
            await SyncCourseCatalogFromDocumentsAsync(allDocuments, cancellationToken);
        }

        var allCourseCatalog = await _repository.GetCourseCatalogAsync(cancellationToken);
        var currentUser = await GetCurrentUserAccountAsync(cancellationToken);
        var accessibleDocuments = FilterDocumentsForCurrentUser(allDocuments, allCourseCatalog, currentUser).ToList();
        var courseCatalog = BuildSynchronizedCourseCatalogForView(
            FilterCourseCatalogForCurrentUser(allCourseCatalog),
            accessibleDocuments);
        var normalizedSubjectFilter = subjectFilter?.Trim();
        var documents = string.IsNullOrWhiteSpace(normalizedSubjectFilter)
            ? accessibleDocuments
            : accessibleDocuments
                .Where(document => SubjectMatchesFilter(document.Subject, normalizedSubjectFilter))
                .ToList();
        var userIsAdmin = base.IsAdmin();
        var userIsLecturer = base.IsLecturer();
        var lecturers = userIsAdmin
            ? await _users.GetByRoleAsync(AppRoles.Lecturer, cancellationToken)
            : Array.Empty<UserAccount>();
        var indexedDocuments = accessibleDocuments
            .Where(document => document.Status == DocumentIndexStatus.Indexed)
            .ToList();

        Documents = documents;
        CourseCatalog = courseCatalog;
        LecturerOptions = lecturers.Select(ToUserOption).ToList();
        DocumentSubjectOptions = accessibleDocuments
            .Select(document => document.Subject)
            .Where(subject => !string.IsNullOrWhiteSpace(subject))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(subject => subject)
            .ToList();
        DocumentChapterOptions = accessibleDocuments
            .Select(document => document.Chapter)
            .Where(chapter => !string.IsNullOrWhiteSpace(chapter))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(chapter => chapter)
            .ToList();
        SubjectFilter = normalizedSubjectFilter;
        IsAdmin = userIsAdmin;
        IsLecturer = userIsLecturer;
        TotalDocumentCount = accessibleDocuments.Count;
        TotalChunkCount = accessibleDocuments.Sum(document => document.ChunkCount);
        TotalUploadedBytes = accessibleDocuments.Sum(document => document.FileSizeBytes);
        IndexedDocumentCount = indexedDocuments.Count;
        ProcessingDocumentCount = accessibleDocuments.Count(document => document.Status == DocumentIndexStatus.Processing);
        FailedDocumentCount = accessibleDocuments.Count(document => document.Status == DocumentIndexStatus.Failed);
        AverageChunksPerIndexedDocument = indexedDocuments.Count == 0
            ? 0
            : indexedDocuments.Average(document => document.ChunkCount);

        return Page();
    }

    public async Task<IActionResult> OnPostSaveSubjectAsync([FromForm] SubjectCatalogViewModel model, CancellationToken cancellationToken)
    {
        if (!base.IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var ownerInfo = await BuildSubjectOwnerInfoAsync(model.OwnerUserId, cancellationToken);
            await _repository.UpsertSubjectAsync(model.Id, model.Code, model.Name, model.Description, cancellationToken, ownerInfo);
            TempData["Success"] = "Đã lưu môn học.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ToVietnameseCatalogError(ex.Message);
        }

        return RedirectToPage("/Home/Index");
    }

    public async Task<IActionResult> OnPostDeleteSubjectAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!base.IsAdmin())
        {
            return Forbid();
        }

        await _repository.DeleteSubjectAsync(id, cancellationToken);
        TempData["Success"] = "Đã xóa môn học.";
        return RedirectToPage("/Home/Index");
    }

    public async Task<IActionResult> OnPostSaveChapterAsync([FromForm] ChapterCatalogViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            if (!await CanManageSubjectAsync(model.SubjectId, cancellationToken))
            {
                return Forbid();
            }

            await _repository.UpsertChapterAsync(model.Id, model.SubjectId, model.Title, model.SortOrder, cancellationToken);
            TempData["Success"] = "Đã lưu chương.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ToVietnameseCatalogError(ex.Message);
        }

        return RedirectToPage("/Home/Index");
    }

    public async Task<IActionResult> OnPostDeleteChapterAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanManageChapterAsync(id, cancellationToken))
        {
            return Forbid();
        }

        await _repository.DeleteChapterAsync(id, cancellationToken);
        TempData["Success"] = "Đã xóa chương.";
        return RedirectToPage("/Home/Index");
    }

    public async Task<IActionResult> OnPostUploadAsync([FromForm] DocumentUploadViewModel model, CancellationToken cancellationToken)
    {
        var isVietnamese = model.Language?.Equals("vi", StringComparison.OrdinalIgnoreCase) == true;
        if ((model.File is null || model.File.Length == 0) && string.IsNullOrWhiteSpace(model.SourceUrl))
        {
            TempData["Error"] = isVietnamese
                ? "Hãy chọn file PDF, DOCX, PPTX, TXT hoặc nhập URL trang bài giảng trước khi index."
                : "Choose a PDF, DOCX, PPTX, TXT file or enter a web page URL before indexing.";
            return RedirectToPage("/Home/Index");
        }

        try
        {
            if (string.IsNullOrWhiteSpace(model.Subject) || string.IsNullOrWhiteSpace(model.Chapter))
            {
                TempData["Error"] = isVietnamese
                    ? "Subject va Chapter la bat buoc khi upload tai lieu."
                    : "Subject and Chapter are required when uploading a document.";
                return RedirectToPage("/Home/Index");
            }

            if (!await CanManageSubjectAsync(model.Subject, cancellationToken))
            {
                return Forbid();
            }

            DocumentUploadResult result;
            var uploader = BuildDocumentUploaderInfo();
            if (model.File is { Length: > 0 })
            {
                await using var stream = model.File.OpenReadStream();
                result = await _indexingService.QueueFileAsync(
                    stream,
                    model.File.FileName,
                    model.File.ContentType,
                    model.Subject,
                    model.Chapter,
                    Path.Combine(_environment.ContentRootPath, "App_Data", "uploads"),
                    uploader,
                    cancellationToken);
            }
            else
            {
                var extracted = await _webPageTextExtractor.ExtractAsync(model.SourceUrl ?? string.Empty, cancellationToken);
                var sourceName = $"{extracted.Title} - {new Uri(extracted.SourceUrl).Host}.txt";
                result = await _indexingService.QueueTextAsync(
                    extracted.Text,
                    sourceName,
                    extracted.UsedBrowserRenderer ? "text/html+playwright" : "text/html",
                    model.Subject,
                    model.Chapter,
                    Path.Combine(_environment.ContentRootPath, "App_Data", "uploads"),
                    uploader,
                    cancellationToken);
            }

            await _indexJobQueue.EnqueueAsync(result.DocumentId, cancellationToken);

            TempData["Success"] = isVietnamese
                ? "Da nhan tai lieu va dang index."
                : "The document has been queued for indexing.";

            var indexedDocument = await _repository.GetDocumentAsync(result.DocumentId, cancellationToken);
            if (indexedDocument is not null)
            {
                await SyncCourseCatalogFromDocumentsAsync(new[] { indexedDocument }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Document upload failed");
            TempData["Error"] = isVietnamese ? ToVietnameseUploadError(ex.Message) : ex.Message;
        }

        return RedirectToPage("/Home/Index");
    }

    public async Task<IActionResult> OnPostDeleteDocumentAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _repository.GetDocumentAsync(id, cancellationToken);
        if (document is null)
        {
            TempData["Error"] = "Không tìm thấy tài liệu để xóa.";
            return RedirectToPage("/Home/Index");
        }

        if (!await CanManageDocumentAsync(document, cancellationToken))
        {
            return Forbid();
        }

        await _repository.DeleteDocumentAsync(id, cancellationToken);
        TryDeleteStoredFile(document);
        TempData["Success"] = $"Đã xóa tài liệu {document.FileName}.";
        return RedirectToPage("/Home/Index");
    }
}
