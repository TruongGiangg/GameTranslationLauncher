using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Infrastructure.Persistence;

namespace GameTranslationLauncher.Infrastructure.FileSystem;

internal sealed class OriginalFileBackupStore
{
    private readonly LauncherStoragePaths storagePaths;

    public OriginalFileBackupStore(LauncherStoragePaths storagePaths)
    {
        this.storagePaths = storagePaths;
    }

    public async Task<string> CreateAsync(
        InstallPlan plan,
        InstallPlanFile file,
        CancellationToken cancellationToken)
    {
        if (file.Action != InstallFileAction.ReplaceExternal
            || file.BackupRelativePath is null
            || file.OriginalFileState?.Sha256 is null
            || file.OriginalFileState.SizeBytes is null)
        {
            throw new ArgumentException("File plan không đủ dữ liệu để backup.", nameof(file));
        }

        var sourcePath = SafeGamePathResolver.Resolve(
            plan.Installation.GameRoot,
            file.PackageFile.Destination);
        var backupPath = storagePaths.GetBackupPath(
            plan.Installation.GameId,
            plan.Installation.InstallationId,
            file.BackupRelativePath);
        var backupDirectory = Path.GetDirectoryName(backupPath)
            ?? throw new ArgumentException("Backup không có thư mục cha.", nameof(file));
        Directory.CreateDirectory(backupDirectory);

        try
        {
            await CopyAsync(sourcePath, backupPath, cancellationToken);
            await VerifyAsync(
                backupPath,
                file.OriginalFileState.Sha256,
                file.OriginalFileState.SizeBytes.Value,
                cancellationToken);
            return backupPath;
        }
        catch
        {
            _ = TryDelete([backupPath]);
            throw;
        }
    }

    public static bool TryDelete(IEnumerable<string> backupPaths)
    {
        var succeeded = true;
        foreach (var backupPath in backupPaths)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                succeeded = false;
            }
        }

        return succeeded;
    }

    private static async Task CopyAsync(
        string sourcePath,
        string backupPath,
        CancellationToken cancellationToken)
    {
        await using var source = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var backup = new FileStream(
            backupPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough);
        await source.CopyToAsync(backup, cancellationToken);
        await backup.FlushAsync(cancellationToken);
    }

    private static async Task VerifyAsync(
        string backupPath,
        string expectedHash,
        long expectedSize,
        CancellationToken cancellationToken)
    {
        var info = new FileInfo(backupPath);
        if (!info.Exists)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.PackageCorrupted,
                "Backup file gốc không được tạo đầy đủ.");
        }

        var hash = await Sha256File.ComputeAsync(backupPath, cancellationToken);
        if (info.Length != expectedSize
            || !string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InstallationOperationException(
                InstallationErrorCode.PackageCorrupted,
                "Backup file gốc không vượt qua xác minh.");
        }
    }
}
