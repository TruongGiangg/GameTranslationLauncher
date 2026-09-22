using GameTranslationLauncher.Domain.Settings;
using GameTranslationLauncher.Infrastructure.Persistence.Serialization;

namespace GameTranslationLauncher.Infrastructure.Persistence;

internal static class LauncherSettingsMapper
{
    public static LauncherSettings Map(LauncherSettingsDocument document)
    {
        return new LauncherSettings(
            document.SchemaVersion,
            document.Installations
                .Select(item => new GameInstallationSetting(
                    item.GameId,
                    item.InstallationId,
                    NormalizeRoot(item.GameRoot),
                    item.LastUsedAtUtc))
                .ToArray(),
            document.DisplayLanguage ?? "vi",
            document.AccentTheme ?? "crimson");
    }

    public static LauncherSettingsDocument Map(LauncherSettings settings)
    {
        return new LauncherSettingsDocument
        {
            SchemaVersion = settings.SchemaVersion,
            Installations = settings.Installations
                .Select(item => new GameInstallationSettingDocument
                {
                    GameId = item.GameId,
                    InstallationId = item.InstallationId,
                    GameRoot = item.GameRoot,
                    LastUsedAtUtc = item.LastUsedAtUtc
                })
                .ToList(),
            DisplayLanguage = settings.DisplayLanguage,
            AccentTheme = settings.AccentTheme
        };
    }

    private static string NormalizeRoot(string gameRoot)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(gameRoot));
    }
}
