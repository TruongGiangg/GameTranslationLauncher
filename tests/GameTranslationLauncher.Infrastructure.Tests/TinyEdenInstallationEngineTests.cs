using System.Security.Cryptography;
using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Domain.Settings;
using GameTranslationLauncher.Infrastructure.Catalog;
using GameTranslationLauncher.Infrastructure.FileSystem;
using GameTranslationLauncher.Infrastructure.Persistence;

namespace GameTranslationLauncher.Infrastructure.Tests;

[TestClass]
public sealed class TinyEdenInstallationEngineTests
{
    [TestMethod]
    public async Task InstallAndUninstall_RealLocalPackage_RoundTripsInTemporaryGameRoot()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var packageRepository = new JsonGamePackageRepository();
        var package = await packageRepository.LoadAsync(TestRepositoryPaths.TinyEdenManifestPath);
        var gameRoot = temporaryDirectory.GetPath("Tiny Eden");
        CreateGameMarkers(gameRoot, package);

        var availablePackage = new AvailableGamePackage(
            package,
            TestRepositoryPaths.TinyEdenManifestPath,
            await ComputeHashAsync(TestRepositoryPaths.TinyEdenManifestPath));
        var installation = new GameInstallationSetting(
            package.GameId,
            Guid.NewGuid(),
            gameRoot,
            DateTimeOffset.UtcNow);
        var request = new ApplyTranslationRequest(
            availablePackage,
            installation,
            package.Prerequisites
                .Where(prerequisite => prerequisite.RequiresExplicitConsent)
                .Select(prerequisite => prerequisite.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase));
        var (install, uninstall) = CreateUseCases(temporaryDirectory);

        var installResult = await install.ExecuteAsync(request);

        Assert.IsTrue(installResult.Succeeded, installResult.Message);
        Assert.HasCount(5, installResult.Receipt!.Files);
        foreach (var file in EnumerateFiles(package))
        {
            Assert.IsTrue(File.Exists(GetGamePath(gameRoot, file.Destination)), file.Destination);
        }

        var uninstallResult = await uninstall.ExecuteAsync(package.GameId, installation.InstallationId);

        Assert.IsTrue(uninstallResult.Succeeded, uninstallResult.Message);
        foreach (var file in EnumerateFiles(package))
        {
            Assert.IsFalse(File.Exists(GetGamePath(gameRoot, file.Destination)), file.Destination);
        }
    }

    private static (InstallTranslationUseCase Install, UninstallTranslationUseCase Uninstall)
        CreateUseCases(TemporaryDirectory temporaryDirectory)
    {
        var storagePaths = new LauncherStoragePaths(temporaryDirectory.GetPath("state"));
        var receiptRepository = new JsonInstallationReceiptRepository(storagePaths);
        var detector = new FileSystemGameInstallationDetector();
        var stateReader = new Sha256InstalledFileStateReader();
        var integrityVerifier = new Sha256PackageIntegrityVerifier();
        var logger = new NullInstallationOperationLogger();
        var operationLock = new InstallationOperationLock();
        var buildInstall = new BuildInstallPlanUseCase(
            detector,
            receiptRepository,
            stateReader,
            integrityVerifier);
        var installExecutor = new FileSystemInstallPlanExecutor(
            new FileSystemInstallPlanPreflight(integrityVerifier, storagePaths),
            receiptRepository,
            storagePaths,
            logger);
        var uninstallExecutor = new FileSystemUninstallPlanExecutor(
            new FileSystemUninstallPlanPreflight(storagePaths),
            receiptRepository,
            storagePaths,
            logger);

        return (
            new InstallTranslationUseCase(buildInstall, installExecutor, operationLock, logger),
            new UninstallTranslationUseCase(
                receiptRepository,
                new BuildUninstallPlanUseCase(stateReader),
                uninstallExecutor,
                operationLock,
                logger));
    }

    private static void CreateGameMarkers(string gameRoot, GamePackage package)
    {
        Directory.CreateDirectory(gameRoot);
        foreach (var marker in package.InstallDetection.RequiredPaths)
        {
            var path = GetGamePath(gameRoot, marker.Path);
            if (marker.Kind == GameDetectionMarkerKind.Directory)
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, "test marker");
            }
        }
    }

    private static IEnumerable<GamePackageFile> EnumerateFiles(GamePackage package) =>
        package.PayloadFiles.Concat(
            package.Prerequisites.SelectMany(prerequisite => prerequisite.Files));

    private static string GetGamePath(string gameRoot, string relativePath) =>
        Path.Combine(gameRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static async Task<string> ComputeHashAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream));
    }
}
