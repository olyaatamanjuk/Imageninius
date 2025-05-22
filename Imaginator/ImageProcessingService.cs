using System.IO.Compression;

namespace Imaginator;

public class ImageProcessingService : IImageProcessingService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ImageProcessingService> _logger;

    public ImageProcessingService(IWebHostEnvironment env, ILogger<ImageProcessingService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid uploadId)
    {
        var uploadPath = Path.Combine(_env.WebRootPath, "uploads", uploadId.ToString());
        var archivePath = Path.Combine(_env.WebRootPath, "archives", $"{uploadId}.zip");

        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);

        using var zip = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        foreach (var file in Directory.GetFiles(uploadPath))
        {
            zip.CreateEntryFromFile(file, Path.GetFileName(file));
        }

        // Ти можеш також зчитати метадані, зберегти в БД і т.д.
        _logger.LogInformation($"✅ Архів створено: {archivePath}");
    }
}
