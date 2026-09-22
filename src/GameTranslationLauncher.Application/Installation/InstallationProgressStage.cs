namespace GameTranslationLauncher.Application.Installation;

public enum InstallationProgressStage
{
    Validating,
    BackingUp,
    Staging,
    Installing,
    Verifying,
    RollingBack,
    Uninstalling,
    Completed
}
