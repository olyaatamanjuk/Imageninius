namespace Imaginator;

public interface IImageProcessingQueue
{
    void Enqueue(Guid uploadId);
    bool TryDequeue(out Guid uploadId);
}