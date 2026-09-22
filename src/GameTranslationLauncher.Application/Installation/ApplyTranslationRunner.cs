namespace GameTranslationLauncher.Application.Installation;

internal sealed class ApplyTranslationRunner
{
    private readonly BuildInstallPlanUseCase buildPlanUseCase;
    private readonly IInstallPlanExecutor executor;
    private readonly InstallationOperationLock operationLock;
    private readonly IInstallationOperationLogger logger;

    public ApplyTranslationRunner(
        BuildInstallPlanUseCase buildPlanUseCase,
        IInstallPlanExecutor executor,
        InstallationOperationLock operationLock,
        IInstallationOperationLogger logger)
    {
        this.buildPlanUseCase = buildPlanUseCase;
        this.executor = executor;
        this.operationLock = operationLock;
        this.logger = logger;
    }

    public async Task<InstallationOperationResult> ExecuteAsync(
        InstallationOperationKind kind,
        ApplyTranslationRequest request,
        IProgress<InstallationProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var operationId = Guid.NewGuid();
        using var lease = operationLock.TryAcquire(
            request.Installation.GameId,
            request.Installation.InstallationId);
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
                "Đang kiểm tra package và thư mục game."));
            await WriteLogAsync(
                operationId,
                request,
                InstallationProgressStage.Validating,
                $"Bắt đầu {kind}.");

            var plan = kind == InstallationOperationKind.Install
                ? await buildPlanUseCase.BuildInstallAsync(operationId, request, cancellationToken)
                : await buildPlanUseCase.BuildUpdateAsync(operationId, request, cancellationToken);

            if (plan.IsNoOp)
            {
                progress?.Report(new InstallationProgress(
                    InstallationProgressStage.Completed,
                    1,
                    1,
                    "Package hiện tại đã được cài đầy đủ."));
                await WriteLogAsync(
                    operationId,
                    request,
                    InstallationProgressStage.Completed,
                    "Operation hoàn tất dưới dạng no-op.");
                return InstallationOperationResult.Success(
                    operationId,
                    "Package hiện tại đã được cài đầy đủ.",
                    plan.PreviousReceipt);
            }

            var receipt = await executor.ExecuteAsync(plan, progress, cancellationToken);
            await WriteLogAsync(
                operationId,
                request,
                InstallationProgressStage.Completed,
                $"Hoàn tất {kind}.");
            return InstallationOperationResult.Success(
                operationId,
                kind == InstallationOperationKind.Install
                    ? "Đã cài bản Việt hóa."
                    : "Đã cập nhật bản Việt hóa.",
                receipt);
        }
        catch (OperationCanceledException)
        {
            await WriteLogAsync(
                operationId,
                request,
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
                request,
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
                request,
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
        ApplyTranslationRequest request,
        InstallationProgressStage stage,
        string message,
        InstallationErrorCode? errorCode = null) =>
        logger.TryWriteAsync(
            new InstallationLogEntry(
                DateTimeOffset.UtcNow,
                operationId,
                request.Installation.GameId,
                request.Installation.InstallationId,
                stage,
                message,
                errorCode),
            CancellationToken.None);
}
