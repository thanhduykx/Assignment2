using System.Threading.Channels;

namespace ServicesLayer;

public interface IDocumentIndexJobQueue
{
    ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken = default);
}

public sealed class DocumentIndexJobQueue : IDocumentIndexJobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(documentId, cancellationToken);
    }

    public async IAsyncEnumerable<Guid> DequeueAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var documentId in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return documentId;
        }
    }
}