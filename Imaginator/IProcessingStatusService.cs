namespace Imaginator;

public interface IProcessingStatusService
{
    void SetStatus(Guid id, string status);
    string GetStatus(Guid id);
}
