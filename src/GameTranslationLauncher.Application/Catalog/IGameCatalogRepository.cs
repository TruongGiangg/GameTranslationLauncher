namespace GameTranslationLauncher.Application.Catalog;

/// <summary>
/// Cung cấp các package khả dụng mà không buộc Application biết cách chúng được lưu.
/// </summary>
public interface IGameCatalogRepository
{
    Task<GameCatalogSnapshot> LoadAsync(CancellationToken cancellationToken = default);
}
