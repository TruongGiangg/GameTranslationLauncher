using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Application.Settings;
using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Tests;

[TestClass]
public sealed class SelectGameInstallationUseCaseTests
{
    [TestMethod]
    public async Task ExecuteAsync_MatchingFolder_SavesLatestInstallation()
    {
        var repository = new InMemorySettingsRepository(LauncherSettings.Empty);
        var useCase = new SelectGameInstallationUseCase(new MatchingDetector(true), repository);

        var installation = await useCase.ExecuteAsync(CreatePackage(), "C:\\Games\\Sample\\");

        Assert.AreEqual("sample-game", installation.GameId);
        Assert.AreEqual("C:\\Games\\Sample", installation.GameRoot);
        Assert.AreEqual(installation, repository.LastSaved?.Installations.Single());
    }

    [TestMethod]
    public async Task ExecuteAsync_SameFolder_ReusesInstallationIdentity()
    {
        var existing = new GameInstallationSetting(
            "sample-game",
            Guid.NewGuid(),
            "C:\\Games\\Sample",
            DateTimeOffset.UtcNow.AddDays(-1));
        var repository = new InMemorySettingsRepository(new LauncherSettings(1, [existing]));
        var useCase = new SelectGameInstallationUseCase(new MatchingDetector(true), repository);

        var installation = await useCase.ExecuteAsync(CreatePackage(), "C:\\Games\\Sample");

        Assert.AreEqual(existing.InstallationId, installation.InstallationId);
    }

    [TestMethod]
    public async Task ExecuteAsync_NonMatchingFolder_DoesNotSaveSettings()
    {
        var repository = new InMemorySettingsRepository(LauncherSettings.Empty);
        var useCase = new SelectGameInstallationUseCase(new MatchingDetector(false), repository);

        var exception = await Assert.ThrowsAsync<InstallationOperationException>(
            () => useCase.ExecuteAsync(CreatePackage(), "C:\\Games\\Wrong"));

        Assert.AreEqual(InstallationErrorCode.Validation, exception.ErrorCode);
        Assert.IsNull(repository.LastSaved);
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
            [],
            []),
        "C:\\Packages\\launcher-package.json",
        new string('a', 64));

    private sealed class MatchingDetector(bool isMatch) : IGameInstallationDetector
    {
        public Task<bool> IsMatchAsync(
            string gameRoot,
            GameInstallDetection detection,
            CancellationToken cancellationToken = default) => Task.FromResult(isMatch);
    }

    private sealed class InMemorySettingsRepository(LauncherSettings settings) : ILauncherSettingsRepository
    {
        private readonly LauncherSettings settings = settings;

        public LauncherSettings? LastSaved { get; private set; }

        public Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(LastSaved ?? settings);

        public Task SaveAsync(LauncherSettings newSettings, CancellationToken cancellationToken = default)
        {
            LastSaved = newSettings;
            return Task.CompletedTask;
        }
    }
}
