using System.IO.Compression;

namespace Imaginator;

public class BackgroundArchiveWorker : BackgroundService
{
    private readonly IWebHostEnvironment _env;
    private readonly IBackgroundProcessingQueue _queue;
    private readonly IProcessingStatusService _status;

    public BackgroundArchiveWorker(IWebHostEnvironment env, IBackgroundProcessingQueue queue, IProcessingStatusService status)
    {
        _env = env;
        _queue = queue;
        _status = status;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                
                Console.WriteLine("🟢 Очікування елементів у черзі...");

                while (!stoppingToken.IsCancellationRequested)
                {
                    var currentTask = await _queue.DequeueAsync(stoppingToken);
                    
                    var uploadId = currentTask.UploadId;
                    var listSimpleFileInfos = currentTask.ItemsFullInfoSimples;
                    
                    
                   // var uploadId = await _queue.DequeueAsync(stoppingToken);
                    Console.WriteLine($"📦 Починаємо обробку архіву: {uploadId}");

                    // Очікуємо завдання з черги асинхронно

                    _status.SetStatus(uploadId, "processing");
                    
                 

                    var inputFolder = Path.Combine(_env.WebRootPath, "uploads", uploadId.ToString());
                    var archiveFolder = Path.Combine(_env.WebRootPath, "archives");
                    
                    
                    var uniqueContentTypes = listSimpleFileInfos
                        .Where(x => !string.IsNullOrWhiteSpace(x.MetadataForStock.ContentType))
                        .Select(x => x.MetadataForStock.ContentType)
                        .Distinct()
                        .ToList();

                    if (uniqueContentTypes.Count > 1)
                    {
                        foreach (var contentType in uniqueContentTypes)
                        {
                           var newFolderPath = Path.Combine(inputFolder, contentType); 
                           Directory.CreateDirectory(newFolderPath);
                        }

                        foreach (var fileInfoSimple in listSimpleFileInfos)
                        {
                            File.Move( Path.Combine(inputFolder, fileInfoSimple.Name),  Path.Combine(inputFolder, fileInfoSimple.MetadataForStock.ContentType, fileInfoSimple.Name));
                        }
                        
                    }
                    
                    Directory.CreateDirectory(archiveFolder);

                    var archivePath = Path.Combine(archiveFolder, $"{uploadId}.zip");

                    if (File.Exists(archivePath)) File.Delete(archivePath);

                    // Створюємо архів з усіма файлами в папці
                    Console.WriteLine("Початок створення архіву БЕК");
                    ZipFile.CreateFromDirectory(inputFolder, archivePath);

                    _status.SetStatus(uploadId, "done");

                    // Видаляємо папку з файлами після архівації
                    Directory.Delete(inputFolder, true);
                    
                }
            }
            catch (OperationCanceledException)
            {
                // Коректне завершення роботи при скасуванні токена
                break;
            }
            catch (Exception ex)
            {
                _status.SetStatus(Guid.Empty, "error"); // Або поставити статус для конкретного uploadId, якщо є
                Console.WriteLine($"Error in BackgroundArchiveWorker: {ex.Message}");
            }
        }
    }
}
