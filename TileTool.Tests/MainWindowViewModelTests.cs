using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using TileTool.Models;
using TileTool.Services;
using TileTool.ViewModels;

namespace TileTool.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void ToggleSnappingCommand_WhenGuidesExist_TogglesGridState()
    {
        var viewModel = CreateViewModel();
        viewModel.HasGhost = true;
        viewModel.GridCellWidth = 32;
        viewModel.GridCellHeight = 32;
        viewModel.HasGrid = true;

        viewModel.ToggleSnappingCommand.Execute(null);

        Assert.False(viewModel.HasGrid);
        Assert.True(viewModel.CanToggleSnapping);
        Assert.Equal("Resume Snapping", viewModel.SnappingButtonText);
        Assert.Equal("Snapping paused", viewModel.SnappingStatusText);

        viewModel.ToggleSnappingCommand.Execute(null);

        Assert.True(viewModel.HasGrid);
        Assert.Equal("Stop Snapping", viewModel.SnappingButtonText);
        Assert.Equal("Snapping active", viewModel.SnappingStatusText);
    }

    [Fact]
    public void ToggleSnappingCommand_WhenGuidesDoNotExist_LeavesSnappingUnavailable()
    {
        var viewModel = CreateViewModel();

        viewModel.ToggleSnappingCommand.Execute(null);

        Assert.False(viewModel.HasGrid);
        Assert.False(viewModel.CanToggleSnapping);
        Assert.Equal("Resume Snapping", viewModel.SnappingButtonText);
        Assert.Equal("Snapping inactive", viewModel.SnappingStatusText);
        Assert.Equal("Save a tile first to enable snapping.", viewModel.StatusMessage);
    }

    private static MainWindowViewModel CreateViewModel() =>
        new(new StubFilePickerService(), new StubConfigService(), new StubTileSaveService());

    private sealed class StubFilePickerService : IFilePickerService
    {
        public Task<string?> PickImageFileAsync(Window owner) => Task.FromResult<string?>(null);
        public Task<string?> PickOutputFolderAsync(Window owner) => Task.FromResult<string?>(null);
    }

    private sealed class StubConfigService : IConfigService
    {
        public string GetConfigPath(string outputFolder) => outputFolder;

        public Task<TileToolConfig> LoadAsync(string outputFolder, CancellationToken cancellationToken = default) =>
            Task.FromResult(new TileToolConfig { OutputFolder = outputFolder });

        public Task SaveAsync(string outputFolder, TileToolConfig config, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubTileSaveService : ITileSaveService
    {
        public Task<TileSaveResult> SaveTileAsync(
            Bitmap image,
            string outputFolder,
            string? prefix,
            int nextTileIndex,
            SelectionRect selection,
            double displayWidth,
            double displayHeight,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TileSaveResult("tile_0001.png", outputFolder, 1, 1));
    }
}
