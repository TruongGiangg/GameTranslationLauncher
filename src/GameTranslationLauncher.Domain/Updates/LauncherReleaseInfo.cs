namespace GameTranslationLauncher.Domain.Updates;

/// <summary>
/// Thông tin một bản phát hành Launcher lấy được từ nguồn cập nhật từ xa (GitHub Releases).
/// </summary>
public sealed record LauncherReleaseInfo(
    Version Version,
    string DownloadUrl,
    string? Sha256,
    string HtmlUrl,
    string ReleaseNotes);
