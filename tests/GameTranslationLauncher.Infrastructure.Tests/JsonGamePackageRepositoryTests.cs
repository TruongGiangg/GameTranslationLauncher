using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Infrastructure.Catalog;

namespace GameTranslationLauncher.Infrastructure.Tests;

[TestClass]
public sealed class JsonGamePackageRepositoryTests
{
    private readonly JsonGamePackageRepository repository = new();

    [TestMethod]
    public async Task LoadAsync_ValidTinyEdenManifest_ReturnsExpectedPackage()
    {
        var package = await repository.LoadAsync(TestRepositoryPaths.TinyEdenManifestPath);

        Assert.AreEqual("tiny-eden", package.GameId);
        Assert.AreEqual("1.0.0", package.PackageVersion);
        Assert.HasCount(3, package.PayloadFiles);
        Assert.HasCount(1, package.Prerequisites);
        Assert.HasCount(2, package.Prerequisites[0].Files);
        Assert.AreEqual(PackageFileRole.Prerequisite, package.Prerequisites[0].Files[0].Role);
    }

    [TestMethod]
    public async Task LoadAsync_MissingRequiredProperty_ThrowsContractException()
    {
        using var manifest = TemporaryManifest.Create(json => json.Remove("displayName"));

        var exception = await Assert.ThrowsAsync<GamePackageContractException>(
            () => repository.LoadAsync(manifest.ManifestPath));

        StringAssert.Contains(exception.Message, "displayName");
    }

    [TestMethod]
    public async Task LoadAsync_DuplicateDestinationIgnoringCase_ThrowsContractException()
    {
        using var manifest = TemporaryManifest.Create(json =>
        {
            var files = json["payloadFiles"]!.AsArray();
            var firstDestination = files[0]!["destination"]!.GetValue<string>();
            files[1]!["destination"] = firstDestination.ToUpperInvariant();
        });

        var exception = await Assert.ThrowsAsync<GamePackageContractException>(
            () => repository.LoadAsync(manifest.ManifestPath));

        StringAssert.Contains(exception.Message, "Destination bị trùng");
    }

    [TestMethod]
    public async Task LoadAsync_DestinationContainsParentSegment_ThrowsContractException()
    {
        using var manifest = TemporaryManifest.Create(json =>
        {
            json["payloadFiles"]![0]!["destination"] = "../outside-game.pak";
        });

        var exception = await Assert.ThrowsAsync<GamePackageContractException>(
            () => repository.LoadAsync(manifest.ManifestPath));

        StringAssert.Contains(exception.Message, "Đường dẫn tương đối không hợp lệ");
    }

    [TestMethod]
    public async Task LoadAsync_SourceContainsParentSegment_ThrowsContractException()
    {
        using var manifest = TemporaryManifest.Create(json =>
        {
            json["payloadFiles"]![0]!["source"] = "../outside-package.pak";
        });

        var exception = await Assert.ThrowsAsync<GamePackageContractException>(
            () => repository.LoadAsync(manifest.ManifestPath));

        StringAssert.Contains(exception.Message, "Đường dẫn tương đối không hợp lệ");
    }

    [TestMethod]
    public async Task LoadAsync_DestinationIsAbsolute_ThrowsContractException()
    {
        using var manifest = TemporaryManifest.Create(json =>
        {
            json["payloadFiles"]![0]!["destination"] = "C:/outside-game.pak";
        });

        var exception = await Assert.ThrowsAsync<GamePackageContractException>(
            () => repository.LoadAsync(manifest.ManifestPath));

        StringAssert.Contains(exception.Message, "Đường dẫn tương đối không hợp lệ");
    }
}
