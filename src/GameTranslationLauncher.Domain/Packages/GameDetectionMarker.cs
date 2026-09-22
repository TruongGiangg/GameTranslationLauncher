namespace GameTranslationLauncher.Domain.Packages;

/// <summary>
/// Một đường dẫn tương đối bắt buộc phải tồn tại trong thư mục game.
/// </summary>
public sealed record GameDetectionMarker(string Path, GameDetectionMarkerKind Kind);
