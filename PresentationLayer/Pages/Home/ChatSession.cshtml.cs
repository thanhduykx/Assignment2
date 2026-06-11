using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;

namespace PresentationLayer.Pages.Home;

[Authorize(Policy = AuthorizationPolicies.ChatAccess)]
public sealed class ChatSessionModel : HomePageModelBase
{
    public ChatSessionModel(
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

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAccountAsync(cancellationToken);
        if (currentUser is null)
        {
            return NotFound(new { error = "Chat session not found." });
        }

        var session = await _repository.GetSessionForOwnerAsync(id, currentUser.Id, cancellationToken);
        if (session is null)
        {
            return NotFound(new { error = "Chat session not found." });
        }

        return new JsonResult(new
        {
            id = session.Id,
            title = GetSessionTitle(session),
            isStarred = session.IsStarred,
            createdAt = session.CreatedAt,
            updatedAt = session.UpdatedAt,
            messages = session.Messages
                .OrderBy(message => message.CreatedAt)
                .Select(message => new
                {
                    role = message.Role,
                    content = message.Content,
                    createdAt = message.CreatedAt,
                    citations = message.Citations.Select(citation => new
                    {
                        documentId = citation.DocumentId,
                        fileName = citation.FileName,
                        subject = citation.Subject,
                        chapter = citation.Chapter,
                        chunkIndex = citation.ChunkIndex,
                        score = citation.Score,
                        excerpt = citation.Excerpt
                    })
                })
        });
    }
}
