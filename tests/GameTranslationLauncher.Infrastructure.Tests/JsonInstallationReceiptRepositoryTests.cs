using System.Text.Json.Nodes;
using GameTranslationLauncher.Application.State;
using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Infrastructure.Persistence;

namespace GameTranslationLauncher.Infrastructure.Tests;

[TestClass]
public sealed class JsonInstallationReceiptRepositoryTests
{
    [TestMethod]
    public async Task FindAsync_ReceiptDoesNotExist_ReturnsNull()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var repository = CreateRepository(temporaryDirectory);

        var receipt = await repository.FindAsync("tiny-eden", Guid.NewGuid());

        Assert.IsNull(receipt);
    }

    [TestMethod]
    public async Task SaveAndFindAsync_ValidReceipt_RoundTrips()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var repository = CreateRepository(temporaryDirectory);
        var expected = CreateReceipt();

        await repository.SaveAsync(expected);
        var actual = await repository.FindAsync(expected.GameId, expected.InstallationId);

        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.InstallationId, actual.InstallationId);
        Assert.AreEqual(expected.PackageManifestSha256, actual.PackageManifestSha256);
        Assert.HasCount(1, actual.Files);
        Assert.AreEqual(InstalledFileOwnership.Created, actual.Files[0].Ownership);

        var paths = new LauncherStoragePaths(temporaryDirectory.RootPath);
        var savedJson = JsonNode.Parse(File.ReadAllText(
            paths.GetReceiptPath(expected.GameId, expected.InstallationId)))!.AsObject();
        var savedFile = savedJson["files"]![0]!.AsObject();
        Assert.IsFalse(savedFile.ContainsKey("backupRelativePath"));
        Assert.IsFalse(savedFile.ContainsKey("originalSha256"));
        Assert.IsFalse(savedFile.ContainsKey("originalSizeBytes"));
    }

    [TestMethod]
    public async Task FindAsync_UnsupportedSchemaVersion_ThrowsStateContractException()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var paths = new LauncherStoragePaths(temporaryDirectory.RootPath);
        var repository = new JsonInstallationReceiptRepository(paths);
        var receipt = CreateReceipt();
        await repository.SaveAsync(receipt);

        var receiptPath = paths.GetReceiptPath(receipt.GameId, receipt.InstallationId);
        var json = JsonNode.Parse(File.ReadAllText(receiptPath))!.AsObject();
        json["schemaVersion"] = 2;
        File.WriteAllText(receiptPath, json.ToJsonString());

        var exception = await Assert.ThrowsAsync<LauncherStateContractException>(
            () => repository.FindAsync(receipt.GameId, receipt.InstallationId));

        StringAssert.Contains(exception.Message, "schemaVersion '2'");
    }

    private static JsonInstallationReceiptRepository CreateRepository(
        TemporaryDirectory temporaryDirectory)
    {
        return new JsonInstallationReceiptRepository(
            new LauncherStoragePaths(temporaryDirectory.RootPath));
    }

    private static InstallationReceipt CreateReceipt()
    {
        return new InstallationReceipt(
            1,
            Guid.NewGuid(),
            "tiny-eden",
            "D:\\Games\\Tiny Eden",
            "1.0.0",
            new string('a', 64),
            "1.0.0",
            DateTimeOffset.Parse("2026-09-12T00:00:00Z"),
            ["CGH/Content/Paks/~mods"],
            [
                new InstalledFileReceipt(
                    "CGH/Content/Paks/~mods/VI_Translation_P.pak",
                    new string('b', 64),
                    100,
                    PackageFileRole.Translation,
                    null,
                    InstalledFileOwnership.Created,
                    null,
                    null,
                    null)
            ]);
    }
}
