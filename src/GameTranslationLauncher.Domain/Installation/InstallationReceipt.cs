namespace GameTranslationLauncher.Domain.Installation;

/// <summary>
/// Trạng thái đã commit của một package trên một thư mục game cụ thể.
/// </summary>
public sealed record InstallationReceipt(
    int SchemaVersion,
    Guid InstallationId,
    string GameId,
    string GameRoot,
    string PackageVersion,
    string PackageManifestSha256,
    string LauncherVersion,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<string> CreatedDirectories,
    IReadOnlyList<InstalledFileReceipt> Files);
