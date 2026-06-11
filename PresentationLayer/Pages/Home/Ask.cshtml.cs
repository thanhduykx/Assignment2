using System.Security.Claims;
using DataAccessLayer;
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
public sealed class AskModel : HomePageModelBase
{
    public AskModel(
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

    public async Task<IActionResult> OnPostAsync([FromBody] ChatRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return new JsonResult(new { error = "Invalid question payload." });
        }

        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            sessionId = Guid.NewGuid();
        }

        try
        {
            var currentUser = await GetCurrentUserAccountAsync(cancellationToken);
            var courseCatalog = await _repository.GetCourseCatalogAsync(cancellationToken);
            var indexedDocuments = (await _indexingService.GetDocumentsAsync(cancellationToken))
                .Where(document => document.Status == DocumentIndexStatus.Indexed)
                .ToList();
            var accessibleDocuments = FilterDocumentsForCurrentUser(indexedDocuments, courseCatalog, currentUser).ToList();
            var allowedSubjects = accessibleDocuments
                .Select(document => document.Subject)
                .Where(subject => !string.IsNullOrWhiteSpace(subject))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var displayName = User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue(ClaimTypes.Email)?.Split('@')[0];
            if (currentUser is not null)
            {
                var existingSession = await _repository.GetSessionAsync(sessionId, cancellationToken);
                if (existingSession?.OwnerUserId is { } ownerUserId && ownerUserId != currentUser.Id)
                {
                    sessionId = Guid.NewGuid();
                }
            }

            var answer = await _chatService.AskAsync(
                sessionId,
                request.Question ?? string.Empty,
                displayName,
                request.SubjectFilter,
                request.Language,
                allowedSubjects,
                BuildChatSessionOwnerInfo(),
                cancellationToken);

            return new JsonResult(new
            {
                sessionId,
                answer = answer.Answer,
                citations = answer.Citations,
                resolvedSubject = answer.ResolvedSubject,
                needsClarification = answer.NeedsClarification,
                subjectOptions = answer.SubjectOptions,
                answerSource = answer.AnswerSource,
                hasDirectCitation = answer.HasDirectCitation,
                fallbackModel = answer.FallbackModel
            });
        }
        catch (Exception ex)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return new JsonResult(new { error = ex.Message });
        }
    }
}
