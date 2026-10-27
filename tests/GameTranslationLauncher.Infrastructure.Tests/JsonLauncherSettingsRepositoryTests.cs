using GameTranslationLauncher.Application.State;
using GameTranslationLauncher.Domain.Settings;
using GameTranslationLauncher.Infrastructure.Persistence;

namespace GameTranslationLauncher.Infrastructure.Tests;

[TestClass]
public sealed class JsonLauncherSettingsRepositoryTests
{
    [TestMethod]
    public async Task LoadAsync_SettingsFileDoesNotExist_ReturnsEmptySettings()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var repository = CreateRepository(temporaryDirectory);

        var settings = await repository.LoadAsync();

        Assert.AreEqual(1, settings.SchemaVersion);
        Assert.IsEmpty(settings.Installations);
        Assert.AreEqual("vi", settings.DisplayLanguage);
        Assert.AreEqual("crimson", settings.AccentTheme);
        Assert.AreEqual("glass", settings.SurfaceTheme);
    }

    [TestMethod]
    public async Task LoadAsync_SettingsWithoutLastSelectedGameId_ReturnsNull()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var paths = new LauncherStoragePaths(temporaryDirectory.RootPath);
        File.WriteAllText(paths.SettingsPath, "{\"schemaVersion\":1,\"installations\":[],\"surfaceTheme\":\"dark\"}");
        var repository = new JsonLauncherSettingsRepository(paths);

        var settings = await repository.LoadAsync();

        Assert.IsNull(settings.LastSelectedGameId);
        Assert.AreEqual("dark", settings.SurfaceTheme);
    }

    [TestMethod]
    public async Task LoadAndSaveAsync_LastSelectedGameId_PreservesFieldAndPreferences()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var paths = new LauncherStoragePaths(temporaryDirectory.RootPath);
        File.WriteAllText(paths.SettingsPath,
            "{\"schemaVersion\":1,\"installations\":[],\"displayLanguage\":\"en\",\"accentTheme\":\"cyan\",\"surfaceTheme\":\"dark\",\"lastSelectedGameId\":\"the-witcher-3-wild-hunt\"}");
        var repository = new JsonLauncherSettingsRepository(paths);

        await repository.SaveAsync(await repository.LoadAsync());
        var settings = await repository.LoadAsync();

        Assert.AreEqual("the-witcher-3-wild-hunt", settings.LastSelectedGameId);
        Assert.AreEqual("en", settings.DisplayLanguage);
        Assert.AreEqual("cyan", settings.AccentTheme);
        Assert.AreEqual("dark", settings.SurfaceTheme);
        StringAssert.Contains(File.ReadAllText(paths.SettingsPath), "\"lastSelectedGameId\"");
    }

    [TestMethod]
    public async Task LoadAsync_InvalidLastSelectedGameId_ThrowsStateContractException()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var paths = new LauncherStoragePaths(temporaryDirectory.RootPath);
        File.WriteAllText(paths.SettingsPath, "{\"schemaVersion\":1,\"installations\":[],\"lastSelectedGameId\":\"../game\"}");
        var repository = new JsonLauncherSettingsRepository(paths);

        var exception = await Assert.ThrowsAsync<LauncherStateContractException>(
            () => repository.LoadAsync());

        StringAssert.Contains(exception.Message, "lastSelectedGameId");
    }

    [TestMethod]
    public async Task LoadAsync_SettingsWithoutSurfaceTheme_DefaultsToGlass()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var paths = new LauncherStoragePaths(temporaryDirectory.RootPath);
        File.WriteAllText(paths.SettingsPath, "{\"schemaVersion\":1,\"installations\":[],\"accentTheme\":\"blue\"}");
        var repository = new JsonLauncherSettingsRepository(paths);

        var settings = await repository.LoadAsync();

        Assert.AreEqual("blue", settings.AccentTheme);
        Assert.AreEqual("glass", settings.SurfaceTheme);
    }

    [TestMethod]
    public async Task LoadAsync_UnsupportedSurfaceTheme_ThrowsStateContractException()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var paths = new LauncherStoragePaths(temporaryDirectory.RootPath);
        File.WriteAllText(paths.SettingsPath, "{\"schemaVersion\":1,\"installations\":[],\"surfaceTheme\":\"light\"}");
        var repository = new JsonLauncherSettingsRepository(paths);

        var exception = await Assert.ThrowsAsync<LauncherStateContractException>(
            () => repository.LoadAsync());

        StringAssert.Contains(exception.Message, "surfaceTheme");
    }

    [TestMethod]
    public async Task SaveAndLoadAsync_ValidSettings_RoundTrips()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var repository = CreateRepository(temporaryDirectory);
        var installationId = Guid.NewGuid();
        var settings = new LauncherSettings(
            1,
            [
                new GameInstallationSetting(
                    "tiny-eden",
                    installationId,
                    "D:\\Games\\Tiny Eden",
                    DateTimeOffset.Parse("2026-09-12T00:00:00Z"))
            ],
            "en",
            "cyan",
            "dark");

        await repository.SaveAsync(settings);
        var loaded = await repository.LoadAsync();

        Assert.HasCount(1, loaded.Installations);
        Assert.AreEqual(installationId, loaded.Installations[0].InstallationId);
        Assert.AreEqual("D:\\Games\\Tiny Eden", loaded.Installations[0].GameRoot);
        Assert.AreEqual("en", loaded.DisplayLanguage);
        Assert.AreEqual("cyan", loaded.AccentTheme);
        Assert.AreEqual("dark", loaded.SurfaceTheme);
        Assert.IsEmpty(Directory.GetFiles(temporaryDirectory.RootPath, "*.tmp", SearchOption.AllDirectories));
    }

    [TestMethod]
    public async Task LoadAsync_UnsupportedSchemaVersion_ThrowsStateContractException()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var paths = new LauncherStoragePaths(temporaryDirectory.RootPath);
        File.WriteAllText(paths.SettingsPath, "{\"schemaVersion\":2,\"installations\":[]}");
        var repository = new JsonLauncherSettingsRepository(paths);

        var exception = await Assert.ThrowsAsync<LauncherStateContractException>(
            () => repository.LoadAsync());

        StringAssert.Contains(exception.Message, "schemaVersion '2'");
    }

    private static JsonLauncherSettingsRepository CreateRepository(
        TemporaryDirectory temporaryDirectory)
    {
        return new JsonLauncherSettingsRepository(
            new LauncherStoragePaths(temporaryDirectory.RootPath));
    }
}
