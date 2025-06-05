namespace Imaginator;

using SixLabors.ImageSharp;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using MetadataExtractor.Formats.Iptc;
public class Metadata
{
    public string Headline { get; set; } = "";
    public string Caption { get; set; } = "";
    public string Keywords { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public int Category { get; set; }

    public async Task<string> GetPromptFromMetadata(FullFileInfo _fileInfo)
    {
        var _prompt = "";

        var base64Data = _fileInfo.Base64;
        var selectedFile = _fileInfo.File;

        if (selectedFile != null)
        {
            using var stream = selectedFile.OpenReadStream(Const.MaxFileSize);
            using var ms = new MemoryStream();

            await stream.CopyToAsync(ms); // 🟢 Додаємо await
            ms.Seek(0, SeekOrigin.Begin); // Скидаємо потік

            using (var _image = Image.Load(ms)) // ImageSharp читає потік
            {
                if (_image.Metadata.ExifProfile != null)
                {
                    if (_image.Metadata?.ExifProfile?.TryGetValue<string>(
                            SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.ImageDescription,
                            out SixLabors.ImageSharp.Metadata.Profiles.Exif.IExifValue<string>? exifDetails) == true)
                    {
                        _prompt = exifDetails.GetValue().ToString();
                        return _prompt;
                    }
                }
            }

            ms.Seek(0, SeekOrigin.Begin); // скидаємо потік для повторного читання

            // 📦 Зчитуємо EXIF, IPTC, XMP через MetadataExtractor
            var directories = ImageMetadataReader.ReadMetadata(ms);

            // ✅ XMP
            var xmpDir = directories.OfType<XmpDirectory>().FirstOrDefault();
            foreach (var dir in directories)
            {
                foreach (var tag in dir.Tags)
                {
                    Console.WriteLine($"{dir.Name} - {tag.Name} = {tag.Description}");
                    
                }
            }
            
            string descriptionQ = directories
                .SelectMany(dir => dir.Tags)
                .Where(tag => tag.Name == "Textual Data" && tag.Description.StartsWith("Description:"))
                .Select(tag => tag.Description.Substring("Description:".Length).Trim())
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(descriptionQ))
            {
                _prompt = descriptionQ;
                return _prompt;
            }
            
            if (xmpDir != null && !xmpDir.IsEmpty)
            {
                
 
                
                
                var xmp = xmpDir.XmpMeta;
                // Варіант 1: Пошук локалізованої версії (en-US або без мови)
                var localizedDescription =
                    xmp?.GetLocalizedText("http://purl.org/dc/elements/1.1/", "description", null, "en-US");

                if (localizedDescription != null && !string.IsNullOrEmpty(localizedDescription?.ToString()))
                {
                    _prompt = localizedDescription.ToString();
                    return _prompt;
                }

                // Варіант 2: Якщо локалізованого немає, пробуємо загальне значення
                var description = xmp.GetPropertyString("http://purl.org/dc/elements/1.1/", "description");
                if (!string.IsNullOrEmpty(description))
                {
                    _prompt = description.ToString();
                    return _prompt;
                }

                // Варіант 3: Витягуємо всі властивості на всяк випадок
                foreach (var prop in xmp.Properties)
                    if (prop.Path != null && prop.Path.Contains("description"))
                    {
                        _prompt = prop.Value.ToString();
                        return _prompt;
                    }
            }


            // ✅ IPTC
            var iptcDir = directories.OfType<IptcDirectory>().FirstOrDefault();
            if (iptcDir != null && iptcDir.ContainsTag(IptcDirectory.TagCaption))
            {
                var caption = iptcDir.GetDescription(IptcDirectory.TagCaption);
                if (!string.IsNullOrWhiteSpace(caption))
                {
                    _prompt = caption.ToString();
                    return _prompt;
                }
            }


         
        }
        // 🔚 Якщо нічого не знайдено
        return _prompt;
    }
}