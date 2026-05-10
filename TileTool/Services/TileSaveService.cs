using Avalonia;
using Avalonia.Media.Imaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TileTool.Services;

public readonly record struct SourceRect(int X, int Y, int Width, int Height);

public sealed record TileSaveResult(string FileName, string FilePath, int SavedWidth, int SavedHeight);

public interface ITileSaveService
{
    Task<TileSaveResult> SaveTileAsync(
        Bitmap image,
        string outputFolder,
        string? prefix,
        int nextTileIndex,
        SelectionRect selection,
        double displayWidth,
        double displayHeight,
        CancellationToken cancellationToken = default);
}

public sealed class TileSaveService : ITileSaveService
{
    public static SourceRect ComputeSourceRect(
        int sourceImageWidth,
        int sourceImageHeight,
        SelectionRect selection,
        double displayWidth,
        double displayHeight)
    {
        if (displayWidth <= 0 || displayHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(displayWidth), "Display dimensions must be positive.");

        var scaleX = sourceImageWidth / displayWidth;
        var scaleY = sourceImageHeight / displayHeight;

        var srcX = (int)Math.Round(selection.X * scaleX);
        var srcY = (int)Math.Round(selection.Y * scaleY);
        var srcW = (int)Math.Round(selection.Width * scaleX);
        var srcH = (int)Math.Round(selection.Height * scaleY);

        srcX = Math.Clamp(srcX, 0, sourceImageWidth - 1);
        srcY = Math.Clamp(srcY, 0, sourceImageHeight - 1);
        srcW = Math.Clamp(srcW, 1, sourceImageWidth - srcX);
        srcH = Math.Clamp(srcH, 1, sourceImageHeight - srcY);

        return new SourceRect(srcX, srcY, srcW, srcH);
    }

    public async Task<TileSaveResult> SaveTileAsync(
        Bitmap image,
        string outputFolder,
        string? prefix,
        int nextTileIndex,
        SelectionRect selection,
        double displayWidth,
        double displayHeight,
        CancellationToken cancellationToken = default)
    {
        var sourceRect = ComputeSourceRect(
            image.PixelSize.Width,
            image.PixelSize.Height,
            selection,
            displayWidth,
            displayHeight);

        Directory.CreateDirectory(outputFolder);

        byte[] pngBytes = await Task.Run(() =>
        {
            using var renderTarget = new RenderTargetBitmap(new PixelSize(sourceRect.Width, sourceRect.Height), new Vector(96, 96));
            using (var ctx = renderTarget.CreateDrawingContext())
            {
                var srcRect = new Rect(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height);
                var dstRect = new Rect(0, 0, sourceRect.Width, sourceRect.Height);
                ctx.DrawImage(image, srcRect, dstRect);
            }

            using var ms = new MemoryStream();
            renderTarget.Save(ms);
            return ms.ToArray();
        }, cancellationToken);

        var fileName = TileNaming.BuildTileFileName(prefix, nextTileIndex);
        var filePath = Path.Combine(outputFolder, fileName);
        await File.WriteAllBytesAsync(filePath, pngBytes, cancellationToken);

        Trace.TraceInformation(
            "event=tile_saved file={0} width={1} height={2} index={3}",
            fileName,
            sourceRect.Width,
            sourceRect.Height,
            nextTileIndex);

        return new TileSaveResult(fileName, filePath, sourceRect.Width, sourceRect.Height);
    }
}
