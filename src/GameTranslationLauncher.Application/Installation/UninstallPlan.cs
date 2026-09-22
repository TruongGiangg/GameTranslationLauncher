using GameTranslationLauncher.Domain.Installation;

namespace GameTranslationLauncher.Application.Installation;

public sealed record UninstallPlan(
    Guid OperationId,
    InstallationReceipt Receipt,
    IReadOnlyList<InstallPlanRemoval> Files);
