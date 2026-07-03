using System.Security.Claims;
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
public sealed class AskModel : HomePageModelBase
{
    public AskModel(
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
            var chatScope = BuildDocumentAccessScope(DocumentAccessMode.Chat);
            var allowedSubjects = await _knowledge.GetIndexedSubjectsAsync(chatScope, cancellationToken);
            var displayName = User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue(ClaimTypes.Email)?.Split('@')[0];
            if (currentUser is not null)
            {
                var existingSession = await _knowledge.GetSessionAsync(sessionId, cancellationToken);
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
                chatScope,
                cancellationToken);

            return new JsonResult(new
            {
                sessionId,
                answer = answer.Answer,
                citations = RedactCitationsForCurrentRole(answer.Citations),
                resolvedSubject = answer.ResolvedSubject,
                needsClarification = answer.NeedsClarification,
                subjectOptions = answer.SubjectOptions,
                answerSource = answer.AnswerSource,
                hasDirectCitation = answer.HasDirectCitation,
                fallbackModel = answer.FallbackModel
            });
        }
        catch (Exception ex) when (IsDataAccessTimeout(ex))
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return new JsonResult(new { error = "Database unavailable/timeout. Vui long thu lai sau vai giay." });
        }
        catch (Exception ex)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return new JsonResult(new { error = ex.Message });
        }
    }

    private object RedactCitationsForCurrentRole(IReadOnlyList<SourceCitation> citations)
    {
        if (CurrentRole() != AppRoles.Student)
        {
            return citations;
        }

        return citations
            .Select(citation => new
            {
                subject = citation.Subject,
                chapter = citation.Chapter,
                score = citation.Score
            })
            .ToList();
    }
}
