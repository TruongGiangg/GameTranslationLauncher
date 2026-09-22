using GameTranslationLauncher.Domain.Updates;

namespace GameTranslationLauncher.Application.Updates;

/// <summary>
/// Đọc bản phát hành Launcher mới nhất mà không buộc Application biết nó đến từ đâu.
/// </summary>
public interface IUpdateFeedRepository
{
    Task<LauncherReleaseInfo?> GetLatestReleaseAsync(CancellationToken cancellationToken = default);
}
