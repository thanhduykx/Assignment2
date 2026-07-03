using System.Text;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;

namespace PresentationLayer.Pages.Home;

[Authorize(Policy = AuthorizationPolicies.DocumentManagement)]
public sealed class ViewDocumentModel : HomePageModelBase
{
    public ViewDocumentModel(
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

    public IndexedDocument Document { get; private set; } = new();
    public new string Content { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _knowledge.GetDocumentAsync(id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        if (!await CanViewDocumentAsync(document, cancellationToken))
        {
            return Forbid();
        }

        var storedPath = Path.GetFullPath(document.StoredPath);
        if (!IsPathUnderDirectory(storedPath, GetUploadsRoot()) || !System.IO.File.Exists(storedPath))
        {
            return NotFound();
        }

        if (IsTextDocument(document))
        {
            Document = document;
            Content = await System.IO.File.ReadAllTextAsync(storedPath, Encoding.UTF8, cancellationToken);
            return Page();
        }

        Response.Headers.ContentDisposition = $"inline; filename=\"{document.FileName.Replace("\"", string.Empty)}\"";
        return new PhysicalFileResult(storedPath, ResolveContentType(document))
        {
            EnableRangeProcessing = true
        };
    }
}
