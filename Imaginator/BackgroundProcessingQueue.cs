using System.Threading.Channels;

namespace Imaginator;

public class BackgroundProcessingQueue : IBackgroundProcessingQueue
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

    public async Task EnqueueAsync(Guid uploadId)
    {
        await _queue.Writer.WriteAsync(uploadId);
    }

    public async Task<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        var item = await _queue.Reader.ReadAsync(cancellationToken);
        return item;
    }
}