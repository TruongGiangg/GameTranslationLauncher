namespace GameTranslationLauncher.Domain.Packages;

/// <summary>
/// Mô tả một file tự chứa trong package và vị trí tương đối của nó trong game.
/// </summary>
public sealed record GamePackageFile(
    string Source,
    string Destination,
    string Sha256,
    long SizeBytes,
    PackageFileRole Role,
    string? PrerequisiteId = null);
