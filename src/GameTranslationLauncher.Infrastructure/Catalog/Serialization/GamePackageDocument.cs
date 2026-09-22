namespace GameTranslationLauncher.Infrastructure.Catalog.Serialization;

internal sealed class GamePackageDocument
{
    public required int SchemaVersion { get; init; }

    public required string GameId { get; init; }

    public required string DisplayName { get; init; }

    public required string PackageVersion { get; init; }

    public required string TargetLanguage { get; init; }

    public required string LastVerifiedWorkingInGame { get; init; }

    public required InstallDetectionDocument InstallDetection { get; init; }

    public required List<PayloadFileDocument> PayloadFiles { get; init; }

    public required List<PrerequisiteDocument> Prerequisites { get; init; }
}

internal sealed class InstallDetectionDocument
{
    public required List<DetectionMarkerDocument> RequiredPaths { get; init; }

    public required List<string> BlockedProcessNames { get; init; }
}

internal sealed class DetectionMarkerDocument
{
    public required string Path { get; init; }

    public required string Kind { get; init; }
}

internal sealed class PayloadFileDocument
{
    public required string Source { get; init; }

    public required string Destination { get; init; }

    public required string Sha256 { get; init; }

    public required long SizeBytes { get; init; }

    public required string Role { get; init; }
}

internal sealed class PrerequisiteDocument
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string Description { get; init; }

    public required bool RequiresExplicitConsent { get; init; }

    public required List<PrerequisiteFileDocument> Files { get; init; }
}

internal sealed class PrerequisiteFileDocument
{
    public required string Source { get; init; }

    public required string Destination { get; init; }

    public required string Sha256 { get; init; }

    public required long SizeBytes { get; init; }
}
