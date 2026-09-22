namespace GameTranslationLauncher.Application.Installation;

public interface IUninstallPlanPreflight
{
    Task ValidateAsync(
        UninstallPlan plan,
        CancellationToken cancellationToken = default);
}
