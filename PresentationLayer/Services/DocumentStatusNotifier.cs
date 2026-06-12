using DataAccessLayer;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.Hubs;

namespace PresentationLayer.Services;

public sealed record DocumentStatusChangedPayload(
    Guid DocumentId,
    string FileName,
    string Subject,
    string Chapter,
    string Status,
    int ChunkCount,
    DateTimeOffset? IndexedAt,
    string IndexError);

public interface IDocumentStatusNotifier
{
    Task NotifyDocumentStatusChangedAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task NotifyDocumentStatusChangedAsync(IndexedDocument document, CancellationToken cancellationToken = default);
}

public sealed class SignalRDocumentStatusNotifier : IDocumentStatusNotifier
{
    private readonly IHubContext<DocumentStatusHub> _hubContext;
    private readonly IKnowledgeRepository _repository;
    private readonly ILogger<SignalRDocumentStatusNotifier> _logger;

    public SignalRDocumentStatusNotifier(
        IHubContext<DocumentStatusHub> hubContext,
        IKnowledgeRepository repository,
        ILogger<SignalRDocumentStatusNotifier> logger)
    {
        _hubContext = hubContext;
        _repository = repository;
        _logger = logger;
    }

    public async Task NotifyDocumentStatusChangedAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _repository.GetDocumentAsync(documentId, cancellationToken);
            if (document is null)
            {
                return;
            }

            await NotifyDocumentStatusChangedCoreAsync(document, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not publish document status notification for {DocumentId}.", documentId);
        }
    }

    public async Task NotifyDocumentStatusChangedAsync(IndexedDocument document, CancellationToken cancellationToken = default)
    {
        try
        {
            await NotifyDocumentStatusChangedCoreAsync(document, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not publish document status notification for {DocumentId}.", document.Id);
        }
    }

    private async Task NotifyDocumentStatusChangedCoreAsync(IndexedDocument document, CancellationToken cancellationToken)
    {
        var payload = new DocumentStatusChangedPayload(
            document.Id,
            document.FileName,
            document.Subject,
            document.Chapter,
            document.Status,
            document.ChunkCount,
            document.IndexedAt,
            document.IndexError);

        var sendTasks = new List<Task>
        {
            _hubContext.Clients
                .Group(DocumentStatusHub.AdminGroup)
                .SendAsync(DocumentStatusHub.DocumentStatusChangedEvent, payload, cancellationToken)
        };

        if (document.UploadedByUserId is { } ownerUserId)
        {
            sendTasks.Add(_hubContext.Clients
                .Group(DocumentStatusHub.UserGroup(ownerUserId))
                .SendAsync(DocumentStatusHub.DocumentStatusChangedEvent, payload, cancellationToken));
        }

        if (!string.IsNullOrWhiteSpace(document.UploadedByEmail))
        {
            sendTasks.Add(_hubContext.Clients
                .Group(DocumentStatusHub.EmailGroup(document.UploadedByEmail))
                .SendAsync(DocumentStatusHub.DocumentStatusChangedEvent, payload, cancellationToken));
        }

        await Task.WhenAll(sendTasks);
    }
}
