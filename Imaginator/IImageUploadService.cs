using Microsoft.AspNetCore.Components.Forms;

namespace Imaginator;

public interface IImageUploadService
{
    Task<Guid> SaveFilesAsync(IReadOnlyList<IBrowserFile> files);
}
