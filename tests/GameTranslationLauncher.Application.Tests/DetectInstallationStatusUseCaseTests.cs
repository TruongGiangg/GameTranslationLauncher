using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Tests;

[TestClass]
public sealed class DetectInstallationStatusUseCaseTests
{
    private const string InstalledHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ManifestHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private static readonly Guid InstallationId = Guid.Parse("7a6511b5-a65f-4e84-bc39-e6ca8fa1bb78");

    [TestMethod]
    public async Task ExecuteAsync_NoSelectedInstallation_ReturnsUnknown()
    {
        var useCase = CreateUseCase(isMatchingGame: true);

        var status = await useCase.ExecuteAsync(CreatePackage(), installation: null);

        Assert.AreEqual(InstallationStatus.Unknown, status);
    }

    [TestMethod]
    public async Task ExecuteAsync_GameMarkersDoNotMatch_ReturnsUnknown()
    {
        var useCase = CreateUseCase(isMatchingGame: false);

        var status = await useCase.ExecuteAsync(CreatePackage(), CreateInstallation());

        Assert.AreEqual(InstallationStatus.Unknown, status);
    }

    [TestMethod]
    public async Task ExecuteAsync_NoReceiptAndNoDestinationFiles_ReturnsNotInstalled()
    {
        var useCase = CreateUseCase(isMatchingGame: true);

        var status = await useCase.ExecuteAsync(CreatePackage(), CreateInstallation());

        Assert.AreEqual(InstallationStatus.NotInstalled, status);
    }

    [TestMethod]
    public async Task ExecuteAsync_NoReceiptButDestinationExists_ReturnsConflict()
    {
        var fileReader = new FakeInstalledFileStateReader();
        fileReader.States["Game/Mods/vi.pak"] = new InstalledFileState(true, 10, InstalledHash);
        var useCase = CreateUseCase(isMatchingGame: true, fileReader: fileReader);

        var status = await useCase.ExecuteAsync(CreatePackage(), CreateInstallation());

        Assert.AreEqual(InstallationStatus.Conflict, status);
    }

    [TestMethod]
    public async Task ExecuteAsync_ReceiptAndFilesMatch_ReturnsInstalled()
    {
        var fileReader = CreateMatchingFileReader();
        var useCase = CreateUseCase(
            isMatchingGame: true,
            receipt: CreateReceipt(),
            fileReader: fileReader);

        var status = await useCase.ExecuteAsync(CreatePackage(), CreateInstallation());

        Assert.AreEqual(InstallationStatus.Installed, status);
    }

    [TestMethod]
    public async Task ExecuteAsync_AvailableVersionIsNewer_ReturnsUpdateAvailable()
    {
        var fileReader = CreateMatchingFileReader();
        var useCase = CreateUseCase(
            isMatchingGame: true,
            receipt: CreateReceipt(packageVersion: "1.0.0"),
            fileReader: fileReader);

        var status = await useCase.ExecuteAsync(
            CreatePackage(packageVersion: "1.1.0"),
            CreateInstallation());

        Assert.AreEqual(InstallationStatus.UpdateAvailable, status);
    }

    [TestMethod]
    public async Task ExecuteAsync_ReceiptFileIsMissing_ReturnsDamaged()
    {
        var useCase = CreateUseCase(
            isMatchingGame: true,
            receipt: CreateReceipt());

        var status = await useCase.ExecuteAsync(CreatePackage(), CreateInstallation());

        Assert.AreEqual(InstallationStatus.Damaged, status);
    }

    [TestMethod]
    public async Task ExecuteAsync_SameVersionButManifestChanged_ReturnsConflict()
    {
        var fileReader = CreateMatchingFileReader();
        var receipt = CreateReceipt(manifestSha256: InstalledHash);
        var useCase = CreateUseCase(true, receipt, fileReader);

        var status = await useCase.ExecuteAsync(CreatePackage(), CreateInstallation());

        Assert.AreEqual(InstallationStatus.Conflict, status);
    }

    private static DetectInstallationStatusUseCase CreateUseCase(
        bool isMatchingGame,
        InstallationReceipt? receipt = null,
        FakeInstalledFileStateReader? fileReader = null)
    {
        return new DetectInstallationStatusUseCase(
            new FakeGameInstallationDetector(isMatchingGame),
            new FakeInstallationReceiptRepository(receipt),
            fileReader ?? new FakeInstalledFileStateReader());
    }

    private static AvailableGamePackage CreatePackage(string packageVersion = "1.0.0")
    {
        var packageFile = new GamePackageFile(
            "payload/vi.pak",
            "Game/Mods/vi.pak",
            InstalledHash,
            10,
            PackageFileRole.Translation);
        var package = new GamePackage(
            1,
            "sample-game",
            "Sample Game",
            packageVersion,
            "vi",
            new DateOnly(2026, 9, 12),
            new GameInstallDetection(
                [new GameDetectionMarker("Game.exe", GameDetectionMarkerKind.File)],
                []),
            [packageFile],
            []);

        return new AvailableGamePackage(package, "C:\\Packages\\launcher-package.json", ManifestHash);
    }

    private static GameInstallationSetting CreateInstallation()
    {
        return new GameInstallationSetting(
            "sample-game",
            InstallationId,
            "C:\\Games\\Sample",
            DateTimeOffset.Parse("2026-09-12T00:00:00Z"));
    }

    private static InstallationReceipt CreateReceipt(
        string packageVersion = "1.0.0",
        string manifestSha256 = ManifestHash)
    {
        return new InstallationReceipt(
            1,
            InstallationId,
            "sample-game",
            "C:\\Games\\Sample",
            packageVersion,
            manifestSha256,
            "1.0.0",
            DateTimeOffset.Parse("2026-09-12T00:00:00Z"),
            [],
            [
                new InstalledFileReceipt(
                    "Game/Mods/vi.pak",
                    InstalledHash,
                    10,
                    PackageFileRole.Translation,
                    null,
                    InstalledFileOwnership.Created,
                    null,
                    null,
                    null)
            ]);
    }

    private static FakeInstalledFileStateReader CreateMatchingFileReader()
    {
        var reader = new FakeInstalledFileStateReader();
        reader.States["Game/Mods/vi.pak"] = new InstalledFileState(true, 10, InstalledHash);
        return reader;
    }

    private sealed class FakeGameInstallationDetector(bool result) : IGameInstallationDetector
    {
        public Task<bool> IsMatchAsync(
            string gameRoot,
            GameInstallDetection detection,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(result);
        }
    }

    private sealed class FakeInstallationReceiptRepository(InstallationReceipt? receipt)
        : IInstallationReceiptRepository
    {
        public Task<InstallationReceipt?> FindAsync(
            string gameId,
            Guid installationId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(receipt);
        }

        public Task SaveAsync(
            InstallationReceipt value,
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

    private sealed class FakeInstalledFileStateReader : IInstalledFileStateReader
    {
        public Dictionary<string, InstalledFileState> States { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<InstalledFileState> ReadAsync(
            string gameRoot,
            string relativePath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                States.GetValueOrDefault(relativePath, InstalledFileState.Missing));
        }
    }
}
