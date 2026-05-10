using TileTool.Services;

namespace TileTool.Tests;

public sealed class TileNamingTests
{
    [Theory]
    [InlineData(null, "tile")]
    [InlineData("", "tile")]
    [InlineData("   ", "tile")]
    [InlineData("...", "tile")]
    [InlineData("___", "tile")]
    [InlineData(" grass ", "grass")]
    [InlineData("tile:name", "tile_name")]
    public void SanitizePrefix_ReturnsExpectedPrefix(string? input, string expected)
    {
        var actual = TileNaming.SanitizePrefix(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BuildTileFileName_UsesPaddedIndexAndSafePrefix()
    {
        var fileName = TileNaming.BuildTileFileName("tile:name", 7);
        Assert.Equal("tile_name_0007.png", fileName);
    }

    [Fact]
    public void BuildTileFileName_ClampsIndexToAtLeastOne()
    {
        var fileName = TileNaming.BuildTileFileName("tile", 0);
        Assert.Equal("tile_0001.png", fileName);
    }
}
