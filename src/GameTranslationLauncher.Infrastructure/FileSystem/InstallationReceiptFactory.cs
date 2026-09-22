using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Installation;

namespace GameTranslationLauncher.Infrastructure.FileSystem;

internal static class InstallationReceiptFactory
{
    public static InstallationReceipt Create(
        InstallPlan plan,
        IReadOnlyList<string> newlyCreatedDirectories,
        string launcherVersion)
    {
        var createdDirectories = (plan.PreviousReceipt?.CreatedDirectories ?? [])
            .Concat(newlyCreatedDirectories)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var files = plan.Files.Select(CreateFileReceipt).ToArray();

        return new InstallationReceipt(
            SchemaVersion: 1,
            plan.Installation.InstallationId,
            plan.Installation.GameId,
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(plan.Installation.GameRoot)),
            plan.AvailablePackage.Package.PackageVersion,
            plan.AvailablePackage.ManifestSha256,
            launcherVersion,
            DateTimeOffset.UtcNow,
            createdDirectories,
            files);
    }

    private static InstalledFileReceipt CreateFileReceipt(InstallPlanFile file)
    {
        var ownership = file.PreviousFile?.Ownership ?? file.Action switch
        {
            InstallFileAction.Preserve => InstalledFileOwnership.Preserved,
            InstallFileAction.ReplaceExternal => InstalledFileOwnership.Replaced,
            _ => InstalledFileOwnership.Created
        };

        var backupRelativePath = file.PreviousFile?.BackupRelativePath ?? file.BackupRelativePath;
        var originalSha256 = file.PreviousFile?.OriginalSha256 ?? file.OriginalFileState?.Sha256;
        var originalSizeBytes = file.PreviousFile?.OriginalSizeBytes ?? file.OriginalFileState?.SizeBytes;

        return new InstalledFileReceipt(
            file.PackageFile.Destination,
            file.PackageFile.Sha256,
            file.PackageFile.SizeBytes,
            file.PackageFile.Role,
            file.PackageFile.PrerequisiteId,
            ownership,
            backupRelativePath,
            originalSha256,
            originalSizeBytes);
    }
}
