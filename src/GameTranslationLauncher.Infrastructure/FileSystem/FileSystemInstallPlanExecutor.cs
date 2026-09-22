using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Infrastructure.Persistence;

namespace GameTranslationLauncher.Infrastructure.FileSystem;

/// <summary>
/// Điều phối install/update transaction và chỉ commit receipt sau khi verify.
/// </summary>
public sealed class FileSystemInstallPlanExecutor : IInstallPlanExecutor
{
    private readonly IInstallPlanPreflight preflight;
    private readonly IInstallationReceiptRepository receiptRepository;
    private readonly InstallPlanFileTransaction fileTransaction;
    private readonly IInstallationOperationLogger logger;
    private readonly string launcherVersion;

    public FileSystemInstallPlanExecutor(
        IInstallPlanPreflight preflight,
        IInstallationReceiptRepository receiptRepository,
        LauncherStoragePaths storagePaths,
        IInstallationOperationLogger logger,
        string launcherVersion = "0.1.0")
    {
        if (!Version.TryParse(launcherVersion, out _))
        {
            throw new ArgumentException("Launcher version phải là semantic version.", nameof(launcherVersion));
        }

        this.preflight = preflight;
        this.receiptRepository = receiptRepository;
        fileTransaction = new InstallPlanFileTransaction(storagePaths);
        this.logger = logger;
        this.launcherVersion = launcherVersion;
    }

    public async Task<InstallationReceipt> ExecuteAsync(
        InstallPlan plan,
        IProgress<InstallationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        // NOTE(install-transaction-boundary): giữ preflight, commit receipt và rollback trong
        // cùng scope để mọi lỗi trước receipt đều hoàn tác đúng FileTransaction hiện tại.
        var transaction = new FileTransaction(plan.Installation.GameRoot, plan.OperationId);
        var createdBackupPaths = new List<string>();
        var receiptCommitted = false;

        try
        {
            await ReportAsync(
                plan,
                progress,
                InstallationProgressStage.Validating,
                0,
                1,
                "Đang chạy preflight cuối.");
            await preflight.ValidateAsync(plan, cancellationToken);

            var stagedFiles = await fileTransaction.PrepareAsync(
                plan,
                transaction,
                createdBackupPaths,
                progress,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            InstallPlanFileTransaction.Commit(plan, transaction, stagedFiles, progress);
            await InstallPlanFileTransaction.VerifyAsync(plan, progress, CancellationToken.None);

            var receipt = InstallationReceiptFactory.Create(
                plan,
                transaction.CreatedDirectories,
                launcherVersion);
            await receiptRepository.SaveAsync(receipt, CancellationToken.None);
            receiptCommitted = true;

            var cleanupSucceeded = transaction.TryComplete();
            await ReportAsync(
                plan,
                progress,
                InstallationProgressStage.Completed,
                1,
                1,
                cleanupSucceeded
                    ? "Đã commit file và receipt."
                    : "Đã commit; còn file tạm sẽ được xử lý ở lần bảo trì sau.");
            return receipt;
        }
        catch (Exception exception)
        {
            if (!receiptCommitted)
            {
                await RollbackAsync(
                    plan,
                    transaction,
                    createdBackupPaths,
                    progress,
                    exception);
            }

            throw MapException(exception);
        }
    }

    private async Task RollbackAsync(
        InstallPlan plan,
        FileTransaction transaction,
        IReadOnlyCollection<string> createdBackupPaths,
        IProgress<InstallationProgress>? progress,
        Exception originalException)
    {
        progress?.Report(new InstallationProgress(
            InstallationProgressStage.RollingBack,
            0,
            1,
            "Đang hoàn tác thay đổi file."));
        await logger.TryWriteAsync(
            new InstallationLogEntry(
                DateTimeOffset.UtcNow,
                plan.OperationId,
                plan.Installation.GameId,
                plan.Installation.InstallationId,
                InstallationProgressStage.RollingBack,
                $"Rollback sau lỗi {originalException.GetType().Name}."),
            CancellationToken.None);

        try
        {
            transaction.Rollback();
            if (!OriginalFileBackupStore.TryDelete(createdBackupPaths))
            {
                throw new IOException("Không thể dọn backup được tạo bởi transaction thất bại.");
            }
        }
        catch (Exception rollbackException)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.RollbackFailed,
                "Operation thất bại và không thể rollback đầy đủ.",
                new AggregateException(originalException, rollbackException));
        }
    }

    private Task<bool> ReportAsync(
        InstallPlan plan,
        IProgress<InstallationProgress>? progress,
        InstallationProgressStage stage,
        int completed,
        int total,
        string message)
    {
        progress?.Report(new InstallationProgress(stage, completed, total, message));
        return logger.TryWriteAsync(
            new InstallationLogEntry(
                DateTimeOffset.UtcNow,
                plan.OperationId,
                plan.Installation.GameId,
                plan.Installation.InstallationId,
                stage,
                message),
            CancellationToken.None);
    }

    private static Exception MapException(Exception exception) => exception switch
    {
        InstallationOperationException => exception,
        OperationCanceledException => exception,
        UnauthorizedAccessException => new InstallationOperationException(
            InstallationErrorCode.AccessDenied,
            "Launcher không có quyền hoàn tất thay đổi file.",
            exception),
        IOException => new InstallationOperationException(
            InstallationErrorCode.FileInUse,
            "Không thể hoàn tất thay đổi vì file đang bận hoặc trạng thái file đã đổi.",
            exception),
        _ => exception
    };
}
