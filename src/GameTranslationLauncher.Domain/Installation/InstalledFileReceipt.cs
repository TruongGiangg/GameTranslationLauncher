using GameTranslationLauncher.Domain.Packages;

namespace GameTranslationLauncher.Domain.Installation;

/// <summary>
/// Bằng chứng về một file đã được Launcher commit thành công vào game.
/// </summary>
public sealed record InstalledFileReceipt(
    string Destination,
    string InstalledSha256,
    long InstalledSizeBytes,
    PackageFileRole Role,
    string? PrerequisiteId,
    InstalledFileOwnership Ownership,
    string? BackupRelativePath,
    string? OriginalSha256,
    long? OriginalSizeBytes);
