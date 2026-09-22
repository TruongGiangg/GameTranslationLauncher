using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Infrastructure.Persistence;

namespace GameTranslationLauncher.Infrastructure.FileSystem;

/// <summary>
/// Gỡ đúng file trong receipt, khôi phục backup và rollback nếu chưa xóa receipt.
/// </summary>
public sealed class FileSystemUninstallPlanExecutor : IUninstallPlanExecutor
{
    private readonly IUninstallPlanPreflight preflight;
    private readonly IInstallationReceiptRepository receiptRepository;
    private readonly LauncherStoragePaths storagePaths;
    private readonly IInstallationOperationLogger logger;

    public FileSystemUninstallPlanExecutor(
        IUninstallPlanPreflight preflight,
        IInstallationReceiptRepository receiptRepository,
        LauncherStoragePaths storagePaths,
        IInstallationOperationLogger logger)
    {
        this.preflight = preflight;
        this.receiptRepository = receiptRepository;
        this.storagePaths = storagePaths;
        this.logger = logger;
    }

    public async Task ExecuteAsync(
        UninstallPlan plan,
        IProgress<InstallationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        // NOTE(uninstall-transaction-boundary): receipt chỉ bị xóa sau verify; mọi lỗi trước
        // thời điểm đó dùng cùng journal để khôi phục toàn bộ file đã xử lý.
        var transaction = new FileTransaction(plan.Receipt.GameRoot, plan.OperationId);
        var receiptDeleted = false;

        try
        {
            await preflight.ValidateAsync(plan, cancellationToken);
            var stagedBackups = await StageBackupsAsync(
                plan,
                transaction,
                progress,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            Commit(plan, transaction, stagedBackups, progress);
            await VerifyAsync(plan, progress, CancellationToken.None);
            await receiptRepository.DeleteAsync(
                plan.Receipt.GameId,
                plan.Receipt.InstallationId,
                CancellationToken.None);
            receiptDeleted = true;

            var cleanupSucceeded = transaction.TryComplete();
            cleanupSucceeded &= CleanupOwnedDirectories(plan);
            cleanupSucceeded &= CleanupBackups(plan);
            await LogAsync(
                plan,
                InstallationProgressStage.Completed,
                cleanupSucceeded
                    ? "Đã gỡ file và xóa receipt."
                    : "Đã gỡ và xóa receipt; còn dữ liệu rỗng/tạm cần bảo trì.");
            progress?.Report(new InstallationProgress(
                InstallationProgressStage.Completed,
                1,
                1,
                "Đã gỡ bản Việt hóa."));
        }
        catch (Exception exception)
        {
            if (!receiptDeleted)
            {
                await RollbackAsync(plan, transaction, progress, exception);
            }

            throw MapException(exception);
        }
    }

    private async Task<Dictionary<string, string>> StageBackupsAsync(
        UninstallPlan plan,
        FileTransaction transaction,
        IProgress<InstallationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var items = plan.Files
            .Where(file => file.Action == RemovedFileAction.RestoreBackup)
            .ToArray();
        var staged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < items.Length; index++)
        {
            var file = items[index].PreviousFile;
            var backup = storagePaths.GetBackupPath(
                plan.Receipt.GameId,
                plan.Receipt.InstallationId,
                file.BackupRelativePath!);
            var destination = SafeGamePathResolver.Resolve(
                plan.Receipt.GameRoot,
                file.Destination);
            staged[file.Destination] = await transaction.StageCopyAsync(
                backup,
                destination,
                file.OriginalSha256!,
                file.OriginalSizeBytes!.Value,
                cancellationToken);
            progress?.Report(new InstallationProgress(
                InstallationProgressStage.Staging,
                index + 1,
                items.Length,
                $"Đã staging backup '{file.Destination}'."));
        }

        return staged;
    }

    private static void Commit(
        UninstallPlan plan,
        FileTransaction transaction,
        IReadOnlyDictionary<string, string> stagedBackups,
        IProgress<InstallationProgress>? progress)
    {
        var mutableFiles = plan.Files
            .Where(file => file.Action != RemovedFileAction.Preserve)
            .ToArray();
        for (var index = 0; index < mutableFiles.Length; index++)
        {
            var item = mutableFiles[index];
            var destination = SafeGamePathResolver.Resolve(
                plan.Receipt.GameRoot,
                item.PreviousFile.Destination);
            if (item.Action == RemovedFileAction.DeleteOwned)
            {
                transaction.CommitRemove(destination);
            }
            else
            {
                transaction.CommitRestore(
                    stagedBackups[item.PreviousFile.Destination],
                    destination);
            }

            progress?.Report(new InstallationProgress(
                InstallationProgressStage.Uninstalling,
                index + 1,
                mutableFiles.Length,
                $"Đã xử lý '{item.PreviousFile.Destination}'."));
        }
    }

