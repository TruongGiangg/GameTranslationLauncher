using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Installation;

/// <summary>
/// Kế hoạch bất biến đã kiểm tra, chưa làm thay đổi package hoặc thư mục game.
/// </summary>
public sealed record InstallPlan(
    Guid OperationId,
    InstallationOperationKind Kind,
    AvailableGamePackage AvailablePackage,
    GameInstallationSetting Installation,
    InstallationReceipt? PreviousReceipt,
    IReadOnlyList<InstallPlanFile> Files,
    IReadOnlyList<InstallPlanRemoval> RemovedFiles)
{
    public bool IsNoOp =>
        PreviousReceipt is not null
        && string.Equals(
            PreviousReceipt.PackageVersion,
            AvailablePackage.Package.PackageVersion,
            StringComparison.Ordinal)
        && string.Equals(
            PreviousReceipt.PackageManifestSha256,
            AvailablePackage.ManifestSha256,
            StringComparison.OrdinalIgnoreCase)
        && Files.All(file => file.Action == InstallFileAction.Preserve)
        && RemovedFiles.Count == 0;
}
