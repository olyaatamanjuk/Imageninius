using System.Text.Json.Serialization;

namespace Imaginator;

using Microsoft.AspNetCore.Components.Forms;


public class FullFileInfo: FileInfoSimple
{
    [JsonIgnore] 
    public IBrowserFile File { get; set; }
    public string Base64 { get; set; }
   
    public string DownloadUrl { get; set; }
    public string FullPath { get; set; }

    public FullFileInfo()
    {
        MetadataForStock = new Metadata();
    }
}
public class FileInfoSimple
{
    public Metadata MetadataForStock { get; set; } = new Metadata(); // <- Ініціалізація
    public string NewBase64 { get; set; } = "";
    public string Name { get; set; } = "";
}