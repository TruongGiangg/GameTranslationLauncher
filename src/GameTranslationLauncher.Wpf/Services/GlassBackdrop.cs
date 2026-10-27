using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Trạng thái kính Glassy dùng chung: ảnh hero đã làm mờ, màu nhuộm theo game, hạt nhiễu và zoom của hero.
/// </summary>
/// <remarks>
/// UI-thread only. GameDetailsView reports the hero; LauncherThemeManager turns glass on/off;
/// every GlassBorder redraws when <see cref="Changed"/> fires.
/// </remarks>
public static class GlassBackdrop
{
    private const byte AmbientAlpha = 51;
    private const byte GlowAlpha = 115;
    private static readonly Color DefaultAmbient = Color.FromRgb(150, 80, 90);
    private static readonly ConditionalWeakTable<BitmapSource, FilteredHero> Cache = new();
    private static ImageSource? heroImage;
    private static Brush? heroFallback;

    public static event EventHandler? Changed;

    /// <summary>Ken Burns zoom shared by the hero image and every glass backdrop, so they stay aligned.</summary>
    public static ScaleTransform Zoom { get; } = new(1, 1);

    public static SolidColorBrush AmbientBrush { get; } = new(Colors.Transparent);

    /// <summary>Same hue as <see cref="AmbientBrush"/> but stronger, for the Ambilight edge glow.</summary>
    public static SolidColorBrush GlowBrush { get; } = new(Colors.Transparent);

    public static Brush GrainBrush { get; } = CreateGrainBrush();

    /// <summary>The element the hero image fills; glass panes map the blurred copy onto its bounds.</summary>
    public static FrameworkElement? Host { get; set; }

    public static bool IsEnabled { get; private set; }

    public static BitmapSource? Backdrop { get; private set; }

    public static void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        Refresh();
    }

    public static void SetHero(ImageSource? image, Brush? fallbackBrush)
    {
        heroImage = image;
        heroFallback = fallbackBrush;
        Refresh();
    }

    private static void Refresh()
    {
        Backdrop = null;
        var ambient = DefaultAmbient;
        if (IsEnabled)
        {
            if (heroImage is BitmapSource bitmap && TryFilter(bitmap) is { } filtered)
            {
                Backdrop = filtered.Backdrop;
                ambient = filtered.Ambient;
            }
            else if (FirstColor(heroFallback) is { } fallbackColor)
            {
                ambient = GlassBackdropImageFilter.ToAmbient(fallbackColor);
            }
        }

        AnimateAmbient(IsEnabled ? Color.FromArgb(AmbientAlpha, ambient.R, ambient.G, ambient.B) : Colors.Transparent);
        Changed?.Invoke(null, EventArgs.Empty);
    }

    private static FilteredHero? TryFilter(BitmapSource bitmap)
    {
        if (Cache.TryGetValue(bitmap, out var cached))
        {
            return cached;
        }

        try
        {
            var (backdrop, ambient) = GlassBackdropImageFilter.Create(bitmap);
            var filtered = new FilteredHero(backdrop, ambient);
            Cache.AddOrUpdate(bitmap, filtered);
            return filtered;
        }
        catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException or ArgumentException)
        {
            // Glass is presentation only: an unreadable hero falls back to the plain tint instead of failing the UI.
            Trace.TraceWarning($"Glass backdrop skipped: {exception.Message}");
            return null;
        }
    }

    private static Color? FirstColor(Brush? brush) => brush switch
    {
        SolidColorBrush solid => solid.Color,
        GradientBrush { GradientStops.Count: > 0 } gradient => gradient.GradientStops[0].Color,
        _ => null
    };

    private static void AnimateAmbient(Color target)
    {
        Animate(AmbientBrush, target);
        Animate(GlowBrush, Color.FromArgb(target.A == 0 ? (byte)0 : GlowAlpha, target.R, target.G, target.B));
    }

    private static void Animate(SolidColorBrush brush, Color target)
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            brush.BeginAnimation(SolidColorBrush.ColorProperty, null);
            brush.Color = target;
            return;
        }

        brush.BeginAnimation(
            SolidColorBrush.ColorProperty,
            new ColorAnimation(target, TimeSpan.FromMilliseconds(700)) { EasingFunction = new QuadraticEase() });
    }

    private static Brush CreateGrainBrush()
    {
        const int size = 128;
        var random = new Random(7);
        var pixels = new byte[size * size * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            // Soft grey specks at ≤3% alpha: texture without visible salt-and-pepper dots.
            var value = (byte)(random.Next(2) == 0 ? 80 : 176);
            pixels[i] = pixels[i + 1] = pixels[i + 2] = value;
            pixels[i + 3] = (byte)random.Next(0, 9);
        }

        var bitmap = BitmapSource.Create(size, size, 96, 96, PixelFormats.Bgra32, null, pixels, size * 4);
        var brush = new ImageBrush(bitmap)
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.None,
            ViewportUnits = BrushMappingMode.Absolute,
            Viewport = new Rect(0, 0, size, size)
        };
        brush.Freeze();
        return brush;
    }

    private sealed record FilteredHero(BitmapSource Backdrop, Color Ambient);
}
