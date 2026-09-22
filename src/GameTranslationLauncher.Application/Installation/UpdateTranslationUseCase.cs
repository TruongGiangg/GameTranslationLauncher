namespace GameTranslationLauncher.Application.Installation;

public sealed class UpdateTranslationUseCase
{
    private readonly ApplyTranslationRunner runner;

    public UpdateTranslationUseCase(
        BuildInstallPlanUseCase buildPlanUseCase,
        IInstallPlanExecutor executor,
        InstallationOperationLock operationLock,
        IInstallationOperationLogger logger)
    {
        runner = new ApplyTranslationRunner(buildPlanUseCase, executor, operationLock, logger);
    }

    public Task<InstallationOperationResult> ExecuteAsync(
        ApplyTranslationRequest request,
        IProgress<InstallationProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        runner.ExecuteAsync(
            InstallationOperationKind.Update,
            request,
            progress,
            cancellationToken);
}
