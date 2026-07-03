using DataAccessLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;

namespace PresentationLayer.Pages.Home;

[Authorize(Policy = AuthorizationPolicies.DocumentManagement)]
public sealed class EditDocumentModel : HomePageModelBase
{
    public EditDocumentModel(
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

    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    public string FileName { get; set; } = string.Empty;

    [BindProperty]
    public string Subject { get; set; } = string.Empty;

    [BindProperty]
    public string Chapter { get; set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; private set; }
    public int ChunkCount { get; private set; }
    public long FileSizeBytes { get; private set; }
    public string UploadedByName { get; private set; } = string.Empty;
    public string UploadedByEmail { get; private set; } = string.Empty;
    public IReadOnlyList<CourseSubject> CourseCatalog { get; private set; } = Array.Empty<CourseSubject>();

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _knowledge.GetDocumentAsync(id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        if (!await CanEditDocumentAsync(document, cancellationToken))
        {
            return Forbid();
        }

        await LoadFromDocumentAsync(document, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _knowledge.GetDocumentAsync(Id, cancellationToken)
                ?? throw new InvalidOperationException("Document not found.");
            if (!await CanEditDocumentAsync(existing, cancellationToken)
                || !await CanManageSubjectAsync(Subject, cancellationToken))
            {
                return Forbid();
            }

            var document = await _knowledge.UpdateDocumentMetadataAsync(
                Id,
                FileName,
                Subject,
                Chapter,
                cancellationToken);
            await SyncCourseCatalogFromDocumentsAsync(new[] { document }, cancellationToken);
            TempData["Success"] = $"Đã cập nhật tài liệu {document.FileName}.";
            return RedirectToPage("/Home/Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ToVietnameseDocumentError(ex.Message);
            var existing = await _knowledge.GetDocumentAsync(Id, cancellationToken);
            if (existing is not null)
            {
                ContentType = existing.ContentType;
                UploadedAt = existing.UploadedAt;
                ChunkCount = existing.ChunkCount;
                FileSizeBytes = existing.FileSizeBytes;
                UploadedByName = existing.UploadedByName;
                UploadedByEmail = existing.UploadedByEmail;
            }

            CourseCatalog = FilterCourseCatalogForCurrentUser(await _knowledge.GetCourseCatalogAsync(cancellationToken)).ToList();
            return Page();
        }
    }

    private async Task LoadFromDocumentAsync(IndexedDocument document, CancellationToken cancellationToken)
    {
        Id = document.Id;
        FileName = document.FileName;
        Subject = document.Subject;
        Chapter = document.Chapter;
        ContentType = document.ContentType;
        UploadedAt = document.UploadedAt;
        ChunkCount = document.ChunkCount;
        FileSizeBytes = document.FileSizeBytes;
        UploadedByName = document.UploadedByName;
        UploadedByEmail = document.UploadedByEmail;
        CourseCatalog = FilterCourseCatalogForCurrentUser(await _knowledge.GetCourseCatalogAsync(cancellationToken)).ToList();
    }
}
