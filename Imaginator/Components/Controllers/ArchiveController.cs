using System.Text;
using ImageMagick;
using Imaginator.Components.Pages;
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

        var currenttask = new UploadTask();
        currenttask.UploadId = uploadId;

        // Додаємо в чергу ТУТ, коли файли точно збережено
        await _queue.EnqueueAsync(currenttask);
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

    [HttpPost("setmetadata/{id}")]
    public async Task<IActionResult>  SetMetadata(Guid id, [FromBody]List<FileInfoSimple> files)
    {
        var inputFolder = Path.Combine(_env.WebRootPath, "uploads", id.ToString());

        if (Directory.Exists(inputFolder))
        {
            // var filesfromfolder = Directory.GetFiles(inputFolder);

            foreach (var _fileInfo in files)
            {
                string filePath = Path.Combine(inputFolder, _fileInfo.Name);
                // Завантаження в памʼять
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                
                using var image = new MagickImage(fileBytes);

                var iptcProfile = image.GetIptcProfile() ?? new IptcProfile();

                // Видаляємо існуючі теги
                iptcProfile.RemoveValue(IptcTag.Headline);
                iptcProfile.RemoveValue(IptcTag.Caption);
                iptcProfile.RemoveValue(IptcTag.Keyword);

                // Додаємо нові значення
                iptcProfile.SetValue(IptcTag.Headline, _fileInfo.MetadataForStock.Headline);
                iptcProfile.SetValue(IptcTag.Caption,  _fileInfo.MetadataForStock.Caption);

                foreach (var keyword in  _fileInfo.MetadataForStock.Keywords.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)))
                {
                    iptcProfile.SetValue(IptcTag.Keyword, keyword);
                }

                image.SetProfile(iptcProfile);
                
                // зберігаєш в той самий файл (перезапис)
                image.Write(filePath);
            }
        }
        else
        {
            Console.WriteLine("❌ Папка не існує: " + inputFolder);
        }
        
        return Ok();
    }
    
    
    /*
    [HttpPost("uploadfiles")]
    public async Task<IActionResult> UploadFiles(List<IFormFile> files)
    {
        
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
        
        return Ok(uploadId);
    }
    */
    
    [DisableRequestSizeLimit]
    [HttpPost("uploadfiles")]
    public async Task<IActionResult> UploadFiles(List<IFormFile> files)
    {
        
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
        
        return Ok(uploadId);
    }
    
    // [HttpGet("uploadzip/{id}")]
    // public async Task<IActionResult> UploadZip(Guid id)
    // {
    //     // Додаємо в чергу ТУТ, коли файли точно збережено
    //     await _queue.EnqueueAsync(id);
    //     Console.WriteLine($"✅ Upload {id} додано в чергу (із контролера)");
    //
    //     return Ok("OK");
    // }
    
    [HttpPost("uploadzip/{id}")]
    public async Task<IActionResult>  UploadZip(Guid id, [FromBody] List<FileInfoSimple> files)
    {
        
        // Додаємо в чергу ТУТ, коли файли точно збережено
        var currenttask = new UploadTask();

        currenttask.UploadId = id;
        currenttask.ItemsFullInfoSimples = files;
        
        await _queue.EnqueueAsync(currenttask);
        Console.WriteLine($"✅ Upload {id} додано в чергу (із контролера)");

        return Ok("OK");
        
        return Ok();
    }
    
    
    
    
    [HttpPost("createcsv/{id}")]
    public async Task<IActionResult>  CreateCsv(Guid id, [FromBody] List<FileInfoSimple> files)
    {
        try
        {
            var csvContent = GenerateCsv(files); 
            var csvPath = Path.Combine(_env.WebRootPath, "uploads", id.ToString(), "output.csv");

            Directory.CreateDirectory(Path.GetDirectoryName(csvPath));
            await System.IO.File.WriteAllTextAsync(csvPath, csvContent); // ← повний шлях, виклик методу
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
       
        
        return Ok();
    }
    
    private string GenerateCsv(List<FileInfoSimple> files)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Filename,Title,Keywords,Category,CategoryName,Prompt");

        foreach (var file in files)
        {
            string filename = EscapeCsv(file.Name);
            string title = EscapeCsv(file.MetadataForStock.Caption ?? "");
            string keywords = EscapeCsv(file.MetadataForStock.Keywords ?? "");
            string prompt = EscapeCsv(file.MetadataForStock.Prompt ?? "");
            string categoryName = EscapeCsv(file.MetadataForStock.CategoryName ?? "");
            string category = EscapeCsv(file.MetadataForStock.Category.ToString());

            sb.AppendLine($"{filename},{title},{keywords},{category},{categoryName},{prompt}");
        }

        return sb.ToString();
    }

    private string EscapeCsv(string input)
    {
        if (input.Contains(',') || input.Contains('"'))
        {
            return $"\"{input.Replace("\"", "\"\"")}\"";
        }

        return input;
    }
}
