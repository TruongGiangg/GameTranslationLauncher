using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Catalog;

/// <summary>
/// Kiểm tra trạng thái duy nhất của game người dùng đang yêu cầu.
/// </summary>
public sealed class CheckGameStatusUseCase
{
    private readonly DetectInstallationStatusUseCase detectStatus;

    public CheckGameStatusUseCase(DetectInstallationStatusUseCase detectStatus)
    {
        this.detectStatus = detectStatus ?? throw new ArgumentNullException(nameof(detectStatus));
    }

    public async Task<GameCatalogItem> ExecuteAsync(
        GameCatalogItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var installation = item is { InstallationId: Guid installationId, GameRoot: not null }
            ? new GameInstallationSetting(
                item.AvailablePackage.Package.GameId,
                installationId,
                item.GameRoot,
                DateTimeOffset.UtcNow)
            : null;
        var status = await detectStatus.ExecuteAsync(
            item.AvailablePackage,
            installation,
            cancellationToken);

        return item with { Status = status };
    }
}
