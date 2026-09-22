using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Infrastructure.Persistence.Serialization;

namespace GameTranslationLauncher.Infrastructure.Persistence;

internal static class InstallationReceiptMapper
{
    public static InstallationReceipt Map(InstallationReceiptDocument document)
    {
        return new InstallationReceipt(
            document.SchemaVersion,
            document.InstallationId,
            document.GameId,
            NormalizeRoot(document.GameRoot),
            document.PackageVersion,
            document.PackageManifestSha256,
            document.LauncherVersion,
            document.CompletedAtUtc,
            document.CreatedDirectories.ToArray(),
            document.Files.Select(MapFile).ToArray());
    }

    public static InstallationReceiptDocument Map(InstallationReceipt receipt)
    {
        return new InstallationReceiptDocument
        {
            SchemaVersion = receipt.SchemaVersion,
            InstallationId = receipt.InstallationId,
            GameId = receipt.GameId,
            GameRoot = receipt.GameRoot,
            PackageVersion = receipt.PackageVersion,
            PackageManifestSha256 = receipt.PackageManifestSha256,
            LauncherVersion = receipt.LauncherVersion,
            CompletedAtUtc = receipt.CompletedAtUtc,
            CreatedDirectories = receipt.CreatedDirectories.ToList(),
            Files = receipt.Files.Select(MapFile).ToList()
        };
    }

    private static InstalledFileReceipt MapFile(InstalledFileReceiptDocument file)
    {
        return new InstalledFileReceipt(
            file.Destination,
            file.InstalledSha256,
            file.InstalledSizeBytes,
            MapRole(file.Role),
            file.PrerequisiteId,
            MapOwnership(file.Ownership),
            file.BackupRelativePath,
            file.OriginalSha256,
            file.OriginalSizeBytes);
    }

    private static InstalledFileReceiptDocument MapFile(InstalledFileReceipt file)
    {
        return new InstalledFileReceiptDocument
        {
            Destination = file.Destination,
            InstalledSha256 = file.InstalledSha256,
            InstalledSizeBytes = file.InstalledSizeBytes,
            Role = MapRole(file.Role),
            PrerequisiteId = file.PrerequisiteId,
            Ownership = MapOwnership(file.Ownership),
            BackupRelativePath = file.BackupRelativePath,
            OriginalSha256 = file.OriginalSha256,
            OriginalSizeBytes = file.OriginalSizeBytes
        };
    }

    private static PackageFileRole MapRole(string role)
    {
        return role switch
        {
            "translation" => PackageFileRole.Translation,
            "companion" => PackageFileRole.Companion,
            "prerequisite" => PackageFileRole.Prerequisite,
            _ => throw new InvalidOperationException($"Receipt role '{role}' chưa được kiểm tra.")
        };
    }

    private static string MapRole(PackageFileRole role)
    {
        return role switch
        {
            PackageFileRole.Translation => "translation",
            PackageFileRole.Companion => "companion",
            PackageFileRole.Prerequisite => "prerequisite",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }

    private static InstalledFileOwnership MapOwnership(string ownership)
    {
        return ownership switch
        {
            "created" => InstalledFileOwnership.Created,
            "replaced" => InstalledFileOwnership.Replaced,
            "preserved" => InstalledFileOwnership.Preserved,
            _ => throw new InvalidOperationException($"Receipt ownership '{ownership}' chưa được kiểm tra.")
        };
    }

    private static string MapOwnership(InstalledFileOwnership ownership)
    {
        return ownership switch
        {
            InstalledFileOwnership.Created => "created",
            InstalledFileOwnership.Replaced => "replaced",
            InstalledFileOwnership.Preserved => "preserved",
            _ => throw new ArgumentOutOfRangeException(nameof(ownership), ownership, null)
        };
    }

    private static string NormalizeRoot(string gameRoot)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(gameRoot));
    }
}
