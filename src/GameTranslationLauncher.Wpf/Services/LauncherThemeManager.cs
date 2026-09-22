using System.Windows;
using System.Windows.Media;

namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Cập nhật màu nhấn dùng chung của WPF, giữ nguyên bề mặt tối và tài sản thương hiệu.
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

    public void Apply(string accentTheme)
    {
        if (!Themes.TryGetValue(accentTheme, out var theme))
        {
            throw new ArgumentOutOfRangeException(nameof(accentTheme));
        }

        SetBrushColor("BrushAccent", theme.Accent);
        SetBrushColor("BrushButtonPrimaryBackground", theme.Accent);
        SetBrushColor("BrushButtonPrimaryForeground", theme.Foreground);
    }

    private static void SetBrushColor(string resourceKey, Color color)
    {
        var application = System.Windows.Application.Current;
        if (application is null)
        {
            throw new InvalidOperationException("WPF application chưa được khởi tạo.");
        }

        application.Resources[resourceKey] = new SolidColorBrush(color);
    }
}
