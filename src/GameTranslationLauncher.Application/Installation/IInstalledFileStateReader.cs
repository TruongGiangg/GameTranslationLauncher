using GameTranslationLauncher.Domain.Installation;

namespace GameTranslationLauncher.Application.Installation;

/// <summary>
/// Đọc tồn tại, kích thước và hash của file đích mà không thay đổi game.
/// </summary>
public interface IInstalledFileStateReader
{
    Task<InstalledFileState> ReadAsync(
        string gameRoot,
        string relativePath,
        CancellationToken cancellationToken = default);
}
