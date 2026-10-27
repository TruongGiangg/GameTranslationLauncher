using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Settings;

/// <summary>
/// Lưu lựa chọn presentation cục bộ mà không làm thay đổi installation đã xác minh.
/// </summary>
public sealed class SaveLauncherPreferencesUseCase
{
    private static readonly HashSet<string> SupportedLanguages = ["vi", "en"];
    private static readonly HashSet<string> SupportedAccentThemes =
        ["crimson", "blue", "emerald", "violet", "white", "orange", "cyan"];
    private static readonly HashSet<string> SupportedSurfaceThemes = ["glass", "dark"];
    private readonly ILauncherSettingsRepository settingsRepository;

    public SaveLauncherPreferencesUseCase(ILauncherSettingsRepository settingsRepository)
    {
        this.settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
    }

    public async Task<LauncherSettings> ExecuteAsync(
        string displayLanguage,
        string accentTheme,
        string surfaceTheme,
        CancellationToken cancellationToken = default)
    {
        if (!SupportedLanguages.Contains(displayLanguage))
        {
            throw new ArgumentOutOfRangeException(nameof(displayLanguage));
        }

        if (!SupportedAccentThemes.Contains(accentTheme))
        {
            throw new ArgumentOutOfRangeException(nameof(accentTheme));
        }

        if (!SupportedSurfaceThemes.Contains(surfaceTheme))
        {
            throw new ArgumentOutOfRangeException(nameof(surfaceTheme));
        }

        var settings = await settingsRepository.LoadAsync(cancellationToken);
        var updatedSettings = settings with
        {
            DisplayLanguage = displayLanguage,
            AccentTheme = accentTheme,
            SurfaceTheme = surfaceTheme
        };
        await settingsRepository.SaveAsync(updatedSettings, cancellationToken);
        return updatedSettings;
    }
}
