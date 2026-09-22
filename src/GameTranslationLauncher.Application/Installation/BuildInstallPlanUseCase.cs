using GameTranslationLauncher.Domain.Installation;

namespace GameTranslationLauncher.Application.Installation;

/// <summary>
/// Lập kế hoạch cài/update chỉ bằng thao tác đọc và không thay đổi thư mục game.
/// </summary>
public sealed class BuildInstallPlanUseCase
{
    private readonly IGameInstallationDetector installationDetector;
    private readonly IInstallationReceiptRepository receiptRepository;
    private readonly IInstalledFileStateReader fileStateReader;
    private readonly IPackageIntegrityVerifier packageIntegrityVerifier;
    private readonly InstallPlanFileClassifier fileClassifier;

    public BuildInstallPlanUseCase(
        IGameInstallationDetector installationDetector,
        IInstallationReceiptRepository receiptRepository,
        IInstalledFileStateReader fileStateReader,
        IPackageIntegrityVerifier packageIntegrityVerifier)
    {
        this.installationDetector = installationDetector;
        this.receiptRepository = receiptRepository;
        this.fileStateReader = fileStateReader;
        this.packageIntegrityVerifier = packageIntegrityVerifier;
        fileClassifier = new InstallPlanFileClassifier(fileStateReader);
    }

    public Task<InstallPlan> BuildInstallAsync(
        Guid operationId,
        ApplyTranslationRequest request,
        CancellationToken cancellationToken = default) =>
        BuildAsync(operationId, InstallationOperationKind.Install, request, cancellationToken);

    public Task<InstallPlan> BuildUpdateAsync(
        Guid operationId,
        ApplyTranslationRequest request,
        CancellationToken cancellationToken = default) =>
        BuildAsync(operationId, InstallationOperationKind.Update, request, cancellationToken);

