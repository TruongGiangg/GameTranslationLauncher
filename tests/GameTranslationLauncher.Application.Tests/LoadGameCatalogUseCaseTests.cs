using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Application.Settings;
using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Tests;

[TestClass]
public sealed class LoadGameCatalogUseCaseTests
{
    [TestMethod]
    public async Task ExecuteAsync_MultipleInstallations_UsesMostRecentWithoutCheckingStatus()
    {
        var package = CreatePackage();
        var older = CreateSetting("C:\\Games\\Old", "2026-09-10T00:00:00Z");
        var recent = CreateSetting("C:\\Games\\Recent", "2026-09-12T00:00:00Z");
        var useCase = new LoadGameCatalogUseCase(
            new CatalogRepository(package),
            new SettingsRepository(new LauncherSettings(1, [older, recent])));

        var result = await useCase.ExecuteAsync();

        Assert.HasCount(1, result.Items);
        Assert.AreEqual(recent.GameRoot, result.Items[0].GameRoot);
        Assert.AreEqual(InstallationStatus.Unknown, result.Items[0].Status);
    }

    [TestMethod]
    public async Task CheckGameStatusAsync_UsesDetectorForRequestedGameOnly()
    {
        var package = CreatePackage();
        var installation = CreateSetting("C:\\Games\\Selected", "2026-09-12T00:00:00Z");
        var detector = new MatchingGameDetector();
        var useCase = new CheckGameStatusUseCase(new DetectInstallationStatusUseCase(
            detector,
            new EmptyReceiptRepository(),
            new MissingFileReader()));
        var item = new GameCatalogItem(
            package,
            installation.InstallationId,
            installation.GameRoot,
            InstallationStatus.Unknown);

        var result = await useCase.ExecuteAsync(item);

        Assert.AreEqual(InstallationStatus.NotInstalled, result.Status);
        Assert.AreEqual(1, detector.CallCount);
    }

    private static AvailableGamePackage CreatePackage()
    {
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
                    new string('a', 64),
                    10,
                    PackageFileRole.Translation)
            ],
            []);
        return new AvailableGamePackage(package, "manifest.json", new string('b', 64));
    }

    private static GameInstallationSetting CreateSetting(string gameRoot, string lastUsedAtUtc)
    {
        return new GameInstallationSetting(
            "sample-game",
            Guid.NewGuid(),
            gameRoot,
            DateTimeOffset.Parse(lastUsedAtUtc));
    }

    private sealed class CatalogRepository(AvailableGamePackage package) : IGameCatalogRepository
    {
        public Task<GameCatalogSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GameCatalogSnapshot([package], []));
        }
    }

    private sealed class SettingsRepository(LauncherSettings settings) : ILauncherSettingsRepository
    {
        public Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(settings);
        }

        public Task SaveAsync(LauncherSettings value, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class MatchingGameDetector : IGameInstallationDetector
    {
        public int CallCount { get; private set; }

        public Task<bool> IsMatchAsync(
            string gameRoot,
            GameInstallDetection detection,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(true);
        }
    }

    private sealed class EmptyReceiptRepository : IInstallationReceiptRepository
    {
        public Task<InstallationReceipt?> FindAsync(
            string gameId,
            Guid installationId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<InstallationReceipt?>(null);
        }

        public Task SaveAsync(
            InstallationReceipt receipt,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(
            string gameId,
            Guid installationId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class MissingFileReader : IInstalledFileStateReader
    {
        public Task<InstalledFileState> ReadAsync(
            string gameRoot,
            string relativePath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(InstalledFileState.Missing);
        }
    }
}
