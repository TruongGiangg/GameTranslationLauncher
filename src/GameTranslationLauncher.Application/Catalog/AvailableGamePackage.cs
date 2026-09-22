using GameTranslationLauncher.Domain.Packages;

namespace GameTranslationLauncher.Application.Catalog;

/// <summary>
/// Package đã hợp lệ cùng vị trí local và hash của manifest đã tải.
/// </summary>
public sealed record AvailableGamePackage(
    GamePackage Package,
    string ManifestPath,
    string ManifestSha256);
