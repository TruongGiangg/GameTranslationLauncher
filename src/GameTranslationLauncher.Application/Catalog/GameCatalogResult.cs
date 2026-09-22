namespace GameTranslationLauncher.Application.Catalog;

/// <summary>
/// Kết quả use case catalog sẵn sàng cho presentation layer.
/// </summary>
public sealed record GameCatalogResult(
    IReadOnlyList<GameCatalogItem> Items,
    IReadOnlyList<GameCatalogError> Errors);
