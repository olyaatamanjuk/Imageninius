using System.Collections.Concurrent;

namespace Imaginator;

public class ImageProcessingQueue : IImageProcessingQueue
{
    private readonly ConcurrentQueue<Guid> _queue = new();
    public void Enqueue(Guid uploadId) => _queue.Enqueue(uploadId);
    public bool TryDequeue(out Guid uploadId) => _queue.TryDequeue(out uploadId);
}
