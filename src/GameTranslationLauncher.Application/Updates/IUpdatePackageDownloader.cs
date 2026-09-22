using GameTranslationLauncher.Domain.Updates;

namespace GameTranslationLauncher.Application.Updates;

/// <summary>
/// Tải file cài đặt của một bản phát hành về thư mục tạm và xác minh toàn vẹn nếu có checksum.
/// </summary>
public interface IUpdatePackageDownloader
{
    /// <returns>Đường dẫn cục bộ tới file cài đặt đã tải.</returns>
    Task<string> DownloadAsync(
        LauncherReleaseInfo release,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}
