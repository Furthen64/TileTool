using TileTool.Services;

namespace TileTool.Tests;

public sealed class TileSaveServiceTests
{
    [Fact]
    public void ComputeSourceRect_ClampsToImageBounds()
    {
        var rect = TileSaveService.ComputeSourceRect(
            sourceImageWidth: 128,
            sourceImageHeight: 96,
            selection: new SelectionRect(90, 70, 50, 40),
            displayWidth: 128,
            displayHeight: 96);

        Assert.Equal(90, rect.X);
        Assert.Equal(70, rect.Y);
        Assert.Equal(38, rect.Width);
        Assert.Equal(26, rect.Height);
    }

    [Fact]
    public void ComputeSourceRect_ScalesDisplayCoordinates()
    {
        var rect = TileSaveService.ComputeSourceRect(
            sourceImageWidth: 256,
            sourceImageHeight: 256,
            selection: new SelectionRect(32, 16, 16, 16),
            displayWidth: 128,
            displayHeight: 128);

        Assert.Equal(64, rect.X);
        Assert.Equal(32, rect.Y);
        Assert.Equal(32, rect.Width);
        Assert.Equal(32, rect.Height);
    }
}
