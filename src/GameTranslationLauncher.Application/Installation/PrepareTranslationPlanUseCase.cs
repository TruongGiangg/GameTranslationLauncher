namespace GameTranslationLauncher.Application.Installation;

/// <summary>
/// Lập kế hoạch preview chỉ đọc để UI có thể yêu cầu xác nhận chính xác trước khi thay file ngoài Launcher.
/// </summary>
public sealed class PrepareTranslationPlanUseCase
{
    private readonly BuildInstallPlanUseCase buildInstallPlanUseCase;
    private readonly IInstalledFileStateReader fileStateReader;

    public PrepareTranslationPlanUseCase(
        BuildInstallPlanUseCase buildInstallPlanUseCase,
        IInstalledFileStateReader fileStateReader)
    {
        this.buildInstallPlanUseCase = buildInstallPlanUseCase;
        this.fileStateReader = fileStateReader;
    }

    public Task<InstallPlan> PrepareInstallAsync(
        ApplyTranslationRequest request,
        CancellationToken cancellationToken = default) =>
        PrepareAsync(InstallationOperationKind.Install, request, cancellationToken);

    public Task<InstallPlan> PrepareUpdateAsync(
        ApplyTranslationRequest request,
        CancellationToken cancellationToken = default) =>
        PrepareAsync(InstallationOperationKind.Update, request, cancellationToken);

    private async Task<InstallPlan> PrepareAsync(
        InstallationOperationKind operationKind,
        ApplyTranslationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var approvedReplacements = request.ApprovedReplacementDestinations is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(request.ApprovedReplacementDestinations, StringComparer.OrdinalIgnoreCase);

        // NOTE(confirmation-preview): chỉ đọc state hiện tại để UI biết file nào cần được
        // người dùng xác nhận. Executor vẫn lập plan lại ngay trước transaction để tránh TOCTOU.
        foreach (var packageFile in GamePackageFileEnumerator.Enumerate(request.AvailablePackage.Package))
        {
            var state = await fileStateReader.ReadAsync(
                request.Installation.GameRoot,
                packageFile.Destination,
                cancellationToken);
            if (state.Exists)
            {
                approvedReplacements.Add(packageFile.Destination);
            }
        }

        var previewRequest = request with { ApprovedReplacementDestinations = approvedReplacements };
        return operationKind == InstallationOperationKind.Install
            ? await buildInstallPlanUseCase.BuildInstallAsync(Guid.NewGuid(), previewRequest, cancellationToken)
            : await buildInstallPlanUseCase.BuildUpdateAsync(Guid.NewGuid(), previewRequest, cancellationToken);
    }
}
