using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.Models;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;

namespace PresentationLayer.Pages.Home;

[Authorize(Policy = AuthorizationPolicies.DocumentManagement)]
public sealed class SubjectDocumentsModel : HomePageModelBase
{
    private readonly IDocumentStatusNotifier _documentStatusNotifier;

    public SubjectDocumentsModel(
        ILogger<HomePageModelBase> logger,
        IKnowledgeService knowledge,
        IDocumentIndexingService indexingService,
        IWebPageTextExtractor webPageTextExtractor,
        IRagChatService chatService,
        IUserAccountStore users,
        IWebHostEnvironment environment,
        IDocumentIndexJobQueue indexJobQueue,
        IDocumentStatusNotifier documentStatusNotifier)
        : base(logger, knowledge, indexingService, webPageTextExtractor, chatService, users, environment, indexJobQueue)
    {
        _documentStatusNotifier = documentStatusNotifier;
    }

    public DocumentTreeSubjectViewModel Subject { get; private set; } = new();
    public bool IsRealCatalogSubject { get; private set; }
    public new bool IsAdmin { get; private set; }
    public new bool IsLecturer { get; private set; }
    public string? LoadErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await LoadPageAsync(id, cancellationToken);
    }

    public async Task<IActionResult> OnPostSaveSubjectAsync(Guid id, [FromForm] SubjectCatalogViewModel model, CancellationToken cancellationToken)
    {
        if (!base.IsAdmin())
        {
            return Forbid();
        }

        try
        {
            await _knowledge.UpsertSubjectAsync(model.Id, model.Code, model.Code, model.Description, cancellationToken);
            TempData["Success"] = "Da luu mon hoc.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ToVietnameseCatalogError(ex.Message);
        }

        return RedirectToPage("/Home/SubjectDocuments", new { id });
    }

    public async Task<IActionResult> OnPostDeleteSubjectAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!base.IsAdmin())
        {
            return Forbid();
        }

        await _knowledge.DeleteSubjectAsync(id, cancellationToken);
        TempData["Success"] = "Da xoa mon hoc.";
        return RedirectToPage("/Home/Index");
    }

    public async Task<IActionResult> OnPostSaveChapterAsync(Guid id, [FromForm] ChapterCatalogViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            if (!await CanManageSubjectAsync(model.SubjectId, cancellationToken))
            {
                return Forbid();
            }

            await _knowledge.UpsertChapterAsync(model.Id, model.SubjectId, model.Title, model.SortOrder, cancellationToken);
            TempData["Success"] = "Da luu chuong.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ToVietnameseCatalogError(ex.Message);
        }

        return RedirectToPage("/Home/SubjectDocuments", new { id });
    }

    public async Task<IActionResult> OnPostDeleteChapterAsync(Guid id, Guid chapterId, CancellationToken cancellationToken)
    {
        if (!await CanManageChapterAsync(chapterId, cancellationToken))
        {
            return Forbid();
        }

        await _knowledge.DeleteChapterAsync(chapterId, cancellationToken);
        TempData["Success"] = "Da xoa chuong.";
        return RedirectToPage("/Home/SubjectDocuments", new { id });
    }

    public async Task<IActionResult> OnPostUploadAsync(Guid id, [FromForm] DocumentUploadViewModel model, CancellationToken cancellationToken)
    {
        if (base.IsAdmin())
        {
            TempData["Error"] = "Admin không được phép upload tài liệu. Chỉ giảng viên mới có quyền upload.";
            return RedirectToPage("/Home/SubjectDocuments", new { id });
        }

        if ((model.File is null || model.File.Length == 0) && string.IsNullOrWhiteSpace(model.SourceUrl))
        {
            TempData["Error"] = "Hay chon file PDF, DOCX, PPTX, TXT hoac nhap URL bai giang truoc khi index.";
            return RedirectToPage("/Home/SubjectDocuments", new { id });
        }

        try
        {
            if (string.IsNullOrWhiteSpace(model.Subject) || string.IsNullOrWhiteSpace(model.Chapter))
            {
                TempData["Error"] = "Mon hoc va chuong/muc la bat buoc khi upload tai lieu.";
                return RedirectToPage("/Home/SubjectDocuments", new { id });
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

            TempData["Success"] = "Da nhan tai lieu va dang index.";
            var indexedDocument = await _knowledge.GetDocumentAsync(result.DocumentId, cancellationToken);
            if (indexedDocument is not null)
            {
                await SyncCourseCatalogFromDocumentsAsync(new[] { indexedDocument }, cancellationToken);
                await _documentStatusNotifier.NotifyDocumentStatusChangedAsync(indexedDocument, CancellationToken.None);
            }

            await _indexJobQueue.EnqueueAsync(result.DocumentId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Document upload failed from subject detail page");
            TempData["Error"] = ToVietnameseUploadError(ex.Message);
        }

        return RedirectToPage("/Home/SubjectDocuments", new { id });
    }

    public async Task<IActionResult> OnPostDeleteDocumentAsync(Guid id, Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _knowledge.GetDocumentAsync(documentId, cancellationToken);
        if (document is null)
        {
            TempData["Error"] = "Khong tim thay tai lieu de xoa.";
            return RedirectToPage("/Home/SubjectDocuments", new { id });
        }

        if (!await CanManageDocumentAsync(document, cancellationToken))
        {
            return Forbid();
        }

        await _knowledge.DeleteDocumentAsync(documentId, cancellationToken);
        TryDeleteStoredFile(document);
        TempData["Success"] = $"Da xoa tai lieu {document.FileName}.";
        return RedirectToPage("/Home/SubjectDocuments", new { id });
    }

    public async Task<IActionResult> OnPostReindexDocumentAsync(Guid id, Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _knowledge.GetDocumentAsync(documentId, cancellationToken);
        if (document is null)
        {
            TempData["Error"] = "Khong tim thay tai lieu de re-index.";
            return RedirectToPage("/Home/SubjectDocuments", new { id });
        }

        if (!await CanManageDocumentAsync(document, cancellationToken))
        {
            return Forbid();
        }

        await _knowledge.MarkDocumentIndexProcessingAsync(documentId, cancellationToken);
        await _documentStatusNotifier.NotifyDocumentStatusChangedAsync(documentId, CancellationToken.None);
        await _indexJobQueue.EnqueueAsync(documentId, cancellationToken);
        TempData["Success"] = $"Da dua {document.FileName} vao hang doi re-index.";
        return RedirectToPage("/Home/SubjectDocuments", new { id });
    }

    private async Task<IActionResult> LoadPageAsync(Guid id, CancellationToken cancellationToken)
    {
        IReadOnlyList<IndexedDocument> accessibleDocuments;
        IReadOnlyList<CourseSubject> allCourseCatalog;
        try
        {
            accessibleDocuments = await _knowledge.GetDocumentsAsync(
                BuildDocumentAccessScope(DocumentAccessMode.DocumentUi),
                null,
                cancellationToken);
            allCourseCatalog = await _knowledge.GetCourseCatalogAsync(cancellationToken);
        }
        catch (Exception ex) when (IsDataAccessTimeout(ex))
        {
            _logger.LogWarning(ex, "Subject detail page could not load because the database was unavailable.");
            LoadErrorMessage = "Database unavailable/timeout. Kiem tra SQL Server hoac connection string.";
            accessibleDocuments = Array.Empty<IndexedDocument>();
            allCourseCatalog = Array.Empty<CourseSubject>();
        }

        var visibleCatalog = FilterCourseCatalogForCurrentUser(allCourseCatalog);
        var synchronizedCatalog = BuildSynchronizedCourseCatalogForView(visibleCatalog, accessibleDocuments);
        var subject = synchronizedCatalog.FirstOrDefault(item => item.Id == id);
        if (subject is null)
        {
            return NotFound();
        }

        IsAdmin = base.IsAdmin();
        IsLecturer = base.IsLecturer();
        IsRealCatalogSubject = allCourseCatalog.Any(item => item.Id == id);

        var subjectDocuments = accessibleDocuments
            .Where(document => SubjectMatchesFilter(document.Subject, subject.DisplayName)
                               || SubjectMatchesFilter(document.Subject, subject.Code))
            .OrderBy(document => document.Chapter)
            .ThenByDescending(document => document.UploadedAt)
            .ToList();

        Subject = BuildSubjectTree(subject, subjectDocuments);
        return Page();
    }

    private static DocumentTreeSubjectViewModel BuildSubjectTree(CourseSubject subject, IReadOnlyList<IndexedDocument> documents)
    {
        var subjectNode = new DocumentTreeSubjectViewModel
        {
            SubjectId = subject.Id,
            Code = subject.Code,
            Name = subject.Name,
            DisplayName = subject.DisplayName,
            Description = subject.Description,
            OwnerUserId = subject.OwnerUserId,
            OwnerName = subject.OwnerName,
            OwnerEmail = subject.OwnerEmail,
            Chapters = subject.Chapters
                .OrderBy(chapter => chapter.SortOrder)
                .ThenBy(chapter => chapter.Title)
                .Select(chapter => new DocumentTreeChapterViewModel
                {
                    ChapterId = chapter.Id,
                    SubjectId = subject.Id,
                    SubjectDisplayName = subject.DisplayName,
                    Title = chapter.Title,
                    SortOrder = chapter.SortOrder
                })
                .ToList()
        };

        foreach (var document in documents)
        {
            var chapterTitle = string.IsNullOrWhiteSpace(document.Chapter) ? "Chua phan loai" : document.Chapter.Trim();
            var chapterNode = subjectNode.Chapters.FirstOrDefault(chapter =>
                chapter.Title.Equals(chapterTitle, StringComparison.OrdinalIgnoreCase));
            if (chapterNode is null)
            {
                chapterNode = new DocumentTreeChapterViewModel
                {
                    SubjectId = subject.Id,
                    SubjectDisplayName = subject.DisplayName,
                    Title = chapterTitle,
                    SortOrder = subjectNode.Chapters.Count == 0
                        ? 1
                        : subjectNode.Chapters.Max(chapter => chapter.SortOrder) + 1
                };
                subjectNode.Chapters.Add(chapterNode);
            }

            chapterNode.Documents.Add(document);
        }

        subjectNode.Chapters = subjectNode.Chapters
            .OrderBy(chapter => chapter.SortOrder)
            .ThenBy(chapter => chapter.Title)
            .ToList();
        return subjectNode;
    }
}
