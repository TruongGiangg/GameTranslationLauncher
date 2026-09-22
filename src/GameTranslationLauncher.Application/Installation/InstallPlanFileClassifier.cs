using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Domain.Packages;

namespace GameTranslationLauncher.Application.Installation;

/// <summary>
/// Phân loại từng destination theo trạng thái thực tế và ownership trong receipt.
/// </summary>
internal sealed class InstallPlanFileClassifier
{
    private readonly IInstalledFileStateReader fileStateReader;

    public InstallPlanFileClassifier(IInstalledFileStateReader fileStateReader)
    {
        this.fileStateReader = fileStateReader;
    }

    public async Task<IReadOnlyList<InstallPlanFile>> BuildWithoutReceiptAsync(
        ApplyTranslationRequest request,
        CancellationToken cancellationToken)
    {
        var files = new List<InstallPlanFile>();
        foreach (var packageFile in GamePackageFileEnumerator.Enumerate(request.AvailablePackage.Package))
        {
            var state = await fileStateReader.ReadAsync(
                request.Installation.GameRoot,
                packageFile.Destination,
                cancellationToken);
            files.Add(ClassifyUnownedDestination(request, packageFile, state));
        }

        return files;
    }

    public async Task<IReadOnlyList<InstallPlanFile>> BuildWithReceiptAsync(
        InstallationReceipt receipt,
        ApplyTranslationRequest request,
        CancellationToken cancellationToken)
    {
        var previousByDestination = receipt.Files.ToDictionary(
            file => file.Destination,
            StringComparer.OrdinalIgnoreCase);
        var files = new List<InstallPlanFile>();

        foreach (var packageFile in GamePackageFileEnumerator.Enumerate(request.AvailablePackage.Package))
        {
            if (!previousByDestination.TryGetValue(packageFile.Destination, out var previousFile))
            {
                var state = await fileStateReader.ReadAsync(
                    request.Installation.GameRoot,
                    packageFile.Destination,
                    cancellationToken);
                files.Add(ClassifyUnownedDestination(request, packageFile, state));
                continue;
            }

            files.Add(ClassifyOwnedDestination(packageFile, previousFile));
        }

        return files;
    }

    public static IReadOnlyList<InstallPlanRemoval> BuildRemovals(
        InstallationReceipt receipt,
        IReadOnlyList<InstallPlanFile> files)
    {
        var currentDestinations = files
            .Select(file => file.PackageFile.Destination)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return receipt.Files
            .Where(file => !currentDestinations.Contains(file.Destination))
            .Select(file => new InstallPlanRemoval(file, file.Ownership switch
            {
                InstalledFileOwnership.Created => RemovedFileAction.DeleteOwned,
                InstalledFileOwnership.Replaced => RemovedFileAction.RestoreBackup,
                InstalledFileOwnership.Preserved => RemovedFileAction.Preserve,
                _ => throw new ArgumentOutOfRangeException(nameof(file.Ownership), file.Ownership, null)
            }))
            .ToArray();
    }

    private static InstallPlanFile ClassifyUnownedDestination(
        ApplyTranslationRequest request,
        GamePackageFile packageFile,
        InstalledFileState state)
    {
        if (!state.Exists)
        {
            return new InstallPlanFile(packageFile, InstallFileAction.Create, null);
        }

        if (packageFile.Role == PackageFileRole.Prerequisite
            && StateMatchesPackageFile(state, packageFile))
        {
            return new InstallPlanFile(packageFile, InstallFileAction.Preserve, null);
        }

        if (request.ApprovedReplacementDestinations?.Contains(packageFile.Destination) == true)
        {
            return CreateExternalReplacement(packageFile, state);
        }

        throw Conflict(packageFile.Destination);
    }

    private static InstallPlanFile ClassifyOwnedDestination(
        GamePackageFile packageFile,
        InstalledFileReceipt previousFile)
    {
        if (previousFile.Role != packageFile.Role
            || !string.Equals(
                previousFile.PrerequisiteId,
                packageFile.PrerequisiteId,
                StringComparison.OrdinalIgnoreCase))
        {
            throw Conflict(packageFile.Destination);
        }

        if (ReceiptMatchesPackageFile(previousFile, packageFile))
        {
            return new InstallPlanFile(packageFile, InstallFileAction.Preserve, previousFile);
        }

        if (previousFile.Ownership == InstalledFileOwnership.Preserved)
        {
            throw Conflict(packageFile.Destination);
        }

        return new InstallPlanFile(packageFile, InstallFileAction.ReplaceOwned, previousFile);
    }

    private static InstallPlanFile CreateExternalReplacement(
        GamePackageFile packageFile,
        InstalledFileState state)
    {
        if (state.Sha256 is null || state.SizeBytes is null)
        {
            throw Conflict(packageFile.Destination);
        }

        return new InstallPlanFile(
            packageFile,
            InstallFileAction.ReplaceExternal,
            null,
            state,
            $"{packageFile.Destination}.original");
    }

    private static bool StateMatchesPackageFile(
        InstalledFileState state,
        GamePackageFile packageFile) =>
        state.SizeBytes == packageFile.SizeBytes
        && string.Equals(state.Sha256, packageFile.Sha256, StringComparison.OrdinalIgnoreCase);

    private static bool ReceiptMatchesPackageFile(
        InstalledFileReceipt receipt,
        GamePackageFile packageFile) =>
        receipt.InstalledSizeBytes == packageFile.SizeBytes
        && string.Equals(receipt.InstalledSha256, packageFile.Sha256, StringComparison.OrdinalIgnoreCase);

    private static InstallationOperationException Conflict(string destination) =>
        new(
            InstallationErrorCode.Conflict,
            $"Không thể xác định ownership an toàn cho '{destination}'.");
}
