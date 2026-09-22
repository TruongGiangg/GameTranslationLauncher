using GameTranslationLauncher.Domain.Updates;

namespace GameTranslationLauncher.Application.Updates;

/// <summary>
/// Tải file cài đặt của bản phát hành đã phát hiện. Ném <see cref="UpdatePackageIntegrityException"/>
/// nếu checksum không khớp — ViewModel không tự ý cài một file chưa xác minh được.
/// </summary>
public sealed class DownloadLauncherUpdateUseCase
{
    private readonly IUpdatePackageDownloader downloader;

    public DownloadLauncherUpdateUseCase(IUpdatePackageDownloader downloader)
    {
        this.downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
    }

    public Task<string> ExecuteAsync(
        LauncherReleaseInfo release,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(release);
        return downloader.DownloadAsync(release, progress, cancellationToken);
    }
}
