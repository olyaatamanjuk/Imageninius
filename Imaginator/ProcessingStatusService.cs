using System.Collections.Concurrent;

namespace Imaginator;

public class ProcessingStatusService : IProcessingStatusService
{
    private readonly ConcurrentDictionary<Guid, string> _statuses = new();
    public void SetStatus(Guid id, string status) => _statuses[id] = status;
    public string GetStatus(Guid id) => _statuses.TryGetValue(id, out var status) ? status : "pending";
}
