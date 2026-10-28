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
    string? PrerequisiteId = null,
    IReadOnlyList<string>? OriginalSha256 = null)
{
    /// <summary>
    /// Cho biết file hiện có ở đích là bản gốc của game mà package khai báo được phép thay thế.
    /// </summary>
    public bool IsOriginalFile(string? sha256) =>
        sha256 is not null
        && OriginalSha256?.Contains(sha256, StringComparer.OrdinalIgnoreCase) == true;
}
