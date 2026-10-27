namespace GameTranslationLauncher.Application.Settings;

/// <summary>
/// Lưu game đang xem mà giữ nguyên preference và installation hiện tại.
/// </summary>
public sealed class SaveLastSelectedGameUseCase
{
    private readonly ILauncherSettingsRepository settingsRepository;

    public SaveLastSelectedGameUseCase(ILauncherSettingsRepository settingsRepository)
    {
        this.settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
    }

    public async Task ExecuteAsync(string gameId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        var settings = await settingsRepository.LoadAsync(cancellationToken);
        if (string.Equals(settings.LastSelectedGameId, gameId, StringComparison.Ordinal))
        {
            return;
        }

        await settingsRepository.SaveAsync(settings with { LastSelectedGameId = gameId }, cancellationToken);
    }
}