    private async Task<InstallPlan> BuildAsync(
        Guid operationId,
        InstallationOperationKind kind,
        ApplyTranslationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateIdentity(operationId, request);
        ValidateExplicitConsent(request);

        var package = request.AvailablePackage.Package;
        var installation = request.Installation;
        var isMatchingGame = await installationDetector.IsMatchAsync(
            installation.GameRoot,
            package.InstallDetection,
            cancellationToken);
        if (!isMatchingGame)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.Validation,
                "Thư mục đã chọn không khớp game trong package.");
        }

        await packageIntegrityVerifier.VerifyAsync(request.AvailablePackage, cancellationToken);
        var receipt = await receiptRepository.FindAsync(
            package.GameId,
            installation.InstallationId,
            cancellationToken);

        if (receipt is null)
        {
            return await BuildWithoutReceiptAsync(
                operationId,
                kind,
                request,
                cancellationToken);
        }

        return await BuildWithReceiptAsync(
            operationId,
            kind,
            request,
            receipt,
            cancellationToken);
    }

    private async Task<InstallPlan> BuildWithoutReceiptAsync(
        Guid operationId,
        InstallationOperationKind kind,
        ApplyTranslationRequest request,
        CancellationToken cancellationToken)
    {
        if (kind == InstallationOperationKind.Update)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.Validation,
                "Không có receipt để cập nhật bản Việt hóa.");
        }

        var files = await fileClassifier.BuildWithoutReceiptAsync(request, cancellationToken);
        return new InstallPlan(
            operationId,
            kind,
            request.AvailablePackage,
            request.Installation,
            null,
            files,
            []);
    }

    private async Task<InstallPlan> BuildWithReceiptAsync(
        Guid operationId,
        InstallationOperationKind kind,
        ApplyTranslationRequest request,
        InstallationReceipt receipt,
        CancellationToken cancellationToken)
    {
        ValidateReceiptIdentity(receipt, request);
        await EnsureReceiptFilesIntactAsync(receipt, cancellationToken);
        ValidateVersionTransition(kind, receipt, request);

        var files = await fileClassifier.BuildWithReceiptAsync(receipt, request, cancellationToken);
        var removals = InstallPlanFileClassifier.BuildRemovals(receipt, files);
        EnsureInstallIsIdempotentOnly(kind, receipt, request, files, removals);

        return new InstallPlan(
            operationId,
            kind,
            request.AvailablePackage,
            request.Installation,
            receipt,
            files,
            removals);
    }

    private async Task EnsureReceiptFilesIntactAsync(
        InstallationReceipt receipt,
        CancellationToken cancellationToken)
    {
        foreach (var file in receipt.Files)
        {
            var state = await fileStateReader.ReadAsync(
                receipt.GameRoot,
                file.Destination,
                cancellationToken);
            if (!state.Exists
                || state.SizeBytes != file.InstalledSizeBytes
                || !string.Equals(state.Sha256, file.InstalledSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw Conflict(file.Destination);
            }
        }
    }

    private static void ValidateIdentity(Guid operationId, ApplyTranslationRequest request)
    {
        if (operationId == Guid.Empty)
        {
            throw new ArgumentException("Operation ID không được rỗng.", nameof(operationId));
        }

        if (!string.Equals(
            request.AvailablePackage.Package.GameId,
            request.Installation.GameId,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InstallationOperationException(
                InstallationErrorCode.Validation,
                "Game ID của package và installation không khớp.");
        }
    }

    private static void ValidateExplicitConsent(ApplyTranslationRequest request)
    {
        var missingConsent = request.AvailablePackage.Package.Prerequisites
            .FirstOrDefault(prerequisite =>
                prerequisite.RequiresExplicitConsent
                && !request.AcceptedPrerequisiteIds.Contains(prerequisite.Id));
        if (missingConsent is not null)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.ExplicitConsentRequired,
                $"Cần xác nhận prerequisite '{missingConsent.DisplayName}' trước khi cài.");
        }
    }

    private static void ValidateReceiptIdentity(
        InstallationReceipt receipt,
        ApplyTranslationRequest request)
    {
        if (receipt.InstallationId != request.Installation.InstallationId
            || !string.Equals(receipt.GameId, request.Installation.GameId, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(receipt.GameRoot, request.Installation.GameRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InstallationOperationException(
                InstallationErrorCode.Validation,
                "Receipt không khớp installation đã chọn.");
        }
    }

    private static void ValidateVersionTransition(
        InstallationOperationKind kind,
        InstallationReceipt receipt,
        ApplyTranslationRequest request)
    {
        var installedVersion = Version.Parse(receipt.PackageVersion);
        var availableVersion = Version.Parse(request.AvailablePackage.Package.PackageVersion);
        var sameManifest = string.Equals(
            receipt.PackageManifestSha256,
            request.AvailablePackage.ManifestSha256,
            StringComparison.OrdinalIgnoreCase);

        if (availableVersion == installedVersion && !sameManifest)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.Conflict,
                "Package cùng version nhưng manifest đã thay đổi.");
        }

        if (kind == InstallationOperationKind.Install && availableVersion != installedVersion)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.Validation,
                "Installation đã có receipt; hãy dùng thao tác cập nhật.");
        }

        if (kind == InstallationOperationKind.Update && availableVersion < installedVersion)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.Validation,
                "Không thể cập nhật xuống package có version thấp hơn.");
        }
    }

    private static void EnsureInstallIsIdempotentOnly(
        InstallationOperationKind kind,
        InstallationReceipt receipt,
        ApplyTranslationRequest request,
        IReadOnlyList<InstallPlanFile> files,
        IReadOnlyList<InstallPlanRemoval> removals)
    {
        if (kind != InstallationOperationKind.Install)
        {
            return;
        }

        var exactReceipt = string.Equals(
            receipt.PackageManifestSha256,
            request.AvailablePackage.ManifestSha256,
            StringComparison.OrdinalIgnoreCase);
        if (!exactReceipt
            || files.Any(file => file.Action != InstallFileAction.Preserve)
            || removals.Count > 0)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.Conflict,
                "Receipt hiện tại không khớp đầy đủ package cần cài lại.");
        }
    }

    private static InstallationOperationException Conflict(string destination) =>
        new(
            InstallationErrorCode.Conflict,
            $"Không thể xác định ownership an toàn cho '{destination}'.");
}
