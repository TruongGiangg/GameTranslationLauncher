namespace GameTranslationLauncher.Application.Installation;

public interface IUninstallPlanExecutor
{
    Task ExecuteAsync(
        UninstallPlan plan,
        IProgress<InstallationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
