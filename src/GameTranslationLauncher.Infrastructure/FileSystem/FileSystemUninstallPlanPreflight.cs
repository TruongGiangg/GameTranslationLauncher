using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Infrastructure.Persistence;

namespace GameTranslationLauncher.Infrastructure.FileSystem;

public sealed class FileSystemUninstallPlanPreflight : IUninstallPlanPreflight
{
    private readonly LauncherStoragePaths storagePaths;

    public FileSystemUninstallPlanPreflight(LauncherStoragePaths storagePaths)
    {
        this.storagePaths = storagePaths;
    }

    public async Task ValidateAsync(
        UninstallPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        try
        {
            EnsureRootIsWritable(plan);
            EnsureEnoughDiskSpace(plan);
            await EnsureFilesStillMatchAsync(plan, cancellationToken);
            await EnsureBackupsAreValidAsync(plan, cancellationToken);
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
                "Launcher không có quyền gỡ file trong thư mục game.",
                exception);
        }
        catch (IOException exception)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.FileInUse,
                "Một file cần gỡ đang được tiến trình khác sử dụng.",
                exception);
        }
    }

    private static void EnsureRootIsWritable(UninstallPlan plan)
    {
        if (!Directory.Exists(plan.Receipt.GameRoot))
        {
            throw new InstallationOperationException(
                InstallationErrorCode.Validation,
                "Thư mục game trong receipt không còn tồn tại.");
        }

        var probePath = Path.Combine(
            plan.Receipt.GameRoot,
            $".gtl-{plan.OperationId:N}.write-test");
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

    private static void EnsureEnoughDiskSpace(UninstallPlan plan)
    {
        var requiredBytes = plan.Files
            .Where(file => file.Action == RemovedFileAction.RestoreBackup)
            .Sum(file => file.PreviousFile.OriginalSizeBytes ?? 0);
        var driveRoot = Path.GetPathRoot(Path.GetFullPath(plan.Receipt.GameRoot));
        if (driveRoot is null || new DriveInfo(driveRoot).AvailableFreeSpace < requiredBytes)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.InsufficientDiskSpace,
                "Không đủ dung lượng trống để khôi phục backup.");
        }
    }

    private static async Task EnsureFilesStillMatchAsync(
        UninstallPlan plan,
        CancellationToken cancellationToken)
    {
        foreach (var item in plan.Files.Where(file => file.Action != RemovedFileAction.Preserve))
        {
            var file = item.PreviousFile;
            var destination = SafeGamePathResolver.Resolve(plan.Receipt.GameRoot, file.Destination);
            if (!File.Exists(destination))
            {
                continue;
            }

            var info = new FileInfo(destination);
            var hash = await Sha256File.ComputeAsync(destination, cancellationToken);
            if (info.Length != file.InstalledSizeBytes
                || !string.Equals(hash, file.InstalledSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw Conflict(file.Destination);
            }
        }
    }

    private async Task EnsureBackupsAreValidAsync(
        UninstallPlan plan,
        CancellationToken cancellationToken)
    {
        foreach (var item in plan.Files.Where(file => file.Action == RemovedFileAction.RestoreBackup))
        {
            var file = item.PreviousFile;
            if (file.BackupRelativePath is null
                || file.OriginalSha256 is null
                || file.OriginalSizeBytes is null)
            {
                throw Conflict(file.Destination);
            }

            var backupPath = storagePaths.GetBackupPath(
                plan.Receipt.GameId,
                plan.Receipt.InstallationId,
                file.BackupRelativePath);
            var info = new FileInfo(backupPath);
            if (!info.Exists || info.Length != file.OriginalSizeBytes)
            {
                throw Conflict(file.Destination);
            }

            var hash = await Sha256File.ComputeAsync(backupPath, cancellationToken);
            if (!string.Equals(hash, file.OriginalSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw Conflict(file.Destination);
            }
        }
    }

    private static void EnsureMutableFilesAreNotLocked(UninstallPlan plan)
    {
        foreach (var item in plan.Files.Where(file => file.Action != RemovedFileAction.Preserve))
        {
            var destination = SafeGamePathResolver.Resolve(
                plan.Receipt.GameRoot,
                item.PreviousFile.Destination);
            if (!File.Exists(destination))
            {
                continue;
            }

            using var stream = new FileStream(
                destination,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);
        }
    }

    private static InstallationOperationException Conflict(string relativePath) =>
        new(
            InstallationErrorCode.Conflict,
            $"Không thể gỡ an toàn file '{relativePath}'.");
}
