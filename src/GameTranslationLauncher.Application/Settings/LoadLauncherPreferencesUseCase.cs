using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Settings;

/// <summary>
/// Đọc preference presentation đã lưu mà không tải catalog hoặc thay đổi state cài đặt.
/// </summary>
public sealed class LoadLauncherPreferencesUseCase
{
    private readonly ILauncherSettingsRepository settingsRepository;

    public LoadLauncherPreferencesUseCase(ILauncherSettingsRepository settingsRepository)
    {
        this.settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
    }

    public Task<LauncherSettings> ExecuteAsync(CancellationToken cancellationToken = default) =>
        settingsRepository.LoadAsync(cancellationToken);
}
