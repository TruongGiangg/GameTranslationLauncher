using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Infrastructure.Persistence;

namespace GameTranslationLauncher.Infrastructure.FileSystem;

/// <summary>
/// Chuẩn bị, commit và xác minh các file của một install/update transaction.
/// </summary>
internal sealed class InstallPlanFileTransaction
{
    private readonly LauncherStoragePaths storagePaths;
    private readonly OriginalFileBackupStore backupStore;

    public InstallPlanFileTransaction(LauncherStoragePaths storagePaths)
    {
        this.storagePaths = storagePaths;
        backupStore = new OriginalFileBackupStore(storagePaths);
    }

    public async Task<IReadOnlyDictionary<string, string>> PrepareAsync(
        InstallPlan plan,
        FileTransaction transaction,
        ICollection<string> createdBackupPaths,
        IProgress<InstallationProgress>? progress,
        CancellationToken cancellationToken)
    {
        await BackupOriginalFilesAsync(plan, createdBackupPaths, progress, cancellationToken);
        return await StageFilesAsync(plan, transaction, progress, cancellationToken);
    }

    public static void Commit(
        InstallPlan plan,
        FileTransaction transaction,
        IReadOnlyDictionary<string, string> stagedFiles,
        IProgress<InstallationProgress>? progress)
    {
        // NOTE(commit-order): payload mới được commit trước file bị loại khỏi package;
        // FileTransaction ghi journal theo đúng thứ tự này để rollback theo chiều ngược lại.
        var total = plan.Files.Count(file => file.Action != InstallFileAction.Preserve)
            + plan.RemovedFiles.Count(file => file.Action != RemovedFileAction.Preserve);
        var completed = 0;

        foreach (var file in plan.Files.Where(file => file.Action != InstallFileAction.Preserve))
        {
            var destination = SafeGamePathResolver.Resolve(
                plan.Installation.GameRoot,
                file.PackageFile.Destination);
            var staging = stagedFiles[file.PackageFile.Destination];
            if (file.Action == InstallFileAction.Create)
            {
                transaction.CommitCreate(staging, destination);
            }
            else
            {
                transaction.CommitReplace(staging, destination);
            }

            completed++;
            progress?.Report(new InstallationProgress(
                InstallationProgressStage.Installing,
                completed,
                total,
                $"Đã commit '{file.PackageFile.Destination}'."));
        }

        foreach (var removal in plan.RemovedFiles
                     .Where(file => file.Action != RemovedFileAction.Preserve))
        {
            var destination = SafeGamePathResolver.Resolve(
                plan.Installation.GameRoot,
                removal.PreviousFile.Destination);
            if (removal.Action == RemovedFileAction.DeleteOwned)
            {
                transaction.CommitRemove(destination);
            }
            else
            {
                transaction.CommitRestore(
                    stagedFiles[removal.PreviousFile.Destination],
                    destination);
            }

            completed++;
            progress?.Report(new InstallationProgress(
                InstallationProgressStage.Installing,
                completed,
                total,
                $"Đã xử lý file cũ '{removal.PreviousFile.Destination}'."));
        }
    }

    public static async Task VerifyAsync(
        InstallPlan plan,
        IProgress<InstallationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var total = plan.Files.Count + plan.RemovedFiles.Count(file => file.Action != RemovedFileAction.Preserve);
        var completed = 0;

        foreach (var file in plan.Files)
        {
            var destination = SafeGamePathResolver.Resolve(
                plan.Installation.GameRoot,
                file.PackageFile.Destination);
            await EnsureFileMatchesAsync(
                destination,
                file.PackageFile.Sha256,
                file.PackageFile.SizeBytes,
                cancellationToken);
            completed++;
            progress?.Report(new InstallationProgress(
                InstallationProgressStage.Verifying,
                completed,
                total,
                $"Đã xác minh '{file.PackageFile.Destination}'."));
        }

        foreach (var removal in plan.RemovedFiles
                     .Where(file => file.Action != RemovedFileAction.Preserve))
        {
            await VerifyRemovalAsync(plan, removal, cancellationToken);
            completed++;
        }
    }

