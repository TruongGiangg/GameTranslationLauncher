namespace GameTranslationLauncher.Domain.Packages;

/// <summary>
/// Các marker và process dùng để xác minh game trước khi thay đổi file.
/// </summary>
public sealed record GameInstallDetection(
    IReadOnlyList<GameDetectionMarker> RequiredPaths,
    IReadOnlyList<string> BlockedProcessNames);
