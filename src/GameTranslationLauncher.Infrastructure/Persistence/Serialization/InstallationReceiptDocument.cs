namespace GameTranslationLauncher.Infrastructure.Persistence.Serialization;

internal sealed class InstallationReceiptDocument
{
    public required int SchemaVersion { get; init; }

    public required Guid InstallationId { get; init; }

    public required string GameId { get; init; }

    public required string GameRoot { get; init; }

    public required string PackageVersion { get; init; }

    public required string PackageManifestSha256 { get; init; }

    public required string LauncherVersion { get; init; }

    public required DateTimeOffset CompletedAtUtc { get; init; }

    public required List<string> CreatedDirectories { get; init; }

    public required List<InstalledFileReceiptDocument> Files { get; init; }
}

internal sealed class InstalledFileReceiptDocument
{
    public required string Destination { get; init; }

    public required string InstalledSha256 { get; init; }

    public required long InstalledSizeBytes { get; init; }

    public required string Role { get; init; }

    public string? PrerequisiteId { get; init; }

    public required string Ownership { get; init; }

    public string? BackupRelativePath { get; init; }

    public string? OriginalSha256 { get; init; }

    public long? OriginalSizeBytes { get; init; }
}
