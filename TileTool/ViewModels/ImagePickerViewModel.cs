using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TileTool.ViewModels;

public partial class ImagePickerItemViewModel : ObservableObject
{
    private static readonly SemaphoreSlim ThrottleSemaphore = new(4, 4);

    [ObservableProperty]
    private Bitmap? _thumbnail;

    [ObservableProperty]
    private bool _hasThumbnail;

    [ObservableProperty]
    private IBrush _borderColor = Brushes.Transparent;

    public string Name { get; }
    public string FullPath { get; }
    public bool IsFolder { get; }
    public string Icon => IsFolder ? "📁" : "🖼️";

    public ImagePickerItemViewModel(string fullPath, bool isFolder)
    {
        FullPath = fullPath;
        Name = Path.GetFileName(fullPath);
        IsFolder = isFolder;
    }

    public async Task LoadThumbnailAsync()
    {
        if (IsFolder) return;
        await ThrottleSemaphore.WaitAsync();
        try
        {
            await Task.Run(() =>
            {
                using var full = new Bitmap(FullPath);
                int srcW = full.PixelSize.Width;
                int srcH = full.PixelSize.Height;

                const int MaxThumb = 256;
                double scale = Math.Min((double)MaxThumb / srcW, (double)MaxThumb / srcH);
                int tw = Math.Max(1, (int)(srcW * scale));
                int th = Math.Max(1, (int)(srcH * scale));

                var thumb = full.CreateScaledBitmap(new Avalonia.PixelSize(tw, th), Avalonia.Media.Imaging.BitmapInterpolationMode.MediumQuality);
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    Thumbnail = thumb;
                    HasThumbnail = true;
                });
            });
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("event=thumbnail_load_failed path={0} message={1}", FullPath, ex.Message);
        }
        finally
        {
            ThrottleSemaphore.Release();
        }
    }
}

public partial class ImagePickerViewModel : ObservableObject
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp"];

    [ObservableProperty]
    private string _currentPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ImagePickerItemViewModel> _items = [];

    [ObservableProperty]
    private ImagePickerItemViewModel? _selectedItem;

    [ObservableProperty]
    private string _selectedFileName = string.Empty;

    [ObservableProperty]
    private bool _canConfirm;

    public string? ResultPath { get; private set; }

    // Raised when the dialog should close (true = confirmed, false = cancelled)
    public event Action<bool>? CloseRequested;

    public ImagePickerViewModel(string initialFolder)
    {
        Navigate(initialFolder);
    }

    partial void OnSelectedItemChanged(ImagePickerItemViewModel? value)
    {
        // Update border highlights
        foreach (var item in Items)
            item.BorderColor = Brushes.Transparent;

        if (value != null)
        {
            value.BorderColor = Brushes.CornflowerBlue;
            SelectedFileName = value.IsFolder ? string.Empty : value.Name;
            CanConfirm = !value.IsFolder;
        }
        else
        {
            SelectedFileName = string.Empty;
            CanConfirm = false;
        }
    }

    [RelayCommand]
    private void NavigateUp()
    {
        var parent = Directory.GetParent(CurrentPath);
        if (parent != null)
            Navigate(parent.FullName);
    }

    [RelayCommand]
    internal void OpenItem(ImagePickerItemViewModel? item)
    {
        if (item == null) return;
        if (item.IsFolder)
        {
            Navigate(item.FullPath);
        }
        else
        {
            ResultPath = item.FullPath;
            CloseRequested?.Invoke(true);
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedItem == null || SelectedItem.IsFolder) return;
        ResultPath = SelectedItem.FullPath;
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }

    private static bool IsHidden(string path)
    {
        // Unix: names starting with '.'
        var name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(name) && name.StartsWith('.'))
            return true;
        // Windows: Hidden file attribute
        try
        {
            return (File.GetAttributes(path) & FileAttributes.Hidden) != 0;
        }
        catch
        {
            return false;
        }
    }

    private void Navigate(string path)
    {
        if (!Directory.Exists(path)) return;

        CurrentPath = path;
        SelectedItem = null;

        var newItems = new ObservableCollection<ImagePickerItemViewModel>();

        try
        {
            // Folders first
            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                if (!IsHidden(dir))
                    newItems.Add(new ImagePickerItemViewModel(dir, isFolder: true));
            }

            // Image files
            var imageItems = new System.Collections.Generic.List<ImagePickerItemViewModel>();
            foreach (var file in Directory.EnumerateFiles(path))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (!IsHidden(file) && Array.IndexOf(ImageExtensions, ext) >= 0)
                    imageItems.Add(new ImagePickerItemViewModel(file, isFolder: false));
            }

            foreach (var img in imageItems)
                newItems.Add(img);

            Items = newItems;

            // Kick off async thumbnail loading for image items
            foreach (var img in imageItems)
                _ = img.LoadThumbnailAsync();
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("event=image_picker_nav_failed path={0} message={1}", path, ex.Message);
        }
    }
}
