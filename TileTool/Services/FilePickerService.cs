using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Threading.Tasks;
using TileTool.Views;

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
        // Determine a sensible starting folder
        string startFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        if (!Directory.Exists(startFolder))
            startFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return await ImagePickerDialog.ShowAsync(owner, startFolder);
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

