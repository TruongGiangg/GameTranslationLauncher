namespace GameTranslationLauncher.Application.Installation;

public sealed class UninstallTranslationUseCase
{
    private readonly IInstallationReceiptRepository receiptRepository;
    private readonly BuildUninstallPlanUseCase buildPlanUseCase;
    private readonly IUninstallPlanExecutor executor;
    private readonly InstallationOperationLock operationLock;
    private readonly IInstallationOperationLogger logger;

    public UninstallTranslationUseCase(
        IInstallationReceiptRepository receiptRepository,
        BuildUninstallPlanUseCase buildPlanUseCase,
        IUninstallPlanExecutor executor,
        InstallationOperationLock operationLock,
        IInstallationOperationLogger logger)
    {
        this.receiptRepository = receiptRepository;
        this.buildPlanUseCase = buildPlanUseCase;
        this.executor = executor;
        this.operationLock = operationLock;
        this.logger = logger;
    }

    public async Task<InstallationOperationResult> ExecuteAsync(
        string gameId,
        Guid installationId,
        IProgress<InstallationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid();
        using var lease = operationLock.TryAcquire(gameId, installationId);
        if (lease is null)
        {
            return InstallationOperationResult.Failure(
                operationId,
                InstallationErrorCode.OperationInProgress,
                "Installation đang có một thao tác khác chạy.");
        }

        try
        {
            progress?.Report(new InstallationProgress(
                InstallationProgressStage.Validating,
                0,
                1,
                "Đang kiểm tra receipt và file đã cài."));
            await WriteLogAsync(
                operationId,
                gameId,
                installationId,
                InstallationProgressStage.Validating,
                "Bắt đầu Uninstall.");

            var receipt = await receiptRepository.FindAsync(gameId, installationId, cancellationToken);
            if (receipt is null)
            {
                return InstallationOperationResult.Success(
                    operationId,
                    "Không có bản cài do Launcher quản lý.");
            }

            var plan = await buildPlanUseCase.ExecuteAsync(operationId, receipt, cancellationToken);
            await executor.ExecuteAsync(plan, progress, cancellationToken);
            await WriteLogAsync(
                operationId,
                gameId,
                installationId,
                InstallationProgressStage.Completed,
                "Hoàn tất Uninstall.");
            return InstallationOperationResult.Success(operationId, "Đã gỡ bản Việt hóa.");
        }
        catch (OperationCanceledException)
        {
            await WriteLogAsync(
                operationId,
                gameId,
                installationId,
                InstallationProgressStage.RollingBack,
                "Operation đã hủy tại điểm an toàn.",
                InstallationErrorCode.Cancelled);
            return InstallationOperationResult.Failure(
                operationId,
                InstallationErrorCode.Cancelled,
                "Thao tác đã được hủy an toàn.");
        }
        catch (InstallationOperationException exception)
        {
            await WriteLogAsync(
                operationId,
                gameId,
                installationId,
                InstallationProgressStage.RollingBack,
                exception.Message,
                exception.ErrorCode);
            return InstallationOperationResult.Failure(
                operationId,
                exception.ErrorCode,
                exception.Message);
        }
        catch (Exception exception)
        {
            await WriteLogAsync(
                operationId,
                gameId,
                installationId,
                InstallationProgressStage.RollingBack,
                $"Lỗi không dự kiến: {exception.GetType().Name}.",
                InstallationErrorCode.Unexpected);
            return InstallationOperationResult.Failure(
                operationId,
                InstallationErrorCode.Unexpected,
                $"Lỗi không dự kiến. Dùng operation ID {operationId:N} để tra log.");
        }
    }

    private Task<bool> WriteLogAsync(
        Guid operationId,
        string gameId,
        Guid installationId,
        InstallationProgressStage stage,
        string message,
        InstallationErrorCode? errorCode = null) =>
        logger.TryWriteAsync(
            new InstallationLogEntry(
                DateTimeOffset.UtcNow,
                operationId,
                gameId,
                installationId,
                stage,
                message,
                errorCode),
            CancellationToken.None);
}
