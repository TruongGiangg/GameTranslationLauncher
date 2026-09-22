using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Installation;

/// <summary>
/// Ghép package, receipt và file thực tế để xác định trạng thái cài đặt hiện tại.
/// </summary>
public sealed class DetectInstallationStatusUseCase
{
    private readonly IGameInstallationDetector installationDetector;
    private readonly IInstallationReceiptRepository receiptRepository;
    private readonly IInstalledFileStateReader fileStateReader;

    public DetectInstallationStatusUseCase(
        IGameInstallationDetector installationDetector,
        IInstallationReceiptRepository receiptRepository,
        IInstalledFileStateReader fileStateReader)
    {
        this.installationDetector = installationDetector;
        this.receiptRepository = receiptRepository;
        this.fileStateReader = fileStateReader;
    }

    public async Task<InstallationStatus> ExecuteAsync(
        AvailableGamePackage availablePackage,
        GameInstallationSetting? installation,
        CancellationToken cancellationToken = default)
    {
        if (installation is null)
        {
            return InstallationStatus.Unknown;
        }

        var isMatchingGame = await installationDetector.IsMatchAsync(
            installation.GameRoot,
            availablePackage.Package.InstallDetection,
            cancellationToken);
        if (!isMatchingGame)
        {
            return InstallationStatus.Unknown;
        }

        var receipt = await receiptRepository.FindAsync(
            availablePackage.Package.GameId,
            installation.InstallationId,
            cancellationToken);
        if (receipt is null)
        {
            return await DetectWithoutReceiptAsync(
                availablePackage.Package,
                installation.GameRoot,
                cancellationToken);
        }

        if (!ReceiptMatchesInstallation(receipt, installation, availablePackage.Package.GameId))
        {
            return InstallationStatus.Unknown;
        }

        if (!await ReceiptFilesAreIntactAsync(receipt, cancellationToken))
        {
            return InstallationStatus.Damaged;
        }

        var availableVersion = Version.Parse(availablePackage.Package.PackageVersion);
        var installedVersion = Version.Parse(receipt.PackageVersion);
        if (availableVersion > installedVersion)
        {
            return InstallationStatus.UpdateAvailable;
        }

        if (availableVersion == installedVersion
            && !string.Equals(
                availablePackage.ManifestSha256,
                receipt.PackageManifestSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            return InstallationStatus.Conflict;
        }

        return InstallationStatus.Installed;
    }

    private async Task<InstallationStatus> DetectWithoutReceiptAsync(
        GamePackage package,
        string gameRoot,
        CancellationToken cancellationToken)
    {
        foreach (var file in GamePackageFileEnumerator.Enumerate(package))
        {
            var state = await fileStateReader.ReadAsync(gameRoot, file.Destination, cancellationToken);
            if (!state.Exists)
            {
                continue;
            }

            if (file.Role == PackageFileRole.Prerequisite
                && state.SizeBytes == file.SizeBytes
                && string.Equals(state.Sha256, file.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return InstallationStatus.Conflict;
        }

        return InstallationStatus.NotInstalled;
    }

    private async Task<bool> ReceiptFilesAreIntactAsync(
        InstallationReceipt receipt,
        CancellationToken cancellationToken)
    {
        foreach (var file in receipt.Files)
        {
            var state = await fileStateReader.ReadAsync(
                receipt.GameRoot,
                file.Destination,
                cancellationToken);
            if (!state.Exists
                || state.SizeBytes != file.InstalledSizeBytes
                || !string.Equals(state.Sha256, file.InstalledSha256, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ReceiptMatchesInstallation(
        InstallationReceipt receipt,
        GameInstallationSetting installation,
        string gameId)
    {
        return receipt.InstallationId == installation.InstallationId
            && string.Equals(receipt.GameId, gameId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(receipt.GameRoot, installation.GameRoot, StringComparison.OrdinalIgnoreCase);
    }

}
