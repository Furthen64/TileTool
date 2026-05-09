using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;

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

    public static readonly StyledProperty<bool> HasGhostProperty =
        AvaloniaProperty.Register<ImageCanvas, bool>(nameof(HasGhost), false);

    public static readonly StyledProperty<double> GhostXProperty =
        AvaloniaProperty.Register<ImageCanvas, double>(nameof(GhostX), 0);

    public static readonly StyledProperty<double> GhostYProperty =
        AvaloniaProperty.Register<ImageCanvas, double>(nameof(GhostY), 0);

    public static readonly StyledProperty<double> GhostWidthProperty =
        AvaloniaProperty.Register<ImageCanvas, double>(nameof(GhostWidth), 0);

    public static readonly StyledProperty<double> GhostHeightProperty =
        AvaloniaProperty.Register<ImageCanvas, double>(nameof(GhostHeight), 0);

    public static readonly StyledProperty<bool> HasGridProperty =
        AvaloniaProperty.Register<ImageCanvas, bool>(nameof(HasGrid), false);

    public static readonly StyledProperty<double> GridOriginXProperty =
        AvaloniaProperty.Register<ImageCanvas, double>(nameof(GridOriginX), 0);

    public static readonly StyledProperty<double> GridOriginYProperty =
        AvaloniaProperty.Register<ImageCanvas, double>(nameof(GridOriginY), 0);

    public static readonly StyledProperty<double> GridCellWidthProperty =
        AvaloniaProperty.Register<ImageCanvas, double>(nameof(GridCellWidth), 0);

    public static readonly StyledProperty<double> GridCellHeightProperty =
        AvaloniaProperty.Register<ImageCanvas, double>(nameof(GridCellHeight), 0);

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

    public bool HasGhost
    {
        get => GetValue(HasGhostProperty);
        set => SetValue(HasGhostProperty, value);
    }

    public double GhostX
    {
        get => GetValue(GhostXProperty);
        set => SetValue(GhostXProperty, value);
    }

    public double GhostY
    {
        get => GetValue(GhostYProperty);
        set => SetValue(GhostYProperty, value);
    }

    public double GhostWidth
    {
        get => GetValue(GhostWidthProperty);
        set => SetValue(GhostWidthProperty, value);
    }

    public double GhostHeight
    {
        get => GetValue(GhostHeightProperty);
        set => SetValue(GhostHeightProperty, value);
    }

    public bool HasGrid
    {
        get => GetValue(HasGridProperty);
        set => SetValue(HasGridProperty, value);
    }

    public double GridOriginX
    {
        get => GetValue(GridOriginXProperty);
        set => SetValue(GridOriginXProperty, value);
    }

    public double GridOriginY
    {
        get => GetValue(GridOriginYProperty);
        set => SetValue(GridOriginYProperty, value);
    }

    public double GridCellWidth
    {
        get => GetValue(GridCellWidthProperty);
        set => SetValue(GridCellWidthProperty, value);
    }

    public double GridCellHeight
    {
        get => GetValue(GridCellHeightProperty);
        set => SetValue(GridCellHeightProperty, value);
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
    private static readonly IPen GhostPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 255, 165, 0)), 1.5, dashStyle: DashStyle.Dash);
    private static readonly IBrush GhostFill = new SolidColorBrush(Color.FromArgb(30, 255, 165, 0));
    private static readonly IBrush HandleFill = Brushes.White;
    private static readonly IPen HandlePen = new Pen(Brushes.CornflowerBlue, 1.5);
    private static readonly IBrush LabelBackgroundBrush = new SolidColorBrush(Color.FromArgb(220, 26, 26, 26));
    private static readonly IPen LabelBorderPen = new Pen(Brushes.CornflowerBlue, 1);
    private static readonly IBrush LabelTextBrush = Brushes.White;
    private static readonly IBrush InputBackgroundBrush = new SolidColorBrush(Color.FromArgb(235, 16, 16, 16));
    private static readonly IBrush InputTextBrush = Brushes.White;
    private static readonly Typeface OverlayTypeface = new("Inter, Segoe UI, Arial");

    private const double OverlayFontSize = 12;
    private const double OverlayPadding = 6;
    private const int MaxInputLength = 16;
    private const double MinInputOverlayWidth = 90;

    private bool _isSizeInputActive;
    private string _sizeInputText = string.Empty;

    // ── Constructor ──────────────────────────────────────────────────────────

    static ImageCanvas()
    {
        AffectsRender<ImageCanvas>(
            ImageSourceProperty,
            SelectionXProperty,
            SelectionYProperty,
            SelectionWidthProperty,
            SelectionHeightProperty,
            HasGhostProperty,
            GhostXProperty,
            GhostYProperty,
            GhostWidthProperty,
            GhostHeightProperty);
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

        // Draw ghost (previous saved selection)
        if (HasGhost && GhostWidth > 0 && GhostHeight > 0)
        {
            var ghostRect = new Rect(GhostX, GhostY, GhostWidth, GhostHeight);
            context.FillRectangle(GhostFill, ghostRect);
            context.DrawRectangle(null, GhostPen, ghostRect.Inflate(0.5));
        }

        // Selection rectangle border
        context.DrawRectangle(null, SelectionPenSolid, selRect.Inflate(0.5));
        context.DrawRectangle(null, SelectionPen, selRect.Inflate(0.5));

        // Draw resize handles
        DrawHandles(context, selRect);

        // Draw floating size label and optional input field
        DrawSelectionSizeLabel(context, selRect);
        if (_isSizeInputActive)
            DrawSizeInputOverlay(context, selRect);
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

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (ImageSource == null || string.IsNullOrEmpty(e.Text))
            return;

        char c = e.Text[0];
        if (!(char.IsDigit(c) || c == 'x' || c == 'X'))
            return;

        if (!_isSizeInputActive)
        {
            _isSizeInputActive = true;
            _sizeInputText = string.Empty;
        }

        if ((c == 'x' || c == 'X') && _sizeInputText.Contains('x'))
        {
            e.Handled = true;
            return;
        }

        if (_sizeInputText.Length < MaxInputLength)
        {
            _sizeInputText += char.ToLowerInvariant(c);
            InvalidateVisual();
        }

        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!_isSizeInputActive)
            return;

        switch (e.Key)
        {
            case Key.Back:
                if (_sizeInputText.Length > 0)
                {
                    _sizeInputText = _sizeInputText[..^1];
                    InvalidateVisual();
                }
                e.Handled = true;
                break;

            case Key.Enter:
                TryApplyTypedSize();
                _isSizeInputActive = false;
                _sizeInputText = string.Empty;
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.Escape:
                _isSizeInputActive = false;
                _sizeInputText = string.Empty;
                InvalidateVisual();
                e.Handled = true;
                break;
        }
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
                if (HasGrid && GridCellWidth > 0 && GridCellHeight > 0)
                {
                    x = Clamp(SnapAxis(x, GridOriginX, GridCellWidth), 0, imgW - w);
                    y = Clamp(SnapAxis(y, GridOriginY, GridCellHeight), 0, imgH - h);
                }
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
                double sx = Math.Round(pos.X);
                double sy = Math.Round(pos.Y);
                if (HasGrid && GridCellWidth > 0 && GridCellHeight > 0)
                {
                    sx = Clamp(SnapAxis(sx, GridOriginX, GridCellWidth), 0, ImageSource.PixelSize.Width);
                    sy = Clamp(SnapAxis(sy, GridOriginY, GridCellHeight), 0, ImageSource.PixelSize.Height);
                }
                SelectionX = sx;
                SelectionY = sy;
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

    private static double SnapAxis(double value, double origin, double cellSize) =>
        origin + Math.Round((value - origin) / cellSize) * cellSize;

    private void DrawSelectionSizeLabel(DrawingContext context, Rect selRect)
    {
        string label = $"{Math.Round(SelectionWidth)}x{Math.Round(SelectionHeight)} px";
        var textLayout = new FormattedText(
            label,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            OverlayTypeface,
            OverlayFontSize,
            LabelTextBrush);
        double panelWidth = textLayout.Width + OverlayPadding * 2;
        double panelHeight = textLayout.Height + OverlayPadding * 2;
        var pos = GetOverlayPosition(selRect, panelWidth, panelHeight);
        var panelRect = new Rect(
            pos.X,
            pos.Y,
            panelWidth,
            panelHeight);

        context.FillRectangle(LabelBackgroundBrush, panelRect);
        context.DrawRectangle(null, LabelBorderPen, panelRect);
        context.DrawText(textLayout, new Point(pos.X + OverlayPadding, pos.Y + OverlayPadding));
    }

    private void DrawSizeInputOverlay(DrawingContext context, Rect selRect)
    {
        string inputText = string.IsNullOrEmpty(_sizeInputText) ? "WxH|" : $"{_sizeInputText}|";
        var textLayout = new FormattedText(
            inputText,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            OverlayTypeface,
            OverlayFontSize,
            InputTextBrush);
        double panelWidth = Math.Max(MinInputOverlayWidth, textLayout.Width + OverlayPadding * 2);
        double panelHeight = textLayout.Height + OverlayPadding * 2;
        var pos = GetOverlayPosition(selRect, panelWidth, panelHeight * 2 + 4);
        pos = new Point(pos.X, pos.Y + panelHeight + 4);

        var panelRect = new Rect(
            pos.X,
            pos.Y,
            panelWidth,
            panelHeight);

        context.FillRectangle(InputBackgroundBrush, panelRect);
        context.DrawRectangle(null, LabelBorderPen, panelRect);
        context.DrawText(textLayout, new Point(pos.X + OverlayPadding, pos.Y + OverlayPadding));
    }

    private Point GetOverlayPosition(Rect selRect, double overlayWidth, double overlayHeight)
    {
        double x = selRect.Left + 4;
        double y = selRect.Top - overlayHeight - 4;

        if (y < 0)
            y = selRect.Top + 4;
        if (ImageSource != null)
        {
            y = Clamp(y, 0, Math.Max(0, ImageSource.PixelSize.Height - overlayHeight));
            x = Clamp(x, 0, Math.Max(0, ImageSource.PixelSize.Width - overlayWidth));
        }

        return new Point(x, y);
    }

    private void TryApplyTypedSize()
    {
        if (ImageSource == null || string.IsNullOrWhiteSpace(_sizeInputText))
            return;

        var parts = _sizeInputText.Split('x', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return;

        if (!int.TryParse(parts[0], out int width) || !int.TryParse(parts[1], out int height))
            return;

        width = Math.Clamp(width, 1, ImageSource.PixelSize.Width);
        height = Math.Clamp(height, 1, ImageSource.PixelSize.Height);

        SelectionWidth = width;
        SelectionHeight = height;
        SelectionX = Clamp(SelectionX, 0, ImageSource.PixelSize.Width - SelectionWidth);
        SelectionY = Clamp(SelectionY, 0, ImageSource.PixelSize.Height - SelectionHeight);
    }
}
