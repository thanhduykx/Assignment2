using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.Models;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;

namespace PresentationLayer.Pages.Home;

[Authorize(Policy = AuthorizationPolicies.DocumentManagement)]
public sealed class IndexModel : HomePageModelBase
{
    private readonly IEmbeddingService _embeddingService;
    private readonly ITextChunker _chunker;
    private readonly IChunkRetrievalEnrichmentService _chunkEnrichment;
    private readonly IDocumentStatusNotifier _documentStatusNotifier;

    public IndexModel(
        ILogger<HomePageModelBase> logger,
        IKnowledgeService knowledge,
        IDocumentIndexingService indexingService,
        IWebPageTextExtractor webPageTextExtractor,
        IRagChatService chatService,
        IUserAccountStore users,
        IWebHostEnvironment environment,
        IDocumentIndexJobQueue indexJobQueue,
        IEmbeddingService embeddingService,
        ITextChunker chunker,
        IChunkRetrievalEnrichmentService chunkEnrichment,
        IDocumentStatusNotifier documentStatusNotifier)
        : base(logger, knowledge, indexingService, webPageTextExtractor, chatService, users, environment, indexJobQueue)
    {
        _embeddingService = embeddingService;
        _chunker = chunker;
        _chunkEnrichment = chunkEnrichment;
        _documentStatusNotifier = documentStatusNotifier;
    }

    private string EffectiveChunkingStrategy => $"{_chunker.StrategyName}+{_chunkEnrichment.StrategyName}";

    public IReadOnlyList<IndexedDocument> Documents { get; private set; } = Array.Empty<IndexedDocument>();
    public IReadOnlyList<CourseSubject> CourseCatalog { get; private set; } = Array.Empty<CourseSubject>();
    public IReadOnlyList<DocumentTreeSubjectViewModel> DocumentTree { get; private set; } = Array.Empty<DocumentTreeSubjectViewModel>();
    public IReadOnlyList<string> DocumentSubjectOptions { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> DocumentChapterOptions { get; private set; } = Array.Empty<string>();
    public string? Query { get; private set; }
    public string? SubjectFilter { get; private set; }
    public string? StatusFilter { get; private set; }
    public string CurrentSection { get; private set; } = DocumentSections.List;
    public new bool IsAdmin { get; private set; }
    public new bool IsLecturer { get; private set; }
    public int TotalDocumentCount { get; private set; }
    public int TotalChunkCount { get; private set; }
    public long TotalUploadedBytes { get; private set; }
    public int IndexedDocumentCount { get; private set; }
    public int ProcessingDocumentCount { get; private set; }
    public int FailedDocumentCount { get; private set; }
    public int FilteredDocumentCount { get; private set; }
    public double AverageChunksPerIndexedDocument { get; private set; }
    public int StaleEmbeddingDocumentCount { get; private set; }
    public string? LoadErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? section, string? q, string? subjectFilter, string? statusFilter, CancellationToken cancellationToken)
    {
        var normalizedQuery = q?.Trim();
        var normalizedSubjectFilter = subjectFilter?.Trim();
        var normalizedStatusFilter = statusFilter?.Trim();
        var scope = BuildDocumentAccessScope(DocumentAccessMode.DocumentUi);
        var userIsAdmin = base.IsAdmin();
        var userIsLecturer = base.IsLecturer();
        var normalizedSection = NormalizeSection(section);
        if (normalizedSection is DocumentSections.Upload or DocumentSections.Catalog
            && !userIsAdmin
            && !userIsLecturer)
        {
            normalizedSection = DocumentSections.List;
        }

        IReadOnlyList<IndexedDocument> accessibleDocuments;
        IReadOnlyList<IndexedDocument> documents;
        IReadOnlyList<CourseSubject> allCourseCatalog;
        IReadOnlyList<Guid> staleDocumentIds = Array.Empty<Guid>();
        try
        {
            accessibleDocuments = await _knowledge.GetDocumentsAsync(scope, null, cancellationToken);
            documents = await _knowledge.GetDocumentsAsync(
                scope,
                new DocumentListQuery(normalizedQuery, normalizedSubjectFilter, normalizedStatusFilter),
                cancellationToken);
            allCourseCatalog = await _knowledge.GetCourseCatalogAsync(cancellationToken);
            if (userIsAdmin)
            {
                staleDocumentIds = await _knowledge.GetStaleIndexedDocumentIdsAsync(
                    _embeddingService.ModelName,
                    _embeddingService.Dimensions,
                    EffectiveChunkingStrategy,
                    scope,
                    cancellationToken);
            }
        }
        catch (Exception ex) when (IsDataAccessTimeout(ex))
        {
            _logger.LogWarning(ex, "Document management page could not load because the database was unavailable.");
            accessibleDocuments = Array.Empty<IndexedDocument>();
            documents = Array.Empty<IndexedDocument>();
            allCourseCatalog = Array.Empty<CourseSubject>();
            LoadErrorMessage = "Database unavailable/timeout. Kiem tra SQL Server hoac connection string, trang da dung query nhanh de khong treo.";
        }

        var courseCatalog = BuildSynchronizedCourseCatalogForView(
            FilterCourseCatalogForCurrentUser(allCourseCatalog),
            accessibleDocuments);
        var indexedDocuments = accessibleDocuments
            .Where(document => document.Status == DocumentIndexStatus.Indexed)
            .ToList();

        Documents = documents;
        CourseCatalog = courseCatalog;
        DocumentTree = BuildDocumentTree(courseCatalog, accessibleDocuments);
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
        Query = normalizedQuery;
        SubjectFilter = normalizedSubjectFilter;
        StatusFilter = normalizedStatusFilter;
        CurrentSection = normalizedSection;
        ViewData["DocumentSection"] = CurrentSection;
        IsAdmin = userIsAdmin;
        IsLecturer = userIsLecturer;
        TotalDocumentCount = accessibleDocuments.Count;
        TotalChunkCount = accessibleDocuments.Sum(document => document.ChunkCount);
        TotalUploadedBytes = accessibleDocuments.Sum(document => document.FileSizeBytes);
        IndexedDocumentCount = indexedDocuments.Count;
        ProcessingDocumentCount = accessibleDocuments.Count(document => document.Status == DocumentIndexStatus.Processing);
        FailedDocumentCount = accessibleDocuments.Count(document => document.Status == DocumentIndexStatus.Failed);
        FilteredDocumentCount = documents.Count;
        StaleEmbeddingDocumentCount = staleDocumentIds.Count;
        AverageChunksPerIndexedDocument = indexedDocuments.Count == 0
            ? 0
            : indexedDocuments.Average(document => document.ChunkCount);

        return Page();
    }

