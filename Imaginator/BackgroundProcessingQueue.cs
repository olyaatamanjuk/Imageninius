using System.Threading.Channels;

namespace Imaginator;

public class UploadTask
{
    public Guid UploadId { get; set; }
    public List<FileInfoSimple> ItemsFullInfoSimples { get; set; } = new();
}
public class BackgroundProcessingQueue : IBackgroundProcessingQueue
{
    private readonly Channel<UploadTask> _queue = Channel.CreateUnbounded<UploadTask>();

    // public async Task EnqueueAsync(Guid uploadId, List<FullFileInfo> fullFileInfos)
    // {
    //     var task = new UploadTask
    //     {
    //         UploadId = uploadId,
    //         ItemsFullFileInfos = fullFileInfos
    //     };
    //
    //     await _queue.Writer.WriteAsync(task);
    // }

    public async Task EnqueueAsync(UploadTask task)
    {
        await _queue.Writer.WriteAsync(task);
    }

    public async Task<UploadTask> DequeueAsync(CancellationToken cancellationToken)
    {
        var item = await _queue.Reader.ReadAsync(cancellationToken);
        return item;
    }
}