using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;

namespace TileTool.Views;

/// <summary>
/// A custom control that renders a bitmap and allows the user to drag/resize
/// a selection rectangle over it.
/// </summary>
public class ImageCanvas : Control
{
    // ── Styled / Attached Properties ─────────────────────────────────────────

    public static readonly StyledProperty<Bitmap?> ImageSourceProperty =
        AvaloniaProperty.Register<ImageCanvas, Bitmap?>(nameof(ImageSource));

    public static readonly StyledProperty<double> SelectionXProperty =
        AvaloniaProperty.Register<ImageCanvas, double>(nameof(SelectionX), 0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> SelectionYProperty =
        AvaloniaProperty.Register<ImageCanvas, double>(nameof(SelectionY), 0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> SelectionWidthProperty =
        AvaloniaProperty.Register<ImageCanvas, double>(nameof(SelectionWidth), 32, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> SelectionHeightProperty =
        AvaloniaProperty.Register<ImageCanvas, double>(nameof(SelectionHeight), 32, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public Bitmap? ImageSource
    {
        get => GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public double SelectionX
    {
        get => GetValue(SelectionXProperty);
        set => SetValue(SelectionXProperty, value);
    }

    public double SelectionY
    {
        get => GetValue(SelectionYProperty);
        set => SetValue(SelectionYProperty, value);
    }

    public double SelectionWidth
    {
        get => GetValue(SelectionWidthProperty);
        set => SetValue(SelectionWidthProperty, value);
    }

    public double SelectionHeight
    {
        get => GetValue(SelectionHeightProperty);
        set => SetValue(SelectionHeightProperty, value);
    }

    // ── Interaction State ────────────────────────────────────────────────────

    private enum DragMode { None, Move, ResizeN, ResizeS, ResizeE, ResizeW, ResizeNE, ResizeNW, ResizeSE, ResizeSW, Create }

    private DragMode _dragMode = DragMode.None;
    private Point _dragStart;
    private double _dragOrigX, _dragOrigY, _dragOrigW, _dragOrigH;

    private const double HandleSize = 8.0;
    private const double EdgeThreshold = 10.0;

    // ── Brushes / Pens ───────────────────────────────────────────────────────

    private static readonly IBrush OverlayBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));
    private static readonly IPen SelectionPen = new Pen(Brushes.White, 1.5, dashStyle: DashStyle.Dash);
    private static readonly IPen SelectionPenSolid = new Pen(Brushes.CornflowerBlue, 1.5);
    private static readonly IBrush HandleFill = Brushes.White;
    private static readonly IPen HandlePen = new Pen(Brushes.CornflowerBlue, 1.5);

    // ── Constructor ──────────────────────────────────────────────────────────

    static ImageCanvas()
    {
        AffectsRender<ImageCanvas>(
            ImageSourceProperty,
            SelectionXProperty,
            SelectionYProperty,
            SelectionWidthProperty,
            SelectionHeightProperty);
    }

    public ImageCanvas()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
    {
        if (ImageSource == null) return new Size(0, 0);
        return new Size(ImageSource.PixelSize.Width, ImageSource.PixelSize.Height);
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    public override void Render(DrawingContext context)
    {
        var bounds = Bounds;

        // Background
        context.FillRectangle(new SolidColorBrush(Color.Parse("#1A1A1A")), new Rect(bounds.Size));

        if (ImageSource == null) return;

        // Draw the image
        var imgRect = new Rect(0, 0, ImageSource.PixelSize.Width, ImageSource.PixelSize.Height);
        context.DrawImage(ImageSource, imgRect, imgRect);

        // Draw dark overlay around selection
        var selRect = GetSelectionRect();
        DrawDimOverlay(context, imgRect, selRect);

        // Selection rectangle border
        context.DrawRectangle(null, SelectionPenSolid, selRect.Inflate(0.5));
        context.DrawRectangle(null, SelectionPen, selRect.Inflate(0.5));

        // Draw resize handles
        DrawHandles(context, selRect);
    }

    private void DrawDimOverlay(DrawingContext ctx, Rect image, Rect sel)
    {
        // Top
        if (sel.Top > 0)
            ctx.FillRectangle(OverlayBrush, new Rect(0, 0, image.Width, sel.Top));
        // Bottom
        if (sel.Bottom < image.Height)
            ctx.FillRectangle(OverlayBrush, new Rect(0, sel.Bottom, image.Width, image.Height - sel.Bottom));
        // Left
        ctx.FillRectangle(OverlayBrush, new Rect(0, sel.Top, sel.Left, sel.Height));
        // Right
        if (sel.Right < image.Width)
            ctx.FillRectangle(OverlayBrush, new Rect(sel.Right, sel.Top, image.Width - sel.Right, sel.Height));
    }

    private void DrawHandles(DrawingContext ctx, Rect sel)
    {
        double h = HandleSize;
        double hh = h / 2;

        // Corners
        DrawHandle(ctx, new Point(sel.Left - hh, sel.Top - hh), h);
        DrawHandle(ctx, new Point(sel.Right - hh, sel.Top - hh), h);
        DrawHandle(ctx, new Point(sel.Left - hh, sel.Bottom - hh), h);
        DrawHandle(ctx, new Point(sel.Right - hh, sel.Bottom - hh), h);

        // Midpoints
        DrawHandle(ctx, new Point(sel.Left + sel.Width / 2 - hh, sel.Top - hh), h);
        DrawHandle(ctx, new Point(sel.Left + sel.Width / 2 - hh, sel.Bottom - hh), h);
        DrawHandle(ctx, new Point(sel.Left - hh, sel.Top + sel.Height / 2 - hh), h);
        DrawHandle(ctx, new Point(sel.Right - hh, sel.Top + sel.Height / 2 - hh), h);
    }

    private void DrawHandle(DrawingContext ctx, Point topLeft, double size)
    {
        ctx.FillRectangle(HandleFill, new Rect(topLeft.X, topLeft.Y, size, size));
        ctx.DrawRectangle(null, HandlePen, new Rect(topLeft.X, topLeft.Y, size, size));
    }

    // ── Mouse Interaction ────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var pos = e.GetPosition(this);
        _dragMode = GetDragMode(pos);

        _dragStart = pos;
        _dragOrigX = SelectionX;
        _dragOrigY = SelectionY;
        _dragOrigW = SelectionWidth;
        _dragOrigH = SelectionHeight;

        e.Pointer.Capture(this);
        Focus();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var pos = e.GetPosition(this);

        if (_dragMode == DragMode.None)
        {
            // Update cursor
            Cursor = GetCursorForPosition(pos);
            return;
        }

        var delta = pos - _dragStart;
        ApplyDrag(delta);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragMode == DragMode.Create)
        {
            // Normalize negative-size rectangles after creation drag
            NormalizeSelection();
        }
        _dragMode = DragMode.None;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void ApplyDrag(Vector delta)
    {
        if (ImageSource == null) return;

        double imgW = ImageSource.PixelSize.Width;
        double imgH = ImageSource.PixelSize.Height;

        double x = _dragOrigX, y = _dragOrigY, w = _dragOrigW, h = _dragOrigH;

        switch (_dragMode)
        {
            case DragMode.Move:
                x = Clamp(_dragOrigX + delta.X, 0, imgW - w);
                y = Clamp(_dragOrigY + delta.Y, 0, imgH - h);
                break;

            case DragMode.Create:
                double x2 = _dragStart.X + delta.X;
                double y2 = _dragStart.Y + delta.Y;
                x = Math.Min(_dragStart.X, x2);
                y = Math.Min(_dragStart.Y, y2);
                w = Math.Abs(delta.X);
                h = Math.Abs(delta.Y);
                x = Clamp(x, 0, imgW);
                y = Clamp(y, 0, imgH);
                w = Math.Max(1, Math.Min(w, imgW - x));
                h = Math.Max(1, Math.Min(h, imgH - y));
                break;

            case DragMode.ResizeE:
                w = Math.Max(1, _dragOrigW + delta.X);
                w = Math.Min(w, imgW - x);
                break;
            case DragMode.ResizeW:
                double newX = Clamp(_dragOrigX + delta.X, 0, _dragOrigX + _dragOrigW - 1);
                w = _dragOrigX + _dragOrigW - newX;
                x = newX;
                break;
            case DragMode.ResizeS:
                h = Math.Max(1, _dragOrigH + delta.Y);
                h = Math.Min(h, imgH - y);
                break;
            case DragMode.ResizeN:
                double newY = Clamp(_dragOrigY + delta.Y, 0, _dragOrigY + _dragOrigH - 1);
                h = _dragOrigY + _dragOrigH - newY;
                y = newY;
                break;
            case DragMode.ResizeNE:
                double nyNE = Clamp(_dragOrigY + delta.Y, 0, _dragOrigY + _dragOrigH - 1);
                h = _dragOrigY + _dragOrigH - nyNE;
                y = nyNE;
                w = Math.Max(1, Math.Min(_dragOrigW + delta.X, imgW - x));
                break;
            case DragMode.ResizeNW:
                double nxNW = Clamp(_dragOrigX + delta.X, 0, _dragOrigX + _dragOrigW - 1);
                double nyNW = Clamp(_dragOrigY + delta.Y, 0, _dragOrigY + _dragOrigH - 1);
                w = _dragOrigX + _dragOrigW - nxNW;
                h = _dragOrigY + _dragOrigH - nyNW;
                x = nxNW;
                y = nyNW;
                break;
            case DragMode.ResizeSE:
                w = Math.Max(1, Math.Min(_dragOrigW + delta.X, imgW - x));
                h = Math.Max(1, Math.Min(_dragOrigH + delta.Y, imgH - y));
                break;
            case DragMode.ResizeSW:
                double nxSW = Clamp(_dragOrigX + delta.X, 0, _dragOrigX + _dragOrigW - 1);
                w = _dragOrigX + _dragOrigW - nxSW;
                x = nxSW;
                h = Math.Max(1, Math.Min(_dragOrigH + delta.Y, imgH - y));
                break;
        }

        SelectionX = Math.Round(x);
        SelectionY = Math.Round(y);
        SelectionWidth = Math.Round(w);
        SelectionHeight = Math.Round(h);
    }

    private void NormalizeSelection()
    {
        if (SelectionWidth < 1) SelectionWidth = 1;
        if (SelectionHeight < 1) SelectionHeight = 1;
    }

    // ── Hit Testing ──────────────────────────────────────────────────────────

    private DragMode GetDragMode(Point pos)
    {
        bool insideSelection = ClassifyPosition(pos, out var mode);

        if (!insideSelection)
        {
            // Outside selection — start a new creation drag
            if (ImageSource != null && pos.X >= 0 && pos.Y >= 0 &&
                pos.X <= ImageSource.PixelSize.Width && pos.Y <= ImageSource.PixelSize.Height)
            {
                SelectionX = Math.Round(pos.X);
                SelectionY = Math.Round(pos.Y);
                SelectionWidth = 1;
                SelectionHeight = 1;
                return DragMode.Create;
            }
            return DragMode.None;
        }

        return mode;
    }

    private Cursor GetCursorForPosition(Point pos)
    {
        ClassifyPosition(pos, out var mode);
        return mode switch
        {
            DragMode.Move => new Cursor(StandardCursorType.SizeAll),
            DragMode.ResizeN or DragMode.ResizeS => new Cursor(StandardCursorType.SizeNorthSouth),
            DragMode.ResizeE or DragMode.ResizeW => new Cursor(StandardCursorType.SizeWestEast),
            DragMode.ResizeNW or DragMode.ResizeSE => new Cursor(StandardCursorType.TopLeftCorner),
            DragMode.ResizeNE or DragMode.ResizeSW => new Cursor(StandardCursorType.TopRightCorner),
            _ => new Cursor(StandardCursorType.Cross)
        };
    }

    /// <summary>
    /// Classifies where <paramref name="pos"/> is relative to the selection rectangle.
    /// Returns <c>true</c> when <paramref name="pos"/> is inside (or on the border of) the
    /// selection and sets <paramref name="mode"/> accordingly.  Returns <c>false</c> and
    /// sets <paramref name="mode"/> to <see cref="DragMode.Create"/> when outside.
    /// </summary>
    private bool ClassifyPosition(Point pos, out DragMode mode)
    {
        var sel = GetSelectionRect();
        double t = EdgeThreshold;

        bool nearLeft   = Math.Abs(pos.X - sel.Left)   <= t;
        bool nearRight  = Math.Abs(pos.X - sel.Right)  <= t;
        bool nearTop    = Math.Abs(pos.Y - sel.Top)    <= t;
        bool nearBottom = Math.Abs(pos.Y - sel.Bottom) <= t;

        bool insideX = pos.X >= sel.Left - t && pos.X <= sel.Right  + t;
        bool insideY = pos.Y >= sel.Top  - t && pos.Y <= sel.Bottom + t;

        if (!insideX || !insideY) { mode = DragMode.Create; return false; }
        if (nearLeft  && nearTop)    { mode = DragMode.ResizeNW; return true; }
        if (nearRight && nearTop)    { mode = DragMode.ResizeNE; return true; }
        if (nearLeft  && nearBottom) { mode = DragMode.ResizeSW; return true; }
        if (nearRight && nearBottom) { mode = DragMode.ResizeSE; return true; }
        if (nearLeft)   { mode = DragMode.ResizeW; return true; }
        if (nearRight)  { mode = DragMode.ResizeE; return true; }
        if (nearTop)    { mode = DragMode.ResizeN; return true; }
        if (nearBottom) { mode = DragMode.ResizeS; return true; }
        mode = DragMode.Move;
        return true;
    }

    private Rect GetSelectionRect() =>
        new Rect(SelectionX, SelectionY, SelectionWidth, SelectionHeight);

    private static double Clamp(double v, double min, double max) =>
        Math.Max(min, Math.Min(max, v));
}
