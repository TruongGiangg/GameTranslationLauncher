using GameTranslationLauncher.Domain.Installation;

namespace GameTranslationLauncher.Application.Installation;

/// <summary>
/// Kiểm tra file owned trước khi lập kế hoạch gỡ; file preserved luôn được giữ lại.
/// </summary>
public sealed class BuildUninstallPlanUseCase
{
    private readonly IInstalledFileStateReader fileStateReader;

    public BuildUninstallPlanUseCase(IInstalledFileStateReader fileStateReader)
    {
        this.fileStateReader = fileStateReader;
    }

    public async Task<UninstallPlan> ExecuteAsync(
        Guid operationId,
        InstallationReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        var files = new List<InstallPlanRemoval>();

        foreach (var file in receipt.Files)
        {
            var action = MapAction(file.Ownership);
            if (file.Ownership != InstalledFileOwnership.Preserved)
            {
                await ValidateOwnedFileAsync(receipt.GameRoot, file, cancellationToken);
            }

            files.Add(new InstallPlanRemoval(file, action));
        }

        return new UninstallPlan(operationId, receipt, files);
    }

    private async Task ValidateOwnedFileAsync(
        string gameRoot,
        InstalledFileReceipt file,
        CancellationToken cancellationToken)
    {
        var state = await fileStateReader.ReadAsync(gameRoot, file.Destination, cancellationToken);
        if (!state.Exists)
        {
            return;
        }

        if (state.SizeBytes != file.InstalledSizeBytes
            || !string.Equals(state.Sha256, file.InstalledSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InstallationOperationException(
                InstallationErrorCode.Conflict,
                $"File '{file.Destination}' đã bị thay đổi ngoài Launcher.");
        }
    }

    private static RemovedFileAction MapAction(InstalledFileOwnership ownership) => ownership switch
    {
        InstalledFileOwnership.Created => RemovedFileAction.DeleteOwned,
        InstalledFileOwnership.Replaced => RemovedFileAction.RestoreBackup,
        InstalledFileOwnership.Preserved => RemovedFileAction.Preserve,
        _ => throw new ArgumentOutOfRangeException(nameof(ownership), ownership, null)
    };
}
