using GameTranslationLauncher.Domain.Installation;

namespace GameTranslationLauncher.Application.Installation;

public sealed record InstallPlanRemoval(
    InstalledFileReceipt PreviousFile,
    RemovedFileAction Action);
