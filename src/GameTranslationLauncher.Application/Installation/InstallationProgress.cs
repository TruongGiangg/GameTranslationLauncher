namespace GameTranslationLauncher.Application.Installation;

public sealed record InstallationProgress(
    InstallationProgressStage Stage,
    int CompletedItems,
    int TotalItems,
    string Message);
