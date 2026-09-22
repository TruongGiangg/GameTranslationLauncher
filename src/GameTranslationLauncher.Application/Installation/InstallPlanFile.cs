using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Domain.Packages;

namespace GameTranslationLauncher.Application.Installation;

public sealed record InstallPlanFile(
    GamePackageFile PackageFile,
    InstallFileAction Action,
    InstalledFileReceipt? PreviousFile,
    InstalledFileState? OriginalFileState = null,
    string? BackupRelativePath = null);
