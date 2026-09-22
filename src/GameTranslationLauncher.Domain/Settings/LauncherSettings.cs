namespace GameTranslationLauncher.Domain.Settings;

/// <summary>
/// Các lựa chọn cục bộ của Launcher, không chứa dữ liệu package hoặc bí mật.
/// </summary>
public sealed record LauncherSettings(
    int SchemaVersion,
    IReadOnlyList<GameInstallationSetting> Installations,
    string DisplayLanguage = "vi",
    string AccentTheme = "crimson")
{
    public static LauncherSettings Empty { get; } = new(1, []);
}
