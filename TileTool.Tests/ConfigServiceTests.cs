using System.IO;
using System.Threading.Tasks;
using TileTool.Models;
using TileTool.Services;

namespace TileTool.Tests;

public sealed class ConfigServiceTests
{
    [Fact]
    public async Task LoadAsync_WhenConfigMissing_ReturnsDefaultConfigForFolder()
    {
        var service = new ConfigService();
        var folder = Path.Combine(Path.GetTempPath(), "tiletool-tests", Path.GetRandomFileName());
        Directory.CreateDirectory(folder);

        try
        {
            var config = await service.LoadAsync(folder);
            Assert.Equal(folder, config.OutputFolder);
            Assert.Equal("tile", config.Prefix);
            Assert.Equal(32, config.DefaultSelectionWidth);
            Assert.Equal(32, config.DefaultSelectionHeight);
            Assert.Equal(1, config.NextTileIndex);
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsConfig()
    {
        var service = new ConfigService();
        var folder = Path.Combine(Path.GetTempPath(), "tiletool-tests", Path.GetRandomFileName());
        Directory.CreateDirectory(folder);

        try
        {
            var input = new TileToolConfig
            {
                OutputFolder = folder,
                Prefix = "terrain",
                DefaultSelectionWidth = 48,
                DefaultSelectionHeight = 24,
                NextTileIndex = 42
            };

            await service.SaveAsync(folder, input);
            var output = await service.LoadAsync(folder);

            Assert.Equal(folder, output.OutputFolder);
            Assert.Equal("terrain", output.Prefix);
            Assert.Equal(48, output.DefaultSelectionWidth);
            Assert.Equal(24, output.DefaultSelectionHeight);
            Assert.Equal(42, output.NextTileIndex);
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenConfigInvalid_ThrowsInvalidDataException()
    {
        var service = new ConfigService();
        var folder = Path.Combine(Path.GetTempPath(), "tiletool-tests", Path.GetRandomFileName());
        Directory.CreateDirectory(folder);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(folder, "tiletool.json"), "{not-valid-json");

            await Assert.ThrowsAsync<InvalidDataException>(() => service.LoadAsync(folder));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }
}
