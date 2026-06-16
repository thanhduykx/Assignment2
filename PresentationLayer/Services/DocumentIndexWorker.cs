using DataAccessLayer;
using ServicesLayer;

namespace PresentationLayer.Services;

public sealed class DocumentIndexWorker : BackgroundService
{
    private const int MaxAttempts = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDocumentIndexJobQueue _queue;
    private readonly IDocumentStatusNotifier _documentStatusNotifier;
    private readonly ILogger<DocumentIndexWorker> _logger;

    public DocumentIndexWorker(
        IServiceScopeFactory scopeFactory,
        IDocumentIndexJobQueue queue,
        IDocumentStatusNotifier documentStatusNotifier,
        ILogger<DocumentIndexWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _documentStatusNotifier = documentStatusNotifier;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await EnqueueProcessingDocumentsAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not enqueue pending indexing jobs at startup.");
        }

        try
        {
            await foreach (var documentId in _queue.DequeueAllAsync(stoppingToken))
            {
                await ProcessWithRetryAsync(documentId, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task EnqueueProcessingDocumentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IKnowledgeRepository>();
        var pendingDocuments = await repository.GetDocumentsByStatusAsync(DocumentIndexStatus.Processing, cancellationToken);
        foreach (var document in pendingDocuments)
        {
            await _queue.EnqueueAsync(document.Id, cancellationToken);
        }
    }

    private async Task ProcessWithRetryAsync(Guid documentId, CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var indexingService = scope.ServiceProvider.GetRequiredService<IDocumentIndexingService>();
                var progress = new Progress<DocumentIndexingProgressUpdate>(update =>
                    _ = _documentStatusNotifier.NotifyDocumentIndexProgressChangedAsync(update, CancellationToken.None));
                await indexingService.ProcessDocumentAsync(documentId, progress, cancellationToken);
                await _documentStatusNotifier.NotifyDocumentStatusChangedAsync(documentId, CancellationToken.None);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Document indexing attempt {Attempt}/{MaxAttempts} failed for {DocumentId}", attempt, MaxAttempts, documentId);
                if (attempt < MaxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                }
            }
        }

        using (var scope = _scopeFactory.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IKnowledgeRepository>();
            await repository.MarkDocumentIndexFailedAsync(
                documentId,
                lastError?.Message ?? "Document indexing failed.",
                cancellationToken);
        }

        await _documentStatusNotifier.NotifyDocumentStatusChangedAsync(documentId, CancellationToken.None);

        if (lastError is not null)
        {
            _logger.LogError(lastError, "Document indexing failed permanently for {DocumentId}", documentId);
        }
    }
}


