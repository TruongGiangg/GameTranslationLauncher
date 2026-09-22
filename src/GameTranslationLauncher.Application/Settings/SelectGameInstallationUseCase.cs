using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Settings;

/// <summary>
/// Xác minh thư mục do người dùng chọn rồi lưu nó thành installation được dùng gần nhất.
/// </summary>
public sealed class SelectGameInstallationUseCase
{
    private readonly IGameInstallationDetector installationDetector;
    private readonly ILauncherSettingsRepository settingsRepository;

    public SelectGameInstallationUseCase(
        IGameInstallationDetector installationDetector,
        ILauncherSettingsRepository settingsRepository)
    {
        this.installationDetector = installationDetector;
        this.settingsRepository = settingsRepository;
    }

    public async Task<GameInstallationSetting> ExecuteAsync(
        AvailableGamePackage availablePackage,
        string selectedDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(availablePackage);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedDirectory);

        var gameRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(selectedDirectory));
        var isMatchingGame = await installationDetector.IsMatchAsync(
            gameRoot,
            availablePackage.Package.InstallDetection,
            cancellationToken);
        if (!isMatchingGame)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.Validation,
                "Thư mục đã chọn không khớp game trong package.");
        }

        var settings = await settingsRepository.LoadAsync(cancellationToken);
        var existing = settings.Installations
            .Where(item => string.Equals(
                item.GameId,
                availablePackage.Package.GameId,
                StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(item => string.Equals(
                item.GameRoot,
                gameRoot,
                StringComparison.OrdinalIgnoreCase));
        var installation = new GameInstallationSetting(
            availablePackage.Package.GameId,
            existing?.InstallationId ?? Guid.NewGuid(),
            gameRoot,
            DateTimeOffset.UtcNow);
        var updatedInstallations = settings.Installations
            .Where(item => item.InstallationId != installation.InstallationId)
            .Append(installation)
            .ToArray();

        await settingsRepository.SaveAsync(
            new LauncherSettings(
                settings.SchemaVersion,
                updatedInstallations,
                settings.DisplayLanguage,
                settings.AccentTheme),
            cancellationToken);
        return installation;
    }
}