    private static string NormalizeSection(string? section)
    {
        return section?.Trim().ToLowerInvariant() switch
        {
            DocumentSections.Upload => DocumentSections.Upload,
            DocumentSections.Catalog => DocumentSections.Catalog,
            _ => DocumentSections.List
        };
    }

    private static class DocumentSections
    {
        public const string List = "list";
        public const string Upload = "upload";
        public const string Catalog = "catalog";
    }

    public async Task<IActionResult> OnPostSaveSubjectAsync([FromForm] SubjectCatalogViewModel model, CancellationToken cancellationToken)
    {
        if (!base.IsAdmin())
        {
            return Forbid();
        }

        try
        {
            await _knowledge.UpsertSubjectAsync(model.Id, model.Code, model.Code, model.Description, cancellationToken);
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

        await _knowledge.DeleteSubjectAsync(id, cancellationToken);
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

            await _knowledge.UpsertChapterAsync(model.Id, model.SubjectId, model.Title, model.SortOrder, cancellationToken);
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

        await _knowledge.DeleteChapterAsync(id, cancellationToken);
        TempData["Success"] = "Đã xóa chương.";
        return RedirectToPage("/Home/Index");
    }

    public async Task<IActionResult> OnPostUploadAsync([FromForm] DocumentUploadViewModel model, CancellationToken cancellationToken)
    {
        var isVietnamese = model.Language?.Equals("vi", StringComparison.OrdinalIgnoreCase) == true;
        if (base.IsAdmin())
        {
            TempData["Error"] = isVietnamese 
                ? "Admin không được phép upload tài liệu. Chỉ giảng viên mới có quyền upload." 
                : "Admin is not allowed to upload documents. Only teachers can upload.";
            return RedirectToPage("/Home/Index");
        }

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
                    ? "Môn học và chương/mục là bắt buộc khi upload tài liệu."
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

            TempData["Success"] = isVietnamese
                ? "Đã nhận tài liệu và đang index."
                : "The document has been queued for indexing.";

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
            _logger.LogWarning(ex, "Document upload failed");
            TempData["Error"] = isVietnamese ? ToVietnameseUploadError(ex.Message) : ex.Message;
        }

        return RedirectToPage("/Home/Index");
    }

    public async Task<IActionResult> OnPostDeleteDocumentAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _knowledge.GetDocumentAsync(id, cancellationToken);
        if (document is null)
        {
            TempData["Error"] = "Không tìm thấy tài liệu để xóa.";
            return RedirectToPage("/Home/Index");
        }

        if (!await CanManageDocumentAsync(document, cancellationToken))
        {
            return Forbid();
        }

