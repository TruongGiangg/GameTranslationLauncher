namespace GameTranslationLauncher.Application.Updates;

/// <summary>
/// So sánh version Launcher đang chạy với bản phát hành mới nhất trên nguồn cập nhật từ xa.
/// Lỗi mạng hoặc dữ liệu phản hồi không hợp lệ được coi là "kiểm tra thất bại", không phải
/// crash — Launcher vẫn phải dùng được bình thường khi không có mạng.
/// </summary>
public sealed class CheckForLauncherUpdateUseCase
{
    private readonly IUpdateFeedRepository feedRepository;

    public CheckForLauncherUpdateUseCase(IUpdateFeedRepository feedRepository)
    {
        this.feedRepository = feedRepository;
    }

    public async Task<LauncherUpdateCheckResult> ExecuteAsync(
        Version currentVersion,
        CancellationToken cancellationToken = default)
    {
        Domain.Updates.LauncherReleaseInfo? latest;
        try
        {
            latest = await feedRepository.GetLatestReleaseAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new LauncherUpdateCheckResult(LauncherUpdateCheckStatus.CheckFailed, null);
        }

        if (latest is null || latest.Version <= currentVersion)
        {
            return new LauncherUpdateCheckResult(LauncherUpdateCheckStatus.UpToDate, null);
        }

        return new LauncherUpdateCheckResult(LauncherUpdateCheckStatus.UpdateAvailable, latest);
    }
}
