using System.Windows;
using System.Windows.Media;

namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Áp kiểu bề mặt (Glassy/Tối) và màu nhấn dùng chung của WPF; logo và dữ liệu package không đổi.
/// </summary>
public sealed class LauncherThemeManager
{
    private static readonly IReadOnlyDictionary<string, (Color Accent, Color Foreground)> Themes =
        new Dictionary<string, (Color Accent, Color Foreground)>(StringComparer.Ordinal)
        {
            ["crimson"] = (Color.FromRgb(231, 67, 67), Colors.White),
            ["blue"] = (Color.FromRgb(67, 155, 235), Colors.White),
            ["emerald"] = (Color.FromRgb(62, 199, 150), Colors.White),
            ["violet"] = (Color.FromRgb(156, 114, 231), Colors.White),
            ["white"] = (Color.FromRgb(245, 245, 245), Color.FromRgb(17, 18, 20)),
            ["orange"] = (Color.FromRgb(239, 137, 55), Colors.White),
            ["cyan"] = (Color.FromRgb(52, 198, 211), Color.FromRgb(7, 35, 39))
        };

    private static readonly IReadOnlyDictionary<string, Uri> Surfaces =
        new Dictionary<string, Uri>(StringComparer.Ordinal)
        {
            ["glass"] = new("Resources/SurfaceGlass.xaml", UriKind.Relative),
            ["dark"] = new("Resources/SurfaceDark.xaml", UriKind.Relative)
        };

    private string? appliedSurface;

    public void Apply(string accentTheme, string surfaceTheme)
    {
        if (!Themes.TryGetValue(accentTheme, out var theme))
        {
            throw new ArgumentOutOfRangeException(nameof(accentTheme));
        }

        if (!Surfaces.TryGetValue(surfaceTheme, out var surfaceSource))
        {
            throw new ArgumentOutOfRangeException(nameof(surfaceTheme));
        }

        var application = System.Windows.Application.Current
            ?? throw new InvalidOperationException("WPF application chưa được khởi tạo.");
        if (appliedSurface != surfaceTheme)
        {
            // App.xaml keeps the surface dictionary first so it can be swapped without touching the shared styles.
            application.Resources.MergedDictionaries[0] = new ResourceDictionary { Source = surfaceSource };
            appliedSurface = surfaceTheme;
            GlassBackdrop.SetEnabled(surfaceTheme == "glass");
        }

        application.Resources["BrushAccent"] = new SolidColorBrush(theme.Accent);
        application.Resources["BrushButtonPrimaryBackground"] = new SolidColorBrush(theme.Accent);
        application.Resources["BrushButtonPrimaryForeground"] = new SolidColorBrush(theme.Foreground);
    }
}
