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
    public IReadOnlyList<ChatQuestionSuggestionViewModel> BenchmarkQuestions { get; private set; } = Array.Empty<ChatQuestionSuggestionViewModel>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var documents = await _indexingService.GetDocumentsAsync(cancellationToken);
        var courseCatalog = await _repository.GetCourseCatalogAsync(cancellationToken);
        var currentUser = await GetCurrentUserAccountAsync(cancellationToken);
        var allIndexedDocuments = documents
            .Where(document => document.Status == DocumentIndexStatus.Indexed)
            .ToList();
        var indexedDocuments = FilterDocumentsForCurrentUser(allIndexedDocuments, courseCatalog, currentUser).ToList();
        var subjectOptions = indexedDocuments
            .Select(document => document.Subject)
            .Where(subject => !string.IsNullOrWhiteSpace(subject))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(subject => subject)
            .ToList();

        ChatSessions = currentUser is null
            ? Array.Empty<ChatSession>()
            : await _repository.GetSessionsForOwnerAsync(currentUser.Id, cancellationToken);
        Documents = indexedDocuments;
        SubjectOptions = subjectOptions;
        BenchmarkQuestions = LoadFineTunedQuestionSuggestions(subjectOptions);
    }
}
