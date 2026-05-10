using TileTool.Services;

namespace TileTool.Tests;

public sealed class SelectionBoundsTests
{
    [Fact]
    public void Clamp_ClampsSizeAndPositionInsideImage()
    {
        var result = SelectionBounds.Clamp(new SelectionRect(-5.2, 98.7, 80.8, 90.9), 100, 100);

        Assert.Equal(0, result.X);
        Assert.Equal(9, result.Y);
        Assert.Equal(81, result.Width);
        Assert.Equal(91, result.Height);
    }

    [Fact]
    public void Clamp_WhenImageSizeInvalid_ReturnsSafeMinimumSelection()
    {
        var result = SelectionBounds.Clamp(new SelectionRect(10, 10, 10, 10), 0, 0);

        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
        Assert.Equal(1, result.Width);
        Assert.Equal(1, result.Height);
    }
}
