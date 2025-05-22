using Microsoft.AspNetCore.Components.Forms;

namespace Imaginator;

public class ImageUploadService : IImageUploadService
{
    private readonly IWebHostEnvironment _env;
    private readonly IBackgroundProcessingQueue _queue;

    public ImageUploadService(IWebHostEnvironment env, IBackgroundProcessingQueue queue)
    {
        _env = env;
        _queue = queue;
    }

    public async Task<Guid> SaveFilesAsync(IReadOnlyList<IBrowserFile> files)
    {
        var uploadId = Guid.NewGuid();
        var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", uploadId.ToString());
        Directory.CreateDirectory(uploadFolder);

        foreach (var file in files)
        {
            var filePath = Path.Combine(uploadFolder, file.Name);

            await using var stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 50); // 50 MB
            await using var fs = new FileStream(filePath, FileMode.Create);
            await stream.CopyToAsync(fs);
        }

        // Додаємо в чергу на обробку
        await _queue.EnqueueAsync(uploadId);

        return uploadId;
    }
}