        await _knowledge.DeleteDocumentAsync(id, cancellationToken);
        TryDeleteStoredFile(document);
        TempData["Success"] = $"Đã xóa tài liệu {document.FileName}.";
        return RedirectToPage("/Home/Index");
    }

    public async Task<IActionResult> OnPostReindexDocumentAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _knowledge.GetDocumentAsync(id, cancellationToken);
        if (document is null)
        {
            TempData["Error"] = "Khong tim thay tai lieu de re-index.";
            return RedirectToPage("/Home/Index");
        }

        if (!await CanManageDocumentAsync(document, cancellationToken))
        {
            return Forbid();
        }

        await _knowledge.MarkDocumentIndexProcessingAsync(id, cancellationToken);
        await _documentStatusNotifier.NotifyDocumentStatusChangedAsync(id, CancellationToken.None);
        await _indexJobQueue.EnqueueAsync(id, cancellationToken);
        TempData["Success"] = $"Da dua {document.FileName} vao hang doi re-index.";
        return RedirectToPage("/Home/Index");
    }

    public async Task<IActionResult> OnPostReindexStaleEmbeddingsAsync(CancellationToken cancellationToken)
    {
        if (!base.IsAdmin())
        {
            return Forbid();
        }

        var staleDocumentIds = await _knowledge.GetStaleIndexedDocumentIdsAsync(
            _embeddingService.ModelName,
            _embeddingService.Dimensions,
            EffectiveChunkingStrategy,
            BuildDocumentAccessScope(DocumentAccessMode.DocumentUi),
            cancellationToken);

        var enqueuedCount = 0;
        foreach (var documentId in staleDocumentIds)
        {
            var document = await _knowledge.GetDocumentAsync(documentId, cancellationToken);
            if (document != null && await CanManageDocumentAsync(document, cancellationToken))
            {
                await _knowledge.MarkDocumentIndexProcessingAsync(documentId, cancellationToken);
                await _documentStatusNotifier.NotifyDocumentStatusChangedAsync(documentId, CancellationToken.None);
                await _indexJobQueue.EnqueueAsync(documentId, cancellationToken);
                enqueuedCount++;
            }
        }

        TempData["Success"] = enqueuedCount == 0
            ? "Khong co tai lieu stale embedding can re-index hoac ban khong co quyen."
            : $"Da dua {enqueuedCount} tai lieu stale embedding vao hang doi re-index.";
        return RedirectToPage("/Home/Index");
    }

    private static bool DocumentMatchesQuery(IndexedDocument document, string query)
    {
        return Contains(document.FileName, query)
               || Contains(document.Subject, query)
               || Contains(document.Chapter, query)
               || Contains(document.UploadedByName, query)
               || Contains(document.UploadedByEmail, query)
               || Contains(document.ContentType, query);
    }

    private static bool DocumentMatchesStatus(IndexedDocument document, string statusFilter)
    {
        return document.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contains(string? value, string query)
    {
        return !string.IsNullOrWhiteSpace(value)
               && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<DocumentTreeSubjectViewModel> BuildDocumentTree(
        IReadOnlyList<CourseSubject> courseCatalog,
        IReadOnlyList<IndexedDocument> documents)
    {
        var subjects = courseCatalog
            .OrderBy(subject => subject.Code)
            .ThenBy(subject => subject.Name)
            .Select(subject => new DocumentTreeSubjectViewModel
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
            })
            .ToList();

        foreach (var document in documents
                     .OrderBy(document => document.Chapter)
                     .ThenByDescending(document => document.UploadedAt))
        {
            var subject = FindSubjectForDocumentSubject(courseCatalog, document.Subject);
            var subjectNode = subject is null
                ? null
                : subjects.FirstOrDefault(item => item.SubjectId == subject.Id);

            if (subjectNode is null)
            {
                var parsed = ParseSubjectForCatalog(document.Subject);
                var subjectCode = string.IsNullOrWhiteSpace(parsed.Code) ? "UNCATALOGED" : parsed.Code;
                subjectNode = new DocumentTreeSubjectViewModel
                {
                    SubjectId = CreateStableCatalogId(subjectCode),
                    Code = subjectCode,
                    Name = subjectCode,
                    DisplayName = subjectCode,
                    Description = "Tạo từ tài liệu đã upload nhưng chưa có trong catalog."
                };
                subjects.Add(subjectNode);
            }

            var chapterTitle = string.IsNullOrWhiteSpace(document.Chapter) ? "Chưa phân loại" : document.Chapter.Trim();
            var chapterNode = subjectNode.Chapters.FirstOrDefault(chapter =>
                chapter.Title.Equals(chapterTitle, StringComparison.OrdinalIgnoreCase));
            if (chapterNode is null)
            {
                chapterNode = new DocumentTreeChapterViewModel
                {
                    SubjectId = subjectNode.SubjectId,
                    SubjectDisplayName = subjectNode.DisplayName,
                    Title = chapterTitle,
                    SortOrder = subjectNode.Chapters.Count == 0
                        ? 1
                        : subjectNode.Chapters.Max(chapter => chapter.SortOrder) + 1
                };
                subjectNode.Chapters.Add(chapterNode);
            }

            chapterNode.Documents.Add(document);
        }

        return subjects
            .OrderBy(subject => subject.Code)
            .ThenBy(subject => subject.Name)
            .ToList();
    }
}
