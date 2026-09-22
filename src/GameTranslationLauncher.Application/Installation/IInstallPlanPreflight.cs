namespace GameTranslationLauncher.Application.Installation;

public interface IInstallPlanPreflight
{
    Task ValidateAsync(
        InstallPlan plan,
        CancellationToken cancellationToken = default);
}
