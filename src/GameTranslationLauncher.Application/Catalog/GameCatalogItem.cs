using GameTranslationLauncher.Domain.Installation;

namespace GameTranslationLauncher.Application.Catalog;

/// <summary>
/// Dữ liệu một game đã ghép package, installation được chọn và trạng thái hiện tại.
/// </summary>
public sealed record GameCatalogItem(
    AvailableGamePackage AvailablePackage,
    Guid? InstallationId,
    string? GameRoot,
    InstallationStatus Status);
