using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Tests;

[TestClass]
public sealed class PrepareTranslationPlanUseCaseTests
{
    private const string FileHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [TestMethod]
    public async Task PrepareInstallAsync_ExistingUnownedFile_PreviewsBackupAndReplacement()
    {
        var fileReader = new FileStateReader();
        fileReader.States["Game/Mods/vi.pak"] = new InstalledFileState(true, 10, FileHash);
        var buildInstall = new BuildInstallPlanUseCase(
            new MatchingDetector(),
            new EmptyReceiptRepository(),
            fileReader,
            new IntegrityVerifier());
        var useCase = new PrepareTranslationPlanUseCase(buildInstall, fileReader);
        var request = new ApplyTranslationRequest(
            CreatePackage(),
            new GameInstallationSetting("sample-game", Guid.NewGuid(), "C:\\Games\\Sample", DateTimeOffset.UtcNow),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var plan = await useCase.PrepareInstallAsync(request);

        Assert.AreEqual(InstallFileAction.ReplaceExternal, plan.Files.Single().Action);
        Assert.AreEqual("Game/Mods/vi.pak.original", plan.Files.Single().BackupRelativePath);
    }

    private static AvailableGamePackage CreatePackage() => new(
        new GamePackage(
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
            []),
        "C:\\Packages\\launcher-package.json",
        new string('c', 64));

    private sealed class MatchingDetector : IGameInstallationDetector
    {
        public Task<bool> IsMatchAsync(
            string gameRoot,
            GameInstallDetection detection,
            CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class EmptyReceiptRepository : IInstallationReceiptRepository
    {
        public Task<InstallationReceipt?> FindAsync(
            string gameId,
            Guid installationId,
            CancellationToken cancellationToken = default) => Task.FromResult<InstallationReceipt?>(null);

        public Task SaveAsync(InstallationReceipt receipt, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string gameId, Guid installationId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FileStateReader : IInstalledFileStateReader
    {
        public Dictionary<string, InstalledFileState> States { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public Task<InstalledFileState> ReadAsync(
            string gameRoot,
            string relativePath,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(States.GetValueOrDefault(relativePath, InstalledFileState.Missing));
    }

    private sealed class IntegrityVerifier : IPackageIntegrityVerifier
    {
        public Task VerifyAsync(
            AvailableGamePackage availablePackage,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
