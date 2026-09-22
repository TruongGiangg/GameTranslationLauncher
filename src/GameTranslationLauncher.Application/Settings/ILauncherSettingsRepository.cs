using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Settings;

/// <summary>
/// Đọc và ghi settings cục bộ của Launcher.
/// </summary>
public interface ILauncherSettingsRepository
{
    Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken = default);
}
