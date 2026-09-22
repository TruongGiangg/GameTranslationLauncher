using GameTranslationLauncher.Application.Settings;
using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Catalog;

/// <summary>
/// Tải catalog và ghép mỗi package với installation được dùng gần nhất.
/// Không kiểm tra thư mục game, receipt hoặc hash trong luồng khởi động.
/// </summary>
public sealed class LoadGameCatalogUseCase
{
    private readonly IGameCatalogRepository catalogRepository;
    private readonly ILauncherSettingsRepository settingsRepository;
    public LoadGameCatalogUseCase(
        IGameCatalogRepository catalogRepository,
        ILauncherSettingsRepository settingsRepository)
    {
        this.catalogRepository = catalogRepository;
        this.settingsRepository = settingsRepository;
    }

    public async Task<GameCatalogResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var catalogTask = catalogRepository.LoadAsync(cancellationToken);
        var settingsTask = settingsRepository.LoadAsync(cancellationToken);
        await Task.WhenAll(catalogTask, settingsTask);

        var catalog = await catalogTask;
        var settings = await settingsTask;
        var items = new List<GameCatalogItem>(catalog.Packages.Count);

        foreach (var availablePackage in catalog.Packages)
        {
            var installation = FindMostRecentInstallation(
                settings,
                availablePackage.Package.GameId);
            items.Add(new GameCatalogItem(
                availablePackage,
                installation?.InstallationId,
                installation?.GameRoot,
                Domain.Installation.InstallationStatus.Unknown));
        }

        return new GameCatalogResult(items, catalog.Errors);
    }

    private static GameInstallationSetting? FindMostRecentInstallation(
        LauncherSettings settings,
        string gameId)
    {
        return settings.Installations
            .Where(item => string.Equals(item.GameId, gameId, StringComparison.OrdinalIgnoreCase))
            .MaxBy(item => item.LastUsedAtUtc);
    }
}
