using GameTranslationLauncher.Application.Settings;
using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Tests;

[TestClass]
public sealed class SaveLastSelectedGameUseCaseTests
{
    [TestMethod]
    public async Task ExecuteAsync_ChangedGame_PreservesPreferencesAndInstallations()
    {
        var installation = new GameInstallationSetting(
            "tiny-eden", Guid.NewGuid(), "D:\\Games\\Tiny Eden", DateTimeOffset.UtcNow);
        var repository = new SettingsRepository(new LauncherSettings(1, [installation], "en", "cyan", "dark", "tiny-eden"));
        var useCase = new SaveLastSelectedGameUseCase(repository);

        await useCase.ExecuteAsync("the-witcher-3-wild-hunt");
        var saved = await repository.LoadAsync();

        Assert.AreEqual("the-witcher-3-wild-hunt", saved.LastSelectedGameId);
        Assert.AreEqual("en", saved.DisplayLanguage);
        Assert.AreEqual("cyan", saved.AccentTheme);
        Assert.AreEqual("dark", saved.SurfaceTheme);
        Assert.AreEqual(installation, saved.Installations.Single());
        Assert.AreEqual(1, repository.SaveCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_UnchangedGame_DoesNotWrite()
    {
        var repository = new SettingsRepository(LauncherSettings.Empty with { LastSelectedGameId = "tiny-eden" });
        var useCase = new SaveLastSelectedGameUseCase(repository);

        await useCase.ExecuteAsync("tiny-eden");

        Assert.AreEqual(0, repository.SaveCount);
    }

    private sealed class SettingsRepository(LauncherSettings settings) : ILauncherSettingsRepository
    {
        private LauncherSettings settings = settings;

        public int SaveCount { get; private set; }

        public Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(settings);

        public Task SaveAsync(LauncherSettings newSettings, CancellationToken cancellationToken = default)
        {
            settings = newSettings;
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
