using GameTranslationLauncher.Domain.Installation;

namespace GameTranslationLauncher.Application.Installation;

public interface IInstallPlanExecutor
{
    Task<InstallationReceipt> ExecuteAsync(
        InstallPlan plan,
        IProgress<InstallationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
