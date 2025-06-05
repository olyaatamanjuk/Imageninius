namespace Imaginator;

public interface IBackgroundProcessingQueue
{
    Task EnqueueAsync(UploadTask task);
    Task<UploadTask> DequeueAsync(CancellationToken cancellationToken);
}
