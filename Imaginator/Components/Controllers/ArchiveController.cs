using Microsoft.AspNetCore.Mvc;

namespace Imaginator.Components.Controllers;

[ApiController]
[Route("api/archive")]
public class ArchiveController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IProcessingStatusService _status;
    private readonly IBackgroundProcessingQueue _queue;

    public ArchiveController(IWebHostEnvironment env, IProcessingStatusService status, IBackgroundProcessingQueue queue)
    {
        _env = env;
        _status = status;
        _queue = queue;
    }

    [DisableRequestSizeLimit]
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(List<IFormFile> files)
    {
        
        Console.WriteLine($"✅Зайшли в контроллер");
        
        if (files == null || files.Count == 0)
            return BadRequest();

        var uploadId = Guid.NewGuid();
        var uploadPath = Path.Combine(_env.WebRootPath, "uploads", uploadId.ToString());
        Directory.CreateDirectory(uploadPath);

        foreach (var file in files)
        {
            var filePath = Path.Combine(uploadPath, file.FileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
        }

        // Додаємо в чергу ТУТ, коли файли точно збережено
        await _queue.EnqueueAsync(uploadId);
        Console.WriteLine($"✅ Upload {uploadId} додано в чергу (із контролера)");

        return Ok(uploadId);
    }
    [HttpGet("status/{id}")]
    public IActionResult GetStatus(Guid id)
    {
        var status = _status.GetStatus(id);
        return Ok(status ?? "unknown");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetArchive(Guid id)
    {
        var archivePath = Path.Combine(_env.WebRootPath, "archives", $"{id}.zip");
        if (!System.IO.File.Exists(archivePath))
            return NotFound("Архів не знайдено");

        var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return File(stream, "application/zip", $"{id}.zip");
    }
}
