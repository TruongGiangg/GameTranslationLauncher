using GameTranslationLauncher.Domain.Updates;

namespace GameTranslationLauncher.Application.Updates;

/// <summary>
/// Kết quả kiểm tra cập nhật sẵn sàng cho presentation layer.
/// </summary>
public sealed record LauncherUpdateCheckResult(
    LauncherUpdateCheckStatus Status,
    LauncherReleaseInfo? Release);
