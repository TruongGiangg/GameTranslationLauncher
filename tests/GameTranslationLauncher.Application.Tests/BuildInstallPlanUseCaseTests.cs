using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Tests;

[TestClass]
public sealed class BuildInstallPlanUseCaseTests
{
    private const string FileHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [TestMethod]
    public async Task BuildInstallAsync_PrerequisiteConsentMissing_ThrowsStructuredError()
    {
        var useCase = CreateUseCase();
        var request = CreateRequest(CreatePackage(withPrerequisite: true), acceptedPrerequisites: []);

        var exception = await Assert.ThrowsAsync<InstallationOperationException>(
            () => useCase.BuildInstallAsync(Guid.NewGuid(), request));

        Assert.AreEqual(InstallationErrorCode.ExplicitConsentRequired, exception.ErrorCode);
    }

    [TestMethod]
    public async Task BuildInstallAsync_ExistingPayloadWithoutApproval_ReturnsConflict()
    {
        var fileReader = new FakeFileStateReader();
        fileReader.States["Game/Mods/vi.pak"] = new InstalledFileState(true, 10, FileHash);
        var useCase = CreateUseCase(fileReader);
        var request = CreateRequest(CreatePackage());

        var exception = await Assert.ThrowsAsync<InstallationOperationException>(
            () => useCase.BuildInstallAsync(Guid.NewGuid(), request));

        Assert.AreEqual(InstallationErrorCode.Conflict, exception.ErrorCode);
    }

    [TestMethod]
    public async Task BuildInstallAsync_ExistingPayloadWithApproval_PlansBackupAndReplace()
    {
        var fileReader = new FakeFileStateReader();
        fileReader.States["Game/Mods/vi.pak"] = new InstalledFileState(true, 10, FileHash);
        var useCase = CreateUseCase(fileReader);
        var request = CreateRequest(
            CreatePackage(),
            approvedReplacements: ["Game/Mods/vi.pak"]);

        var plan = await useCase.BuildInstallAsync(Guid.NewGuid(), request);

        var file = plan.Files.Single();
        Assert.AreEqual(InstallFileAction.ReplaceExternal, file.Action);
        Assert.AreEqual(FileHash, file.OriginalFileState?.Sha256);
        Assert.AreEqual("Game/Mods/vi.pak.original", file.BackupRelativePath);
    }

    private static BuildInstallPlanUseCase CreateUseCase(FakeFileStateReader? fileReader = null)
    {
        return new BuildInstallPlanUseCase(
            new MatchingDetector(),
            new EmptyReceiptRepository(),
            fileReader ?? new FakeFileStateReader(),
            new ValidIntegrityVerifier());
    }

    private static ApplyTranslationRequest CreateRequest(
        AvailableGamePackage package,
        IEnumerable<string>? acceptedPrerequisites = null,
        IEnumerable<string>? approvedReplacements = null)
    {
        return new ApplyTranslationRequest(
            package,
            new GameInstallationSetting(
                "sample-game",
                Guid.NewGuid(),
                "C:\\Games\\Sample",
                DateTimeOffset.UtcNow),
            new HashSet<string>(acceptedPrerequisites ?? [], StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(approvedReplacements ?? [], StringComparer.OrdinalIgnoreCase));
    }

    private static AvailableGamePackage CreatePackage(bool withPrerequisite = false)
    {
        var prerequisites = withPrerequisite
            ? new[]
            {
                new GamePrerequisite(
                    "signature-helper",
                    "Signature Helper",
                    "Test",
                    true,
                    [
                        new GamePackageFile(
                            "prerequisites/helper.dll",
                            "Game/helper.dll",
                            FileHash,
                            10,
                            PackageFileRole.Prerequisite,
                            "signature-helper")
                    ])
            }
            : [];
        var package = new GamePackage(
            1,
            "sample-game",
            "Sample Game",
            "1.0.0",
            "vi",
            new DateOnly(2026, 9, 12),
            new GameInstallDetection([], []),
            [
                new GamePackageFile(
                    "payload/vi.pak",
                    "Game/Mods/vi.pak",
                    new string('b', 64),
                    20,
                    PackageFileRole.Translation)
            ],
            prerequisites);
        return new AvailableGamePackage(package, "C:\\Packages\\manifest.json", new string('c', 64));
    }

    private sealed class MatchingDetector : IGameInstallationDetector
    {
        public Task<bool> IsMatchAsync(
            string gameRoot,
            GameInstallDetection detection,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class EmptyReceiptRepository : IInstallationReceiptRepository
    {
        public Task<InstallationReceipt?> FindAsync(
            string gameId,
            Guid installationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<InstallationReceipt?>(null);

        public Task SaveAsync(
            InstallationReceipt receipt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string gameId,
            Guid installationId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeFileStateReader : IInstalledFileStateReader
    {
        public Dictionary<string, InstalledFileState> States { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public Task<InstalledFileState> ReadAsync(
            string gameRoot,
            string relativePath,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(States.GetValueOrDefault(relativePath, InstalledFileState.Missing));
    }

    private sealed class ValidIntegrityVerifier : IPackageIntegrityVerifier
    {
        public Task VerifyAsync(
            AvailableGamePackage availablePackage,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
