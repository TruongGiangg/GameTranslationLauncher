using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using GameTranslationLauncher.Wpf.Services;

namespace GameTranslationLauncher.Wpf.Controls;

/// <summary>
/// Border dùng cho tấm kính cấp cao nhất: khi bật Glassy, vẽ ảnh hero đã làm mờ đúng vị trí phía sau,
/// rồi màu nhuộm theo game và hạt nhiễu, trước khi vẽ Background/BorderBrush như Border thường.
/// </summary>
/// <remarks>
/// With glass off it renders exactly like a plain Border, so the dark surface is unchanged.
/// </remarks>
public class GlassBorder : Border
{
    private readonly ScaleTransform zoom = new();
    private Point lastHostOrigin = new(double.NaN, double.NaN);

    public GlassBorder()
    {
        // Mirror the hero's Ken Burns zoom; the centre is set per render because it is in this element's space.
        BindingOperations.SetBinding(zoom, ScaleTransform.ScaleXProperty, new Binding(nameof(ScaleTransform.ScaleX)) { Source = GlassBackdrop.Zoom });
        BindingOperations.SetBinding(zoom, ScaleTransform.ScaleYProperty, new Binding(nameof(ScaleTransform.ScaleY)) { Source = GlassBackdrop.Zoom });
        Loaded += (_, _) =>
        {
            GlassBackdrop.Changed += OnBackdropChanged;
            LayoutUpdated += OnLayoutUpdated;
        };
        Unloaded += (_, _) =>
        {
            GlassBackdrop.Changed -= OnBackdropChanged;
            LayoutUpdated -= OnLayoutUpdated;
        };
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (GlassBackdrop.IsEnabled && RenderSize.Width > 0 && RenderSize.Height > 0)
        {
            var bounds = new Rect(RenderSize);
            var radius = CornerRadius.TopLeft;
            if (CreateBackdropBrush() is { } backdrop)
            {
                drawingContext.DrawRoundedRectangle(backdrop, null, bounds, radius, radius);
            }

            drawingContext.DrawRoundedRectangle(GlassBackdrop.AmbientBrush, null, bounds, radius, radius);
            drawingContext.DrawRoundedRectangle(GlassBackdrop.GrainBrush, null, bounds, radius, radius);
        }

        base.OnRender(drawingContext);
    }

    private Brush? CreateBackdropBrush()
    {
        if (GlassBackdrop.Backdrop is not { } source || TryGetHostOrigin(out var origin, out var host) is false)
        {
            return null;
        }

        lastHostOrigin = origin;
        var hostSize = host.RenderSize;
        zoom.CenterX = origin.X + hostSize.Width / 2;
        zoom.CenterY = origin.Y + hostSize.Height / 2;
        return new ImageBrush(source)
        {
            Stretch = Stretch.Fill,
            ViewportUnits = BrushMappingMode.Absolute,
            Viewport = new Rect(origin, hostSize),
            Transform = zoom
        };
    }

    private bool TryGetHostOrigin(out Point origin, out FrameworkElement host)
    {
        origin = default;
        host = GlassBackdrop.Host!;
        if (host is null || !host.IsLoaded || PresentationSource.FromVisual(this) is null
            || PresentationSource.FromVisual(host) != PresentationSource.FromVisual(this))
        {
            return false;
        }

        origin = host.TranslatePoint(default, this);
        return true;
    }

    private void OnBackdropChanged(object? sender, EventArgs e) => InvalidateVisual();

    // Panes can move without resizing (e.g. a right-aligned chip when the window grows), so re-check the offset.
    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (GlassBackdrop.Backdrop is not null && TryGetHostOrigin(out var origin, out _) && origin != lastHostOrigin)
        {
            InvalidateVisual();
        }
    }
}
