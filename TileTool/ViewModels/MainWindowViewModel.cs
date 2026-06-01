using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TileTool.Models;
using TileTool.Services;

namespace TileTool.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IFilePickerService _filePickerService;
    private readonly IConfigService _configService;
    private readonly ITileSaveService _tileSaveService;

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

    [ObservableProperty]
    private bool _hasGhost;

    [ObservableProperty]
    private double _ghostX;

    [ObservableProperty]
    private double _ghostY;

    [ObservableProperty]
    private double _ghostWidth;

    [ObservableProperty]
    private double _ghostHeight;

    [ObservableProperty]
    private bool _hasGrid;

    [ObservableProperty]
    private double _gridOriginX;

    [ObservableProperty]
    private double _gridOriginY;

    [ObservableProperty]
    private double _gridCellWidth;

    [ObservableProperty]
    private double _gridCellHeight;

    [ObservableProperty]
    private bool _canToggleSnapping;

    [ObservableProperty]
    private string _snappingButtonText = "Resume Snapping";

    [ObservableProperty]
    private string _snappingStatusText = "Snapping inactive";

    private TileToolConfig _config = new();
    private bool _isNormalizingSelection;

    public MainWindowViewModel()
        : this(new FilePickerService(), new ConfigService(), new TileSaveService())
    {
    }

    public MainWindowViewModel(IFilePickerService filePickerService, IConfigService configService, ITileSaveService tileSaveService)
    {
        _filePickerService = filePickerService;
        _configService = configService;
        _tileSaveService = tileSaveService;
        UpdateSnappingUi();
    }

    // Window reference for dialogs
    public Window? OwnerWindow { get; set; }

    [RelayCommand]
    private async Task OpenImageAsync()
    {
        if (OwnerWindow == null)
        {
            StatusMessage = "Window is not ready yet.";
            return;
        }

        var path = await _filePickerService.PickImageFileAsync(OwnerWindow);
        if (string.IsNullOrWhiteSpace(path))
            return;

        Bitmap? newBitmap = null;
        try
        {
            newBitmap = new Bitmap(path);

            var oldBitmap = LoadedImage;
            LoadedImage = newBitmap;
            newBitmap = null;
            oldBitmap?.Dispose();

            ImageDisplayWidth = LoadedImage.PixelSize.Width;
            ImageDisplayHeight = LoadedImage.PixelSize.Height;
            HasImage = true;
            HasGhost = false;
            HasGrid = false;

            SelectionWidth = _config.DefaultSelectionWidth;
            SelectionHeight = _config.DefaultSelectionHeight;
            SelectionX = 0;
            SelectionY = 0;
            ClampSelectionToImageBounds();

            StatusMessage = $"Image loaded: {Path.GetFileName(path)} ({LoadedImage.PixelSize.Width}x{LoadedImage.PixelSize.Height})";
        }
        catch (Exception ex)
        {
            newBitmap?.Dispose();
            Trace.TraceError("event=image_load_failed path={0} message={1}", path, ex.Message);
            StatusMessage = $"Could not open image '{Path.GetFileName(path)}'. Ensure it is a supported and readable image file.";
        }
    }

    [RelayCommand]
    private async Task SelectOutputFolderAsync()
    {
        if (OwnerWindow == null)
        {
            StatusMessage = "Window is not ready yet.";
            return;
        }

        var path = await _filePickerService.PickOutputFolderAsync(OwnerWindow);
        if (string.IsNullOrWhiteSpace(path))
            return;

        OutputFolder = path;
        HasOutputFolder = true;

        await LoadConfigAsync();

        StatusMessage = $"Output folder set: {path}";
    }

    [RelayCommand]
    private async Task SaveDefaultSizeAsync()
    {
        _config.DefaultSelectionWidth = (int)Math.Round(Math.Max(1, SelectionWidth));
        _config.DefaultSelectionHeight = (int)Math.Round(Math.Max(1, SelectionHeight));
        await SaveConfigAsync();
        StatusMessage = $"Default size saved: {_config.DefaultSelectionWidth}x{_config.DefaultSelectionHeight}";
    }

    public async Task SaveTileAsync()
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

        var snap = SelectionBounds.Clamp(
            new SelectionRect(SelectionX, SelectionY, SelectionWidth, SelectionHeight),
            ImageDisplayWidth,
            ImageDisplayHeight);

        SelectionX = snap.X;
        SelectionY = snap.Y;
        SelectionWidth = snap.Width;
        SelectionHeight = snap.Height;

        GhostX = snap.X;
        GhostY = snap.Y;
        GhostWidth = snap.Width;
        GhostHeight = snap.Height;
        HasGhost = true;

        GridOriginX = snap.X;
        GridOriginY = snap.Y;
        GridCellWidth = snap.Width;
        GridCellHeight = snap.Height;
        HasGrid = true;

        double nextX = snap.X + snap.Width;
        if (nextX + snap.Width <= ImageDisplayWidth)
            SelectionX = nextX;

        try
        {
            var result = await _tileSaveService.SaveTileAsync(
                LoadedImage,
                OutputFolder,
                Prefix,
                NextTileIndex,
                snap,
                ImageDisplayWidth,
                ImageDisplayHeight);

            NextTileIndex++;
            _config.NextTileIndex = NextTileIndex;
            await SaveConfigAsync();

            StatusMessage = $"Saved: {result.FileName} ({result.SavedWidth}x{result.SavedHeight}px)";
        }
        catch (Exception ex)
        {
            Trace.TraceError("event=tile_save_failed folder={0} message={1}", OutputFolder, ex.Message);
            StatusMessage = "Tile save failed. Check folder permissions, available disk space, and filename settings.";
        }
    }

    [RelayCommand]
    private async Task SaveTileFromToolbarAsync() => await SaveTileAsync();

    [RelayCommand]
    private void ToggleSnapping()
    {
        if (!CanToggleSnapping)
        {
            StatusMessage = "Save a tile first to enable snapping.";
            return;
        }

        HasGrid = !HasGrid;
        StatusMessage = HasGrid
            ? "Snapping resumed."
            : "Snapping paused. Drag freely or save again to re-anchor the snap grid.";
    }

    private async Task LoadConfigAsync()
    {
        try
        {
            var cfg = await _configService.LoadAsync(OutputFolder);
            _config = cfg;

            Prefix = TileNaming.SanitizePrefix(_config.Prefix);
            NextTileIndex = Math.Max(1, _config.NextTileIndex);
            SelectionWidth = Math.Max(1, _config.DefaultSelectionWidth);
            SelectionHeight = Math.Max(1, _config.DefaultSelectionHeight);

            ClampSelectionToImageBounds();
        }
        catch (InvalidDataException ex)
        {
            Trace.TraceWarning("event=config_invalid folder={0} message={1}", OutputFolder, ex.Message);
            StatusMessage = "Output-folder config is invalid and was reset to defaults.";
            await InitConfigAsync();
        }
        catch (IOException ex)
        {
            Trace.TraceError("event=config_load_io_failed folder={0} message={1}", OutputFolder, ex.Message);
            StatusMessage = "Could not read output-folder config; defaults were applied for this session.";
            await InitConfigAsync();
        }
        catch (UnauthorizedAccessException ex)
        {
            Trace.TraceError("event=config_load_access_denied folder={0} message={1}", OutputFolder, ex.Message);
            StatusMessage = "Cannot access output-folder config due to permissions; defaults were applied.";
            await InitConfigAsync();
        }
    }

    private async Task InitConfigAsync()
    {
        _config = new TileToolConfig
        {
            OutputFolder = OutputFolder,
            Prefix = TileNaming.SanitizePrefix(Prefix),
            DefaultSelectionWidth = (int)Math.Round(Math.Max(1, SelectionWidth)),
            DefaultSelectionHeight = (int)Math.Round(Math.Max(1, SelectionHeight)),
            NextTileIndex = 1
        };
        NextTileIndex = 1;

        await SaveConfigAsync();
    }

    private async Task SaveConfigAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputFolder))
            return;

        _config.OutputFolder = OutputFolder;
        _config.Prefix = TileNaming.SanitizePrefix(Prefix);
        _config.NextTileIndex = Math.Max(1, NextTileIndex);
        _config.DefaultSelectionWidth = (int)Math.Round(Math.Max(1, SelectionWidth));
        _config.DefaultSelectionHeight = (int)Math.Round(Math.Max(1, SelectionHeight));

        try
        {
            await _configService.SaveAsync(OutputFolder, _config);
        }
        catch (IOException ex)
        {
            Trace.TraceError("event=config_save_io_failed folder={0} message={1}", OutputFolder, ex.Message);
            StatusMessage = "Could not save config due to an I/O error.";
        }
        catch (UnauthorizedAccessException ex)
        {
            Trace.TraceError("event=config_save_access_denied folder={0} message={1}", OutputFolder, ex.Message);
            StatusMessage = "Could not save config due to insufficient folder permissions.";
        }
    }

    private void ClampSelectionToImageBounds()
    {
        if (_isNormalizingSelection)
            return;

        if (ImageDisplayWidth <= 0 || ImageDisplayHeight <= 0)
            return;

        _isNormalizingSelection = true;
        try
        {
            var clamped = SelectionBounds.Clamp(
                new SelectionRect(SelectionX, SelectionY, SelectionWidth, SelectionHeight),
                ImageDisplayWidth,
                ImageDisplayHeight);

            SelectionX = clamped.X;
            SelectionY = clamped.Y;
            SelectionWidth = clamped.Width;
            SelectionHeight = clamped.Height;
        }
        finally
        {
            _isNormalizingSelection = false;
        }
    }

    public void DisposeLoadedImage()
    {
        LoadedImage?.Dispose();
        LoadedImage = null;
        HasImage = false;
        HasGhost = false;
        HasGrid = false;
        ImageDisplayWidth = 0;
        ImageDisplayHeight = 0;
    }

    public void Dispose() => DisposeLoadedImage();

    partial void OnPrefixChanged(string value)
    {
        var sanitized = TileNaming.SanitizePrefix(value);
        if (!string.Equals(value, sanitized, StringComparison.Ordinal))
        {
            Prefix = sanitized;
            return;
        }

        _ = SaveConfigAsync();
    }

    partial void OnSelectionXChanged(double value) => ClampSelectionToImageBounds();
    partial void OnSelectionYChanged(double value) => ClampSelectionToImageBounds();
    partial void OnSelectionWidthChanged(double value) => ClampSelectionToImageBounds();
    partial void OnSelectionHeightChanged(double value) => ClampSelectionToImageBounds();
    partial void OnHasGhostChanged(bool value) => UpdateSnappingUi();
    partial void OnHasGridChanged(bool value) => UpdateSnappingUi();
    partial void OnGridCellWidthChanged(double value) => UpdateSnappingUi();
    partial void OnGridCellHeightChanged(double value) => UpdateSnappingUi();

    private void UpdateSnappingUi()
    {
        CanToggleSnapping = HasGhost && GridCellWidth > 0 && GridCellHeight > 0;
        SnappingButtonText = HasGrid ? "Stop Snapping" : "Resume Snapping";
        SnappingStatusText = HasGrid
            ? "Snapping active"
            : CanToggleSnapping
                ? "Snapping paused"
                : "Snapping inactive";
    }
}
