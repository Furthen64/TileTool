using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TileTool.Models;

namespace TileTool.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const string ConfigFileName = "tiletool.json";
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    [ObservableProperty]
    private Bitmap? _loadedImage;

    [ObservableProperty]
    private string _outputFolder = string.Empty;

    [ObservableProperty]
    private string _prefix = "tile";

    [ObservableProperty]
    private double _selectionX;

    [ObservableProperty]
    private double _selectionY;

    [ObservableProperty]
    private double _selectionWidth = 32;

    [ObservableProperty]
    private double _selectionHeight = 32;

    [ObservableProperty]
    private bool _hasImage;

    [ObservableProperty]
    private bool _hasOutputFolder;

    [ObservableProperty]
    private string _statusMessage = "Open an image and select an output folder to get started.";

    [ObservableProperty]
    private int _nextTileIndex = 1;

    [ObservableProperty]
    private double _imageDisplayWidth;

    [ObservableProperty]
    private double _imageDisplayHeight;

    private TileToolConfig _config = new();
    private string? _currentImagePath;

    // Window reference for dialogs
    public Window? OwnerWindow { get; set; }

    [RelayCommand]
    private async Task OpenImageAsync()
    {
        if (OwnerWindow == null) return;

        var files = await OwnerWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Image",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp" }
                },
                FilePickerFileTypes.All
            }
        });

        if (files.Count == 0) return;

        var path = files[0].TryGetLocalPath();
        if (path == null) return;

        try
        {
            LoadedImage?.Dispose();
            LoadedImage = new Bitmap(path);
            _currentImagePath = path;
            HasImage = true;
            ImageDisplayWidth = LoadedImage.PixelSize.Width;
            ImageDisplayHeight = LoadedImage.PixelSize.Height;

            // Apply default selection size
            SelectionWidth = _config.DefaultSelectionWidth;
            SelectionHeight = _config.DefaultSelectionHeight;
            SelectionX = 0;
            SelectionY = 0;

            StatusMessage = $"Image loaded: {Path.GetFileName(path)} ({LoadedImage.PixelSize.Width}x{LoadedImage.PixelSize.Height})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading image: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SelectOutputFolderAsync()
    {
        if (OwnerWindow == null) return;

        var folders = await OwnerWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Output Folder"
        });

        if (folders.Count == 0) return;

        var path = folders[0].TryGetLocalPath();
        if (path == null) return;

        OutputFolder = path;
        HasOutputFolder = true;

        // Load or create config
        LoadConfig();

        StatusMessage = $"Output folder set: {path}";
    }

    [RelayCommand]
    private void SaveDefaultSize()
    {
        _config.DefaultSelectionWidth = (int)Math.Round(SelectionWidth);
        _config.DefaultSelectionHeight = (int)Math.Round(SelectionHeight);
        SaveConfig();
        StatusMessage = $"Default size saved: {_config.DefaultSelectionWidth}x{_config.DefaultSelectionHeight}";
    }

    public void SaveTile()
    {
        if (!HasImage || !HasOutputFolder || LoadedImage == null)
        {
            StatusMessage = "Please open an image and select an output folder first.";
            return;
        }

        if (ImageDisplayWidth <= 0 || ImageDisplayHeight <= 0)
        {
            StatusMessage = "Image display dimensions are invalid.";
            return;
        }

        try
        {
            // Compute the actual pixel coordinates on the original image
            double scaleX = LoadedImage.PixelSize.Width / ImageDisplayWidth;
            double scaleY = LoadedImage.PixelSize.Height / ImageDisplayHeight;

            int srcX = (int)Math.Round(SelectionX * scaleX);
            int srcY = (int)Math.Round(SelectionY * scaleY);
            int srcW = (int)Math.Round(SelectionWidth * scaleX);
            int srcH = (int)Math.Round(SelectionHeight * scaleY);

            // Clamp to image bounds
            srcX = Math.Max(0, Math.Min(srcX, LoadedImage.PixelSize.Width - 1));
            srcY = Math.Max(0, Math.Min(srcY, LoadedImage.PixelSize.Height - 1));
            srcW = Math.Max(1, Math.Min(srcW, LoadedImage.PixelSize.Width - srcX));
            srcH = Math.Max(1, Math.Min(srcH, LoadedImage.PixelSize.Height - srcY));

            // Render the cropped region
            var renderTarget = new RenderTargetBitmap(new PixelSize(srcW, srcH), new Vector(96, 96));
            using (var ctx = renderTarget.CreateDrawingContext())
            {
                var srcRect = new Rect(srcX, srcY, srcW, srcH);
                var dstRect = new Rect(0, 0, srcW, srcH);
                ctx.DrawImage(LoadedImage, srcRect, dstRect);
            }

            // Generate filename
            string safePrefix = string.IsNullOrWhiteSpace(Prefix) ? "tile" : Prefix.Trim();
            string fileName = $"{safePrefix}_{NextTileIndex:D4}.png";
            string filePath = Path.Combine(OutputFolder, fileName);

            renderTarget.Save(filePath);
            renderTarget.Dispose();

            NextTileIndex++;
            _config.NextTileIndex = NextTileIndex;
            SaveConfig();

            StatusMessage = $"Saved: {fileName} ({srcW}x{srcH}px)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving tile: {ex.Message}";
        }
    }

    private void LoadConfig()
    {
        var configPath = Path.Combine(OutputFolder, ConfigFileName);
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var cfg = JsonSerializer.Deserialize<TileToolConfig>(json, _jsonOptions);
                if (cfg != null)
                {
                    _config = cfg;
                    Prefix = _config.Prefix;
                    NextTileIndex = _config.NextTileIndex;
                    SelectionWidth = _config.DefaultSelectionWidth;
                    SelectionHeight = _config.DefaultSelectionHeight;
                }
            }
            catch (JsonException ex)
            {
                // Config is corrupt — reset to defaults
                StatusMessage = $"Config parse error: {ex.Message}. Resetting.";
                InitConfig();
            }
        }
        else
        {
            InitConfig();
        }
    }

    private void InitConfig()
    {
        _config = new TileToolConfig
        {
            OutputFolder = OutputFolder,
            Prefix = Prefix,
            DefaultSelectionWidth = (int)SelectionWidth,
            DefaultSelectionHeight = (int)SelectionHeight,
            NextTileIndex = 1
        };
        NextTileIndex = 1;
        SaveConfig();
    }

    private void SaveConfig()
    {
        if (string.IsNullOrEmpty(OutputFolder)) return;
        _config.OutputFolder = OutputFolder;
        _config.Prefix = Prefix;
        _config.NextTileIndex = NextTileIndex;

        try
        {
            var configPath = Path.Combine(OutputFolder, ConfigFileName);
            var json = JsonSerializer.Serialize(_config, _jsonOptions);
            File.WriteAllText(configPath, json);
        }
        catch (IOException ex)
        {
            StatusMessage = $"Could not save config: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            StatusMessage = $"Config access denied: {ex.Message}";
        }
    }

    partial void OnPrefixChanged(string value)
    {
        _config.Prefix = value;
        SaveConfig();
    }
}

