namespace GameTranslationLauncher.Application.Catalog;

/// <summary>
/// Lỗi của một manifest riêng lẻ; các package hợp lệ khác vẫn được tải.
/// </summary>
public sealed record GameCatalogError(string ManifestPath, string Message);
