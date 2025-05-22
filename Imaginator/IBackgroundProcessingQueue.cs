namespace Imaginator;

public interface IBackgroundProcessingQueue
{
    Task EnqueueAsync(Guid uploadId);
    Task<Guid> DequeueAsync(CancellationToken cancellationToken);
}
