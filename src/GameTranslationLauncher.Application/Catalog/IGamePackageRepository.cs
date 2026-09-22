using GameTranslationLauncher.Domain.Packages;

namespace GameTranslationLauncher.Application.Catalog;

/// <summary>
/// Đọc một package manifest và chuyển nó thành mô hình domain đã được kiểm tra.
/// </summary>
public interface IGamePackageRepository
{
    Task<GamePackage> LoadAsync(
        string manifestPath,
        CancellationToken cancellationToken = default);
}
