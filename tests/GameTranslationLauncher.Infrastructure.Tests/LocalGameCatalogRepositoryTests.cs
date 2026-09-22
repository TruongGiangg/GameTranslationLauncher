using GameTranslationLauncher.Infrastructure.Catalog;

namespace GameTranslationLauncher.Infrastructure.Tests;

[TestClass]
public sealed class LocalGameCatalogRepositoryTests
{
    [TestMethod]
    public async Task LoadAsync_EmptyGamesRoot_ReturnsEmptyCatalog()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var repository = CreateRepository(temporaryDirectory.RootPath);

        var result = await repository.LoadAsync();

        Assert.IsEmpty(result.Packages);
        Assert.IsEmpty(result.Errors);
    }

    [TestMethod]
    public async Task LoadAsync_ValidAndInvalidManifests_ReturnsPackageAndIsolatedError()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        AddManifest(temporaryDirectory, "valid-game", File.ReadAllText(TestRepositoryPaths.TinyEdenManifestPath));
        AddManifest(temporaryDirectory, "invalid-game", "{}");
        var repository = CreateRepository(temporaryDirectory.RootPath);

        var result = await repository.LoadAsync();

        Assert.HasCount(1, result.Packages);
        Assert.HasCount(1, result.Errors);
        Assert.AreEqual("tiny-eden", result.Packages[0].Package.GameId);
        StringAssert.Contains(result.Errors[0].ManifestPath, "invalid-game");
    }

    private static LocalGameCatalogRepository CreateRepository(string gamesRoot)
    {
        return new LocalGameCatalogRepository(gamesRoot, new JsonGamePackageRepository());
    }

    private static void AddManifest(
        TemporaryDirectory temporaryDirectory,
        string gameFolderName,
        string content)
    {
        var manifestDirectory = temporaryDirectory.GetPath(gameFolderName, "dist", "launcher");
        Directory.CreateDirectory(manifestDirectory);
        File.WriteAllText(Path.Combine(manifestDirectory, "launcher-package.json"), content);
    }
}
