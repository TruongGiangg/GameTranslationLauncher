using System.IO;
using System.Reflection;
using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Application.Settings;
using GameTranslationLauncher.Application.Updates;
using GameTranslationLauncher.Infrastructure.Catalog;
using GameTranslationLauncher.Infrastructure.FileSystem;
using GameTranslationLauncher.Infrastructure.Logging;
using GameTranslationLauncher.Infrastructure.Persistence;
using GameTranslationLauncher.Infrastructure.Updates;
using GameTranslationLauncher.Wpf.ViewModels;
using GameTranslationLauncher.Wpf.Services;

namespace GameTranslationLauncher.Wpf.Composition;

/// <summary>
/// Ghép các implementation Infrastructure vào use case và ViewModel của WPF.
/// </summary>
public static class LauncherCompositionRoot
{
    private const string UpdateFeedOwner = "TruongGiangg";
    private const string UpdateFeedRepository = "GameTranslationLauncher";

    public static GameLibraryViewModel CreateGameLibraryViewModel()
    {
        var storagePaths = LauncherStoragePaths.CreateDefault();
        var settingsRepository = new JsonLauncherSettingsRepository(storagePaths);
        var receiptRepository = new JsonInstallationReceiptRepository(storagePaths);
        var installationDetector = new FileSystemGameInstallationDetector();
        var installedFileStateReader = new Sha256InstalledFileStateReader();
        var packageIntegrityVerifier = new Sha256PackageIntegrityVerifier();
        var operationLogger = new JsonLinesInstallationOperationLogger(storagePaths);
        var buildInstallPlan = new BuildInstallPlanUseCase(
            installationDetector,
            receiptRepository,
            installedFileStateReader,
            packageIntegrityVerifier);
        var operationLock = new InstallationOperationLock();
        var installPreflight = new FileSystemInstallPlanPreflight(
            packageIntegrityVerifier,
            storagePaths);
        var installExecutor = new FileSystemInstallPlanExecutor(
            installPreflight,
            receiptRepository,
            storagePaths,
            operationLogger);
        var uninstallPreflight = new FileSystemUninstallPlanPreflight(storagePaths);
        var uninstallExecutor = new FileSystemUninstallPlanExecutor(
            uninstallPreflight,
            receiptRepository,
            storagePaths,
            operationLogger);
        var detectStatus = new DetectInstallationStatusUseCase(
            installationDetector,
            receiptRepository,
            installedFileStateReader);
        var catalogRepository = new LocalGameCatalogRepository(
            FindGamesRoot(),
            new JsonGamePackageRepository());
        var loadCatalogUseCase = new LoadGameCatalogUseCase(
            catalogRepository,
            settingsRepository);
        var checkGameStatusUseCase = new CheckGameStatusUseCase(detectStatus);
        var settings = new LauncherSettingsViewModel(
            new LoadLauncherPreferencesUseCase(settingsRepository),
            new SaveLauncherPreferencesUseCase(settingsRepository),
            new LauncherThemeManager(),
            new WindowsExternalUriLauncher());
        var updateViewModel = new UpdateViewModel(
            new CheckForLauncherUpdateUseCase(new GitHubReleaseFeedRepository(UpdateFeedOwner, UpdateFeedRepository)),
            new DownloadLauncherUpdateUseCase(new HttpUpdatePackageDownloader(storagePaths)),
            new LauncherUpdateInstaller(),
            Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0));
        var operations = new GameOperationViewModel(
            new SelectGameInstallationUseCase(installationDetector, settingsRepository),
            new PrepareTranslationPlanUseCase(buildInstallPlan, installedFileStateReader),
            new InstallTranslationUseCase(buildInstallPlan, installExecutor, operationLock, operationLogger),
            new UpdateTranslationUseCase(buildInstallPlan, installExecutor, operationLock, operationLogger),
            new UninstallTranslationUseCase(
                receiptRepository,
                new BuildUninstallPlanUseCase(installedFileStateReader),
                uninstallExecutor,
                operationLock,
                operationLogger),
            new WindowsGameFolderPicker(),
            new WindowsGameShell());

        return new GameLibraryViewModel(
            loadCatalogUseCase,
            checkGameStatusUseCase,
            new GameArtworkResolver(),
            new GamePresentationResolver(),
            operations,
            settings,
            updateViewModel);
    }

    private static string FindGamesRoot()
    {
        // NOTE(catalog-root): Bản phát hành có thể chưa mang catalog cùng executable.
        // Fallback vẫn trả một đường dẫn hợp lệ để use case cho catalog rỗng thay vì làm app lỗi.
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "games");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "games");
    }
}
