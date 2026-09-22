using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Infrastructure.Persistence;

namespace GameTranslationLauncher.Infrastructure.FileSystem;

/// <summary>
/// Chạy kiểm tra cuối ngay trước staging; không thay đổi file game lâu dài.
/// </summary>
public sealed class FileSystemInstallPlanPreflight : IInstallPlanPreflight
{
    private readonly IPackageIntegrityVerifier packageIntegrityVerifier;
    private readonly LauncherStoragePaths storagePaths;

    public FileSystemInstallPlanPreflight(
        IPackageIntegrityVerifier packageIntegrityVerifier,
        LauncherStoragePaths storagePaths)
    {
        this.packageIntegrityVerifier = packageIntegrityVerifier;
        this.storagePaths = storagePaths;
    }

    public async Task ValidateAsync(
        InstallPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        await packageIntegrityVerifier.VerifyAsync(plan.AvailablePackage, cancellationToken);
        EnsureGameIsNotRunning(plan);

        try
        {
            EnsureGameRootIsWritable(plan);
            EnsureEnoughDiskSpace(plan);
            await EnsureDestinationsStillMatchAsync(plan, cancellationToken);
            await EnsureBackupsAreValidAsync(plan, cancellationToken);
            EnsureNewBackupTargetsAreAvailable(plan);
            EnsureMutableFilesAreNotLocked(plan);
        }
        catch (InstallationOperationException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.AccessDenied,
                "Launcher không có quyền thay đổi thư mục game.",
                exception);
        }
        catch (IOException exception)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.FileInUse,
                "Một file cần thay đổi đang được tiến trình khác sử dụng.",
                exception);
        }
    }

    private static void EnsureGameIsNotRunning(InstallPlan plan)
    {
        if (RunningProcessDetector.IsAnyRunning(
            plan.AvailablePackage.Package.InstallDetection.BlockedProcessNames))
        {
            throw new InstallationOperationException(
                InstallationErrorCode.FileInUse,
                "Game đang chạy; hãy đóng game trước khi tiếp tục.");
        }
    }

    private static void EnsureGameRootIsWritable(InstallPlan plan)
    {
        var gameRoot = Path.GetFullPath(plan.Installation.GameRoot);
        if (!Directory.Exists(gameRoot))
        {
            throw new InstallationOperationException(
                InstallationErrorCode.Validation,
                "Thư mục game không còn tồn tại.");
        }

        var probePath = Path.Combine(gameRoot, $".gtl-{plan.OperationId:N}.write-test");
        try
        {
            using var stream = new FileStream(
                probePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);
            stream.WriteByte(0);
            stream.Flush(flushToDisk: true);
        }
        finally
        {
            if (File.Exists(probePath))
            {
                File.Delete(probePath);
            }
        }
    }

    private void EnsureEnoughDiskSpace(InstallPlan plan)
    {
        var requiredBytes = plan.Files
            .Where(file => file.Action != InstallFileAction.Preserve)
            .Sum(file => file.PackageFile.SizeBytes);
        requiredBytes += plan.RemovedFiles
            .Where(file => file.Action == RemovedFileAction.RestoreBackup)
            .Sum(file => file.PreviousFile.OriginalSizeBytes ?? 0);

        var root = Path.GetPathRoot(Path.GetFullPath(plan.Installation.GameRoot));
        if (root is null || new DriveInfo(root).AvailableFreeSpace < requiredBytes)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.InsufficientDiskSpace,
                "Không đủ dung lượng trống để staging package.");
        }

        var backupBytes = plan.Files
            .Where(file => file.Action == InstallFileAction.ReplaceExternal)
            .Sum(file => file.OriginalFileState?.SizeBytes ?? 0);
        if (backupBytes == 0)
        {
            return;
        }

        var backupRoot = storagePaths.GetBackupRoot(
            plan.Installation.GameId,
            plan.Installation.InstallationId);
        var backupDriveRoot = Path.GetPathRoot(Path.GetFullPath(backupRoot));
        if (backupDriveRoot is null || new DriveInfo(backupDriveRoot).AvailableFreeSpace < backupBytes)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.InsufficientDiskSpace,
                "Không đủ dung lượng trống để backup file gốc.");
        }
    }

    private static async Task EnsureDestinationsStillMatchAsync(
        InstallPlan plan,
        CancellationToken cancellationToken)
    {
        foreach (var file in plan.Files)
        {
            var destination = SafeGamePathResolver.Resolve(
                plan.Installation.GameRoot,
                file.PackageFile.Destination);
            if (file.Action == InstallFileAction.Create)
            {
                if (File.Exists(destination))
                {
                    throw Conflict(file.PackageFile.Destination);
                }

                continue;
            }

            var expectedHash = file.Action switch
            {
                InstallFileAction.Preserve => file.PackageFile.Sha256,
                InstallFileAction.ReplaceExternal => file.OriginalFileState?.Sha256,
                _ => file.PreviousFile?.InstalledSha256
            };
            var expectedSize = file.Action switch
            {
                InstallFileAction.Preserve => file.PackageFile.SizeBytes,
                InstallFileAction.ReplaceExternal => file.OriginalFileState?.SizeBytes,
                _ => file.PreviousFile?.InstalledSizeBytes
            };
            await EnsureFileMatchesAsync(
                destination,
                file.PackageFile.Destination,
                expectedHash,
                expectedSize,
                cancellationToken);
        }

        foreach (var removal in plan.RemovedFiles.Where(file => file.Action != RemovedFileAction.Preserve))
        {
            var destination = SafeGamePathResolver.Resolve(
                plan.Installation.GameRoot,
                removal.PreviousFile.Destination);
            await EnsureFileMatchesAsync(
                destination,
                removal.PreviousFile.Destination,
                removal.PreviousFile.InstalledSha256,
                removal.PreviousFile.InstalledSizeBytes,
                cancellationToken);
        }
    }

    private async Task EnsureBackupsAreValidAsync(
        InstallPlan plan,
        CancellationToken cancellationToken)
    {
        var filesNeedingBackup = plan.Files
            .Where(file => file.PreviousFile?.Ownership == InstalledFileOwnership.Replaced)
            .Select(file => file.PreviousFile!)
            .Concat(plan.RemovedFiles
                .Where(file => file.Action == RemovedFileAction.RestoreBackup)
                .Select(file => file.PreviousFile))
            .DistinctBy(file => file.Destination, StringComparer.OrdinalIgnoreCase);

        foreach (var file in filesNeedingBackup)
        {
            var backupPath = ResolveBackupPath(plan, file);
            await EnsureFileMatchesAsync(
                backupPath,
                file.BackupRelativePath!,
                file.OriginalSha256,
                file.OriginalSizeBytes,
                cancellationToken);
        }
    }

    private static void EnsureMutableFilesAreNotLocked(InstallPlan plan)
    {
        var destinations = plan.Files
            .Where(file => file.Action is InstallFileAction.ReplaceOwned or InstallFileAction.ReplaceExternal)
            .Select(file => file.PackageFile.Destination)
            .Concat(plan.RemovedFiles
                .Where(file => file.Action != RemovedFileAction.Preserve)
                .Select(file => file.PreviousFile.Destination))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var relativePath in destinations)
        {
            var path = SafeGamePathResolver.Resolve(plan.Installation.GameRoot, relativePath);
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);
        }
    }

    private void EnsureNewBackupTargetsAreAvailable(InstallPlan plan)
    {
        foreach (var file in plan.Files.Where(file => file.Action == InstallFileAction.ReplaceExternal))
        {
            if (file.BackupRelativePath is null)
            {
                throw Conflict(file.PackageFile.Destination);
            }

            var backupPath = storagePaths.GetBackupPath(
                plan.Installation.GameId,
                plan.Installation.InstallationId,
                file.BackupRelativePath);
            if (File.Exists(backupPath))
            {
                throw new InstallationOperationException(
                    InstallationErrorCode.Conflict,
                    $"Backup cho '{file.PackageFile.Destination}' đã tồn tại ngoài transaction hiện tại.");
            }
        }
    }

    private string ResolveBackupPath(InstallPlan plan, InstalledFileReceipt file)
    {
        if (file.BackupRelativePath is null)
        {
            throw Conflict(file.Destination);
        }

        return storagePaths.GetBackupPath(
            plan.Installation.GameId,
            plan.Installation.InstallationId,
            file.BackupRelativePath);
    }

    private static async Task EnsureFileMatchesAsync(
        string path,
        string displayPath,
        string? expectedHash,
        long? expectedSize,
        CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length != expectedSize)
        {
            throw Conflict(displayPath);
        }

        var hash = await Sha256File.ComputeAsync(path, cancellationToken);
        if (!string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw Conflict(displayPath);
        }
    }

    private static InstallationOperationException Conflict(string relativePath) =>
        new(
            InstallationErrorCode.Conflict,
            $"File '{relativePath}' đã thay đổi sau khi lập kế hoạch.");
}
