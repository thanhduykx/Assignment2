using DataAccessLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.Models;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;

namespace PresentationLayer.Pages.Home;

[IgnoreAntiforgeryToken]
[Authorize(Policy = AuthorizationPolicies.ChatAccess)]
public sealed class StarChatSessionModel : HomePageModelBase
{
    public StarChatSessionModel(
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

    public async Task<IActionResult> OnPostAsync([FromBody] ChatSessionStarRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || !Guid.TryParse(request.SessionId, out var sessionId))
        {
            return BadRequest(new { error = "Invalid chat session." });
        }

        try
        {
            var session = await _knowledge.SetSessionStarredAsync(
                sessionId,
                request.IsStarred,
                cancellationToken,
                BuildChatSessionOwnerInfo());
            return session is null
                ? NotFound(new { error = "Chat session not found." })
                : new JsonResult(ToSessionSummary(session));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
