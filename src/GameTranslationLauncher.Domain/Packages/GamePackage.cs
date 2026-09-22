namespace GameTranslationLauncher.Domain.Packages;

/// <summary>
/// Package bản dịch đã được đọc và kiểm tra theo contract mà Launcher hỗ trợ.
/// </summary>
public sealed record GamePackage(
    int SchemaVersion,
    string GameId,
    string DisplayName,
    string PackageVersion,
    string TargetLanguage,
    DateOnly LastVerifiedWorkingInGame,
    GameInstallDetection InstallDetection,
    IReadOnlyList<GamePackageFile> PayloadFiles,
    IReadOnlyList<GamePrerequisite> Prerequisites);