    private async Task BackupOriginalFilesAsync(
        InstallPlan plan,
        ICollection<string> createdBackupPaths,
        IProgress<InstallationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var files = plan.Files
            .Where(file => file.Action == InstallFileAction.ReplaceExternal)
            .ToArray();
        for (var index = 0; index < files.Length; index++)
        {
            var backupPath = await backupStore.CreateAsync(plan, files[index], cancellationToken);
            createdBackupPaths.Add(backupPath);
            progress?.Report(new InstallationProgress(
                InstallationProgressStage.BackingUp,
                index + 1,
                files.Length,
                $"Đã backup '{files[index].PackageFile.Destination}'."));
        }
    }

    private async Task<IReadOnlyDictionary<string, string>> StageFilesAsync(
        InstallPlan plan,
        FileTransaction transaction,
        IProgress<InstallationProgress>? progress,
        CancellationToken cancellationToken)
    {
        // NOTE(stage-before-commit): staging toàn bộ source và backup trước khi thay đổi
        // destination để lỗi đọc/copy không tạo ra trạng thái cài đặt một phần.
        var itemsToStage = plan.Files.Count(file => file.Action != InstallFileAction.Preserve)
            + plan.RemovedFiles.Count(file => file.Action == RemovedFileAction.RestoreBackup);
        var completed = 0;
        var stagedFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in plan.Files.Where(file => file.Action != InstallFileAction.Preserve))
        {
            var source = SafePackagePathResolver.Resolve(
                plan.AvailablePackage.ManifestPath,
                file.PackageFile.Source);
            var destination = SafeGamePathResolver.Resolve(
                plan.Installation.GameRoot,
                file.PackageFile.Destination);
            stagedFiles[file.PackageFile.Destination] = await transaction.StageCopyAsync(
                source,
                destination,
                file.PackageFile.Sha256,
                file.PackageFile.SizeBytes,
                cancellationToken);
            completed++;
            progress?.Report(new InstallationProgress(
                InstallationProgressStage.Staging,
                completed,
                itemsToStage,
                $"Đã staging '{file.PackageFile.Destination}'."));
        }

        foreach (var removal in plan.RemovedFiles
                     .Where(file => file.Action == RemovedFileAction.RestoreBackup))
        {
            var previous = removal.PreviousFile;
            var backupPath = ResolveBackupPath(plan, previous);
            var destination = SafeGamePathResolver.Resolve(
                plan.Installation.GameRoot,
                previous.Destination);
            stagedFiles[previous.Destination] = await transaction.StageCopyAsync(
                backupPath,
                destination,
                previous.OriginalSha256!,
                previous.OriginalSizeBytes!.Value,
                cancellationToken);
            completed++;
            progress?.Report(new InstallationProgress(
                InstallationProgressStage.Staging,
                completed,
                itemsToStage,
                $"Đã staging backup '{previous.Destination}'."));
        }

        return stagedFiles;
    }

    private static async Task VerifyRemovalAsync(
        InstallPlan plan,
        InstallPlanRemoval removal,
        CancellationToken cancellationToken)
    {
        var previous = removal.PreviousFile;
        var destination = SafeGamePathResolver.Resolve(
            plan.Installation.GameRoot,
            previous.Destination);
        if (removal.Action == RemovedFileAction.DeleteOwned)
        {
            if (File.Exists(destination))
            {
                throw new IOException("File owned cũ vẫn tồn tại sau commit.");
            }

            return;
        }

        await EnsureFileMatchesAsync(
            destination,
            previous.OriginalSha256!,
            previous.OriginalSizeBytes!.Value,
            cancellationToken);
    }

    private string ResolveBackupPath(InstallPlan plan, InstalledFileReceipt file)
    {
        if (file.BackupRelativePath is null)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.Conflict,
                $"Receipt thiếu backup cho '{file.Destination}'.");
        }

        return storagePaths.GetBackupPath(
            plan.Installation.GameId,
            plan.Installation.InstallationId,
            file.BackupRelativePath);
    }

    private static async Task EnsureFileMatchesAsync(
        string path,
        string expectedHash,
        long expectedSize,
        CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length != expectedSize)
        {
            throw new IOException("File đích không khớp kích thước sau commit.");
        }

        var hash = await Sha256File.ComputeAsync(path, cancellationToken);
        if (!string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("File đích không khớp hash sau commit.");
        }
    }
}
