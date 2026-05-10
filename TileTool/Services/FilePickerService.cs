using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace TileTool.Services;

public interface IFilePickerService
{
    Task<string?> PickImageFileAsync(Window owner);
    Task<string?> PickOutputFolderAsync(Window owner);
}

public sealed class FilePickerService : IFilePickerService
{
    public async Task<string?> PickImageFileAsync(Window owner)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Image",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Images")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp"]
                },
                FilePickerFileTypes.All
            ]
        });

        if (files.Count == 0)
            return null;

        return files[0].TryGetLocalPath();
    }

    public async Task<string?> PickOutputFolderAsync(Window owner)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Output Folder"
        });

        if (folders.Count == 0)
            return null;

        return folders[0].TryGetLocalPath();
    }
}
