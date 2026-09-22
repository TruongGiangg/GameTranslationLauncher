using GameTranslationLauncher.Domain.Packages;

namespace GameTranslationLauncher.Application.Installation;

/// <summary>
/// Xác minh một thư mục có đủ marker của game trước khi đọc trạng thái cài đặt.
/// </summary>
public interface IGameInstallationDetector
{
    Task<bool> IsMatchAsync(
        string gameRoot,
        GameInstallDetection detection,
        CancellationToken cancellationToken = default);
}
