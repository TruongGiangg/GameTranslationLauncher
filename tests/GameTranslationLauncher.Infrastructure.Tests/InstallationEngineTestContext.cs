using System.Security.Cryptography;
using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Domain.Settings;
using GameTranslationLauncher.Infrastructure.FileSystem;
using GameTranslationLauncher.Infrastructure.Persistence;

namespace GameTranslationLauncher.Infrastructure.Tests;

internal sealed class InstallationEngineTestContext : IDisposable
{
    private int manifestRevision;

    public InstallationEngineTestContext()
    {
        TemporaryDirectory = new TemporaryDirectory();
        GameRoot = TemporaryDirectory.GetPath("game");
        PackageRoot = TemporaryDirectory.GetPath("package");
        Directory.CreateDirectory(GameRoot);
        Directory.CreateDirectory(PackageRoot);
        File.WriteAllText(Path.Combine(GameRoot, "Game.exe"), "marker");

        Installation = new GameInstallationSetting(
            "sample-game",
            Guid.NewGuid(),
            GameRoot,
            DateTimeOffset.UtcNow);
        StoragePaths = new LauncherStoragePaths(TemporaryDirectory.GetPath("state"));
        ReceiptRepository = new JsonInstallationReceiptRepository(StoragePaths);

        var detector = new FileSystemGameInstallationDetector();
        var stateReader = new Sha256InstalledFileStateReader();
        var integrityVerifier = new Sha256PackageIntegrityVerifier();
        var installPreflight = new FileSystemInstallPlanPreflight(integrityVerifier, StoragePaths);
        var uninstallPreflight = new FileSystemUninstallPlanPreflight(StoragePaths);
        var logger = new NullInstallationOperationLogger();
        var operationLock = new InstallationOperationLock();
        var buildInstallPlan = new BuildInstallPlanUseCase(
            detector,
            ReceiptRepository,
            stateReader,
            integrityVerifier);
        var installExecutor = new FileSystemInstallPlanExecutor(
            installPreflight,
            ReceiptRepository,
            StoragePaths,
            logger);
        var buildUninstallPlan = new BuildUninstallPlanUseCase(stateReader);
        var uninstallExecutor = new FileSystemUninstallPlanExecutor(
            uninstallPreflight,
            ReceiptRepository,
            StoragePaths,
            logger);

        Install = new InstallTranslationUseCase(
            buildInstallPlan,
            installExecutor,
            operationLock,
            logger);
        Update = new UpdateTranslationUseCase(
            buildInstallPlan,
            installExecutor,
            operationLock,
            logger);
        Uninstall = new UninstallTranslationUseCase(
            ReceiptRepository,
            buildUninstallPlan,
            uninstallExecutor,
            operationLock,
            logger);
    }

    public TemporaryDirectory TemporaryDirectory { get; }

    public string GameRoot { get; }

    public string PackageRoot { get; }

    public GameInstallationSetting Installation { get; }

    public LauncherStoragePaths StoragePaths { get; }

    public JsonInstallationReceiptRepository ReceiptRepository { get; }

    public InstallTranslationUseCase Install { get; }

    public UpdateTranslationUseCase Update { get; }

    public UninstallTranslationUseCase Uninstall { get; }

    public AvailableGamePackage CreatePackage(
        string version,
        params TestPackageFile[] definitions)
    {
        foreach (var definition in definitions)
        {
            var sourcePath = GetPackagePath(definition.Source);
            Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
            File.WriteAllBytes(sourcePath, definition.Content);
        }

        var manifestPath = Path.Combine(PackageRoot, "launcher-package.json");
        File.WriteAllText(
            manifestPath,
            $"test-manifest:{version}:{++manifestRevision}");

        var payload = definitions
            .Where(definition => definition.Role != PackageFileRole.Prerequisite)
            .Select(CreatePackageFile)
            .ToArray();
        var prerequisites = definitions
            .Where(definition => definition.Role == PackageFileRole.Prerequisite)
            .GroupBy(definition => definition.PrerequisiteId!, StringComparer.OrdinalIgnoreCase)
            .Select(group => new GamePrerequisite(
                group.Key,
                group.Key,
                "Test prerequisite",
                group.Any(definition => definition.RequiresConsent),
                group.Select(CreatePackageFile).ToArray()))
            .ToArray();
        var package = new GamePackage(
            1,
            "sample-game",
            "Sample Game",
            version,
            "vi",
            new DateOnly(2026, 9, 12),
            new GameInstallDetection(
                [new GameDetectionMarker("Game.exe", GameDetectionMarkerKind.File)],
                []),
            payload,
            prerequisites);

        return new AvailableGamePackage(
            package,
            manifestPath,
            Hash(File.ReadAllBytes(manifestPath)));
    }

    public ApplyTranslationRequest CreateRequest(
        AvailableGamePackage package,
        IEnumerable<string>? acceptedPrerequisites = null,
        IEnumerable<string>? approvedReplacements = null,
        GameInstallationSetting? installation = null)
    {
        return new ApplyTranslationRequest(
            package,
            installation ?? Installation,
            new HashSet<string>(
                acceptedPrerequisites ?? [],
                StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(
                approvedReplacements ?? [],
                StringComparer.OrdinalIgnoreCase));
    }

    public string GetGamePath(string relativePath) =>
        Path.Combine(GameRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

    public void WriteGameFile(string relativePath, byte[] content)
    {
        var path = GetGamePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
    }

    public void Dispose()
    {
        TemporaryDirectory.Dispose();
    }

    private string GetPackagePath(string relativePath) =>
        Path.Combine(PackageRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static GamePackageFile CreatePackageFile(TestPackageFile definition) =>
        new(
            definition.Source,
            definition.Destination,
            Hash(definition.Content),
            definition.Content.LongLength,
            definition.Role,
            definition.PrerequisiteId);

    private static string Hash(byte[] content) =>
        Convert.ToHexStringLower(SHA256.HashData(content));
}

internal sealed record TestPackageFile(
    string Source,
    string Destination,
    byte[] Content,
    PackageFileRole Role = PackageFileRole.Translation,
    string? PrerequisiteId = null,
    bool RequiresConsent = false);
