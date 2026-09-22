namespace GameTranslationLauncher.Application.Catalog;

/// <summary>
/// Kết quả quét catalog local, bao gồm cả package hợp lệ và lỗi độc lập.
/// </summary>
public sealed record GameCatalogSnapshot(
    IReadOnlyList<AvailableGamePackage> Packages,
    IReadOnlyList<GameCatalogError> Errors);
