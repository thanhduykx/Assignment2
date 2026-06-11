using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using PresentationLayer.Models;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;

namespace PresentationLayer.Pages.Home;

[Authorize(Policy = AuthorizationPolicies.ChatAccess)]
public sealed class ChatModel : HomePageModelBase
{
    public ChatModel(
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

    public IReadOnlyList<ChatSession> ChatSessions { get; private set; } = Array.Empty<ChatSession>();
    public IReadOnlyList<IndexedDocument> Documents { get; private set; } = Array.Empty<IndexedDocument>();
    public IReadOnlyList<string> SubjectOptions { get; private set; } = Array.Empty<string>();
    public string? LoadErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAccountAsync(cancellationToken);
        var chatScope = BuildDocumentAccessScope(DocumentAccessMode.Chat);
        IReadOnlyList<string> subjectOptions;
        IReadOnlyList<IndexedDocument> indexedDocuments;
        try
        {
            subjectOptions = await _repository.GetIndexedSubjectsAsync(chatScope, cancellationToken);
            indexedDocuments = CurrentRole() == AppRoles.Student
                ? Array.Empty<IndexedDocument>()
                : await _repository.GetDocumentsAsync(
                    chatScope,
                    new DocumentListQuery(StatusFilter: DocumentIndexStatus.Indexed),
                    cancellationToken);
        }
        catch (Exception ex) when (IsDataAccessTimeout(ex))
        {
            _logger.LogWarning(ex, "Chat page could not load indexed subject metadata because the database was unavailable.");
            subjectOptions = Array.Empty<string>();
            indexedDocuments = Array.Empty<IndexedDocument>();
            LoadErrorMessage = "Database unavailable/timeout. Chat metadata could not be loaded.";
        }

        try
        {
            ChatSessions = currentUser is null
                ? Array.Empty<ChatSession>()
                : await _repository.GetSessionsForOwnerAsync(currentUser.Id, cancellationToken);
        }
        catch (Exception ex) when (IsDataAccessTimeout(ex))
        {
            _logger.LogWarning(ex, "Chat page could not load sessions because the database was unavailable.");
            ChatSessions = Array.Empty<ChatSession>();
            LoadErrorMessage ??= "Database unavailable/timeout. Chat metadata could not be loaded.";
        }

        Documents = indexedDocuments;
        SubjectOptions = subjectOptions;
    }
}
