using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;

namespace PresentationLayer.Pages.Home;

[Authorize(Policy = AuthorizationPolicies.ChatAccess)]
public sealed class ChatSessionsModel : HomePageModelBase
{
    public ChatSessionsModel(
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

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAccountAsync(cancellationToken);
        var sessions = currentUser is null
            ? Array.Empty<ChatSessionSummary>()
            : await _repository.GetSessionSummariesForOwnerAsync(currentUser.Id, cancellationToken);
        return new JsonResult(sessions.Select(ToSessionSummary));
    }
}
