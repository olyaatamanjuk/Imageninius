namespace Imaginator;

public interface IImageProcessingService
{
    Task ProcessAsync(Guid uploadId);
}