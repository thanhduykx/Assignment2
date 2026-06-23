using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;

namespace PresentationLayer.Pages.Home;

[IgnoreAntiforgeryToken]
[Authorize(Policy = AuthorizationPolicies.ChatAccess)]
public sealed class CreateChatSessionModel : HomePageModelBase
{
    public CreateChatSessionModel(
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

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var session = await _repository.GetOrCreateSessionAsync(Guid.NewGuid(), cancellationToken, BuildChatSessionOwnerInfo());
        return new JsonResult(ToSessionSummary(session));
    }
}
