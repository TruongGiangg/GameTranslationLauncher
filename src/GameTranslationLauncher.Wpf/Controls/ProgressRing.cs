using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GameTranslationLauncher.Wpf.Controls;

/// <summary>
/// Vòng tiến trình 0–100: rãnh tròn mờ và cung màu nhấn chạy theo chiều kim đồng hồ từ đỉnh.
/// </summary>
/// <remarks>
/// Value changes glide to the new position instead of jumping, unless Windows animations are off. At 0 (long
/// preflight checks report nothing yet) a short arc circles the track so the dialog never looks frozen.
/// </remarks>
public sealed class ProgressRing : FrameworkElement
{
    private const double SpinArcFraction = 0.22;
    private bool spinning;

    private static readonly DependencyProperty SpinAngleProperty = DependencyProperty.Register(
        "SpinAngle", typeof(double), typeof(ProgressRing),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public ProgressRing()
    {
        IsVisibleChanged += (_, _) => UpdateSpin();
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(ProgressRing), new PropertyMetadata(0d, OnValueChanged));

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness), typeof(double), typeof(ProgressRing),
        new FrameworkPropertyMetadata(8d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(ProgressRing),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ArcBrushProperty = DependencyProperty.Register(
        nameof(ArcBrush), typeof(Brush), typeof(ProgressRing),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly DependencyProperty DisplayValueProperty = DependencyProperty.Register(
        "DisplayValue", typeof(double), typeof(ProgressRing),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public Brush? TrackBrush
    {
        get => (Brush?)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public Brush? ArcBrush
    {
        get => (Brush?)GetValue(ArcBrushProperty);
        set => SetValue(ArcBrushProperty, value);
    }

    private static void OnValueChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        var ring = (ProgressRing)element;
        ring.UpdateSpin();
        var target = Math.Clamp((double)e.NewValue, 0, 100);
        // Dropping back (a new operation starting) snaps instead of unwinding the ring.
        if (!SystemParameters.ClientAreaAnimation || target < (double)ring.GetValue(DisplayValueProperty))
        {
            ring.BeginAnimation(DisplayValueProperty, null);
            ring.SetValue(DisplayValueProperty, target);
            return;
        }

        ring.BeginAnimation(DisplayValueProperty, new DoubleAnimation(target, TimeSpan.FromMilliseconds(350))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        });
    }

    // Runs only while visible, so a hidden dialog does not keep the render loop busy.
    private void UpdateSpin()
    {
        var shouldSpin = Value <= 0 && IsVisible && SystemParameters.ClientAreaAnimation;
        if (shouldSpin == spinning)
        {
            return;
        }

        spinning = shouldSpin;
        BeginAnimation(SpinAngleProperty, shouldSpin
            ? new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1.1)) { RepeatBehavior = RepeatBehavior.Forever }
            : null);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var size = Math.Min(RenderSize.Width, RenderSize.Height);
        if (size <= StrokeThickness)
        {
            return;
        }

        var center = new Point(RenderSize.Width / 2, RenderSize.Height / 2);
        var radius = (size - StrokeThickness) / 2;
        drawingContext.DrawEllipse(null, new Pen(TrackBrush, StrokeThickness), center, radius, radius);

        if (ArcBrush is null)
        {
            return;
        }

        var arcPen = new Pen(ArcBrush, StrokeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var fraction = (double)GetValue(DisplayValueProperty) / 100;
        if (spinning && fraction <= 0)
        {
            DrawArc(drawingContext, arcPen, center, radius, (double)GetValue(SpinAngleProperty) / 360, SpinArcFraction);
        }
        else if (fraction >= 1)
        {
            drawingContext.DrawEllipse(null, arcPen, center, radius, radius);
        }
        else if (fraction > 0)
        {
            DrawArc(drawingContext, arcPen, center, radius, 0, fraction);
        }
    }

    /// <summary>Clockwise arc; <paramref name="startTurn"/> and <paramref name="sweep"/> are fractions of a full turn from 12 o'clock.</summary>
    private static void DrawArc(DrawingContext drawingContext, Pen pen, Point center, double radius, double startTurn, double sweep)
    {
        Point PointAt(double turn) => new(
            center.X + radius * Math.Sin(turn * 2 * Math.PI),
            center.Y - radius * Math.Cos(turn * 2 * Math.PI));

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(PointAt(startTurn), false, false);
            context.ArcTo(PointAt(startTurn + sweep), new Size(radius, radius), 0, sweep > 0.5, SweepDirection.Clockwise, true, false);
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(null, pen, geometry);
    }
}
