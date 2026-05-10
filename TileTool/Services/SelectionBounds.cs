using System;

namespace TileTool.Services;

public readonly record struct SelectionRect(double X, double Y, double Width, double Height);

public static class SelectionBounds
{
    public static SelectionRect Clamp(SelectionRect selection, double imageWidth, double imageHeight)
    {
        if (imageWidth <= 0 || imageHeight <= 0)
            return new SelectionRect(0, 0, 1, 1);

        var width = Math.Clamp(Math.Round(selection.Width), 1, imageWidth);
        var height = Math.Clamp(Math.Round(selection.Height), 1, imageHeight);
        var x = Math.Clamp(Math.Round(selection.X), 0, imageWidth - width);
        var y = Math.Clamp(Math.Round(selection.Y), 0, imageHeight - height);

        return new SelectionRect(x, y, width, height);
    }
}