    private static async Task VerifyAsync(
        UninstallPlan plan,
        IProgress<InstallationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var mutableFiles = plan.Files
            .Where(file => file.Action != RemovedFileAction.Preserve)
            .ToArray();
        for (var index = 0; index < mutableFiles.Length; index++)
        {
            var item = mutableFiles[index];
            var file = item.PreviousFile;
            var destination = SafeGamePathResolver.Resolve(plan.Receipt.GameRoot, file.Destination);
            if (item.Action == RemovedFileAction.DeleteOwned)
            {
                if (File.Exists(destination))
                {
                    throw new IOException("File owned vẫn tồn tại sau uninstall.");
                }
            }
            else
            {
                var info = new FileInfo(destination);
                var hash = info.Exists
                    ? await Sha256File.ComputeAsync(destination, cancellationToken)
                    : null;
                if (!info.Exists
                    || info.Length != file.OriginalSizeBytes
                    || !string.Equals(hash, file.OriginalSha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException("File gốc không được khôi phục chính xác.");
                }
            }

            progress?.Report(new InstallationProgress(
                InstallationProgressStage.Verifying,
                index + 1,
                mutableFiles.Length,
                $"Đã xác minh '{file.Destination}'."));
        }
    }

    private static bool CleanupOwnedDirectories(UninstallPlan plan)
    {
        var succeeded = true;
        foreach (var relativePath in plan.Receipt.CreatedDirectories
                     .OrderByDescending(path => path.Count(character => character == '/')))
        {
            try
            {
                var directory = SafeGamePathResolver.Resolve(plan.Receipt.GameRoot, relativePath);
                if (Directory.Exists(directory)
                    && !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                succeeded = false;
            }
        }

        return succeeded;
    }

    private bool CleanupBackups(UninstallPlan plan)
    {
        var backupPaths = plan.Files
            .Where(file => file.Action == RemovedFileAction.RestoreBackup)
            .Select(file => storagePaths.GetBackupPath(
                plan.Receipt.GameId,
                plan.Receipt.InstallationId,
                file.PreviousFile.BackupRelativePath!));
        return OriginalFileBackupStore.TryDelete(backupPaths);
    }

    private async Task RollbackAsync(
        UninstallPlan plan,
        FileTransaction transaction,
        IProgress<InstallationProgress>? progress,
        Exception originalException)
    {
        progress?.Report(new InstallationProgress(
            InstallationProgressStage.RollingBack,
            0,
            1,
            "Đang hoàn tác thao tác gỡ."));
        await LogAsync(
            plan,
            InstallationProgressStage.RollingBack,
            $"Rollback uninstall sau lỗi {originalException.GetType().Name}.");

        try
        {
            transaction.Rollback();
        }
        catch (Exception rollbackException)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.RollbackFailed,
                "Uninstall thất bại và không thể rollback đầy đủ.",
                new AggregateException(originalException, rollbackException));
        }
    }

    private Task<bool> LogAsync(
        UninstallPlan plan,
        InstallationProgressStage stage,
        string message) =>
        logger.TryWriteAsync(
            new InstallationLogEntry(
                DateTimeOffset.UtcNow,
                plan.OperationId,
                plan.Receipt.GameId,
                plan.Receipt.InstallationId,
                stage,
                message),
            CancellationToken.None);

    private static Exception MapException(Exception exception) => exception switch
    {
        InstallationOperationException => exception,
        OperationCanceledException => exception,
        UnauthorizedAccessException => new InstallationOperationException(
            InstallationErrorCode.AccessDenied,
            "Launcher không có quyền hoàn tất uninstall.",
            exception),
        IOException => new InstallationOperationException(
            InstallationErrorCode.FileInUse,
            "Không thể hoàn tất uninstall vì trạng thái file đã thay đổi.",
            exception),
        _ => exception
    };
}
