using GameTranslationLauncher.Domain.Installation;

namespace GameTranslationLauncher.Application.Installation;

/// <summary>
/// Lưu bằng chứng cài đặt theo game và installation identity.
/// </summary>
public interface IInstallationReceiptRepository
{
    Task<InstallationReceipt?> FindAsync(
        string gameId,
        Guid installationId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        InstallationReceipt receipt,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string gameId,
        Guid installationId,
        CancellationToken cancellationToken = default);
}
