using GameTranslationLauncher.Application.Settings;
using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Tests;

[TestClass]
public sealed class SaveLauncherPreferencesUseCaseTests
{
    [TestMethod]
    public async Task ExecuteAsync_ValidPreferences_PreservesInstallations()
    {
        var installation = new GameInstallationSetting(
            "tiny-eden",
            Guid.NewGuid(),
            "D:\\Games\\Tiny Eden",
            DateTimeOffset.Parse("2026-09-12T00:00:00Z"));
        var repository = new SettingsRepository(new LauncherSettings(1, [installation]));
        var useCase = new SaveLauncherPreferencesUseCase(repository);

        var saved = await useCase.ExecuteAsync("en", "white");

        Assert.AreEqual("en", saved.DisplayLanguage);
        Assert.AreEqual("white", saved.AccentTheme);
        Assert.AreEqual(installation, saved.Installations.Single());
    }

    [TestMethod]
    public async Task ExecuteAsync_UnsupportedAccent_Throws()
    {
        var repository = new SettingsRepository(LauncherSettings.Empty);
        var useCase = new SaveLauncherPreferencesUseCase(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => useCase.ExecuteAsync("vi", "pink"));
    }

    private sealed class SettingsRepository(LauncherSettings settings) : ILauncherSettingsRepository
    {
        private LauncherSettings settings = settings;

        public Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(settings);

        public Task SaveAsync(LauncherSettings newSettings, CancellationToken cancellationToken = default)
        {
            settings = newSettings;
            return Task.CompletedTask;
        }
    }
}
