using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Domain.Settings;
using GameTranslationLauncher.Infrastructure.FileSystem;

namespace GameTranslationLauncher.Infrastructure.Tests;

[TestClass]
public sealed class InstallationEngineTests
{
    private const string TranslationDestination = "Game/Mods/vi.pak";

    [TestMethod]
    public async Task InstallAsync_CleanGame_InstallsAndWritesReceipt()
    {
        using var context = new InstallationEngineTestContext();
        var content = "translation-v1"u8.ToArray();
        var package = context.CreatePackage(
            "1.0.0",
            new TestPackageFile("payload/vi.pak", TranslationDestination, content));

        var result = await context.Install.ExecuteAsync(context.CreateRequest(package));
        var receipt = await context.ReceiptRepository.FindAsync(
            context.Installation.GameId,
            context.Installation.InstallationId);

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(content, File.ReadAllBytes(context.GetGamePath(TranslationDestination)));
        Assert.IsNotNull(receipt);
        Assert.AreEqual("1.0.0", receipt.PackageVersion);
        Assert.AreEqual(InstalledFileOwnership.Created, receipt.Files.Single().Ownership);
    }

    [TestMethod]
    public async Task InstallAsync_WrongGameRoot_LeavesGameUnchanged()
    {
        using var context = new InstallationEngineTestContext();
        var package = context.CreatePackage(
            "1.0.0",
            new TestPackageFile("payload/vi.pak", TranslationDestination, "translation"u8.ToArray()));
        var wrongRoot = context.TemporaryDirectory.GetPath("wrong-game");
        Directory.CreateDirectory(wrongRoot);
        var wrongInstallation = new GameInstallationSetting(
            context.Installation.GameId,
            context.Installation.InstallationId,
            wrongRoot,
            DateTimeOffset.UtcNow);

        var result = await context.Install.ExecuteAsync(
            context.CreateRequest(package, installation: wrongInstallation));

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(InstallationErrorCode.Validation, result.ErrorCode);
        Assert.IsFalse(File.Exists(Path.Combine(wrongRoot, "Game", "Mods", "vi.pak")));
        Assert.IsNull(await context.ReceiptRepository.FindAsync(
            context.Installation.GameId,
            context.Installation.InstallationId));
    }

    [TestMethod]
    public async Task InstallAsync_PackageHashChanged_LeavesGameUnchanged()
    {
        using var context = new InstallationEngineTestContext();
        var package = context.CreatePackage(
            "1.0.0",
            new TestPackageFile("payload/vi.pak", TranslationDestination, "expected"u8.ToArray()));
        File.WriteAllText(Path.Combine(context.PackageRoot, "payload", "vi.pak"), "corrupt!");

        var result = await context.Install.ExecuteAsync(context.CreateRequest(package));

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(InstallationErrorCode.PackageCorrupted, result.ErrorCode);
        Assert.IsFalse(File.Exists(context.GetGamePath(TranslationDestination)));
    }

    [TestMethod]
    public async Task InstallAsync_SameVersionTwice_IsIdempotent()
    {
        using var context = new InstallationEngineTestContext();
        var package = context.CreatePackage(
            "1.0.0",
            new TestPackageFile("payload/vi.pak", TranslationDestination, "translation"u8.ToArray()));
        var first = await context.Install.ExecuteAsync(context.CreateRequest(package));

        var second = await context.Install.ExecuteAsync(context.CreateRequest(package));

        Assert.IsTrue(first.Succeeded);
        Assert.IsTrue(second.Succeeded);
        Assert.AreEqual(first.Receipt?.CompletedAtUtc, second.Receipt?.CompletedAtUtc);
        Assert.HasCount(1, Directory.GetFiles(context.GameRoot, "vi.pak", SearchOption.AllDirectories));
    }

    [TestMethod]
    public async Task UpdateAsync_OlderReceipt_ReplacesOnlyOwnedFile()
    {
        using var context = new InstallationEngineTestContext();
        var v1 = context.CreatePackage(
            "1.0.0",
            new TestPackageFile("payload/vi.pak", TranslationDestination, "v1"u8.ToArray()));
        Assert.IsTrue((await context.Install.ExecuteAsync(context.CreateRequest(v1))).Succeeded);
        var v2Content = "v2-new-content"u8.ToArray();
        var v2 = context.CreatePackage(
            "2.0.0",
            new TestPackageFile("payload/vi.pak", TranslationDestination, v2Content));

        var result = await context.Update.ExecuteAsync(context.CreateRequest(v2));

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(v2Content, File.ReadAllBytes(context.GetGamePath(TranslationDestination)));
        Assert.AreEqual("2.0.0", result.Receipt?.PackageVersion);
    }

    [TestMethod]
    public async Task UpdateAsync_FileRemovedFromPackage_DeletesOnlyThatOwnedFile()
    {
        using var context = new InstallationEngineTestContext();
        var keptDestination = "Game/Mods/kept.pak";
        var removedDestination = "Game/Mods/removed.pak";
        var v1 = context.CreatePackage(
            "1.0.0",
            new TestPackageFile("payload/kept.pak", keptDestination, "kept-v1"u8.ToArray()),
            new TestPackageFile("payload/removed.pak", removedDestination, "removed-v1"u8.ToArray()));
        Assert.IsTrue((await context.Install.ExecuteAsync(context.CreateRequest(v1))).Succeeded);
        var v2 = context.CreatePackage(
            "2.0.0",
            new TestPackageFile("payload/kept.pak", keptDestination, "kept-v2"u8.ToArray()));

        var result = await context.Update.ExecuteAsync(context.CreateRequest(v2));

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(File.Exists(context.GetGamePath(keptDestination)));
        Assert.IsFalse(File.Exists(context.GetGamePath(removedDestination)));
        Assert.HasCount(1, result.Receipt!.Files);
        Assert.AreEqual(keptDestination, result.Receipt.Files.Single().Destination);
    }

    [TestMethod]
    public async Task InstallAsync_CancelledBeforeValidation_LeavesGameUnchanged()
    {
        using var context = new InstallationEngineTestContext();
        var package = context.CreatePackage(
            "1.0.0",
            new TestPackageFile("payload/vi.pak", TranslationDestination, "translation"u8.ToArray()));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await context.Install.ExecuteAsync(
            context.CreateRequest(package),
            cancellationToken: cancellation.Token);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(InstallationErrorCode.Cancelled, result.ErrorCode);
        Assert.IsFalse(File.Exists(context.GetGamePath(TranslationDestination)));
    }

    [TestMethod]
    public async Task UpdateAsync_SecondCommitFails_RollsBackFirstFile()
    {
        using var context = new InstallationEngineTestContext();
        var firstDestination = "Game/Mods/first.pak";
        var secondDestination = "Game/Mods/second.pak";
        var v1 = context.CreatePackage(
            "1.0.0",
            new TestPackageFile("payload/first.pak", firstDestination, "first-v1"u8.ToArray()),
            new TestPackageFile("payload/second.pak", secondDestination, "second-v1"u8.ToArray()));
        Assert.IsTrue((await context.Install.ExecuteAsync(context.CreateRequest(v1))).Succeeded);
        var v2 = context.CreatePackage(
            "2.0.0",
            new TestPackageFile("payload/first.pak", firstDestination, "first-v2"u8.ToArray()),
            new TestPackageFile("payload/second.pak", secondDestination, "second-v2"u8.ToArray()));
        using var progress = new LockSecondFileAfterFirstCommitProgress(
            context.GetGamePath(secondDestination));

        var result = await context.Update.ExecuteAsync(context.CreateRequest(v2), progress);
        progress.Dispose();

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(InstallationErrorCode.FileInUse, result.ErrorCode);
        Assert.AreEqual("first-v1", File.ReadAllText(context.GetGamePath(firstDestination)));
        Assert.AreEqual("second-v1", File.ReadAllText(context.GetGamePath(secondDestination)));
        var receipt = await context.ReceiptRepository.FindAsync(
            context.Installation.GameId,
            context.Installation.InstallationId);
        Assert.AreEqual("1.0.0", receipt?.PackageVersion);
    }

    [TestMethod]
    public async Task UninstallAsync_ReplacedFile_RestoresOriginalAndDeletesReceipt()
    {
        using var context = new InstallationEngineTestContext();
        var original = "original-game-file"u8.ToArray();
        var translated = "translated-file"u8.ToArray();
        context.WriteGameFile(TranslationDestination, original);
        var package = context.CreatePackage(
            "1.0.0",
            new TestPackageFile("payload/vi.pak", TranslationDestination, translated));
        var install = await context.Install.ExecuteAsync(
            context.CreateRequest(package, approvedReplacements: [TranslationDestination]));
        Assert.IsTrue(install.Succeeded);
        Assert.AreEqual(InstalledFileOwnership.Replaced, install.Receipt?.Files.Single().Ownership);

        var uninstall = await context.Uninstall.ExecuteAsync(
            context.Installation.GameId,
            context.Installation.InstallationId);

        Assert.IsTrue(uninstall.Succeeded);
        CollectionAssert.AreEqual(original, File.ReadAllBytes(context.GetGamePath(TranslationDestination)));
        Assert.IsNull(await context.ReceiptRepository.FindAsync(
            context.Installation.GameId,
            context.Installation.InstallationId));
    }

    [TestMethod]
    public async Task UninstallAsync_ReplacedFileIsMissing_StillRestoresVerifiedBackup()
    {
        using var context = new InstallationEngineTestContext();
        var original = "original-game-file"u8.ToArray();
        context.WriteGameFile(TranslationDestination, original);
        var package = context.CreatePackage(
            "1.0.0",
            new TestPackageFile("payload/vi.pak", TranslationDestination, "translated"u8.ToArray()));
        var install = await context.Install.ExecuteAsync(
            context.CreateRequest(package, approvedReplacements: [TranslationDestination]));
        Assert.IsTrue(install.Succeeded);
        File.Delete(context.GetGamePath(TranslationDestination));

        var uninstall = await context.Uninstall.ExecuteAsync(
            context.Installation.GameId,
            context.Installation.InstallationId);

        Assert.IsTrue(uninstall.Succeeded);
        CollectionAssert.AreEqual(original, File.ReadAllBytes(context.GetGamePath(TranslationDestination)));
    }

    [TestMethod]
    public async Task UninstallAsync_OwnedFileWasModified_ReturnsConflictWithoutDeleting()
    {
        using var context = new InstallationEngineTestContext();
        var package = context.CreatePackage(
            "1.0.0",
            new TestPackageFile("payload/vi.pak", TranslationDestination, "translation"u8.ToArray()));
        Assert.IsTrue((await context.Install.ExecuteAsync(context.CreateRequest(package))).Succeeded);
        File.WriteAllText(context.GetGamePath(TranslationDestination), "user-modified");

        var result = await context.Uninstall.ExecuteAsync(
            context.Installation.GameId,
            context.Installation.InstallationId);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(InstallationErrorCode.Conflict, result.ErrorCode);
        Assert.AreEqual("user-modified", File.ReadAllText(context.GetGamePath(TranslationDestination)));
        Assert.IsNotNull(await context.ReceiptRepository.FindAsync(
            context.Installation.GameId,
            context.Installation.InstallationId));
    }

    [TestMethod]
    public async Task UninstallAsync_PreexistingPrerequisite_LeavesItUntouched()
    {
        using var context = new InstallationEngineTestContext();
        var prerequisiteDestination = "Game/Binaries/helper.dll";
        var prerequisite = "existing-prerequisite"u8.ToArray();
        context.WriteGameFile(prerequisiteDestination, prerequisite);
        var package = context.CreatePackage(
            "1.0.0",
            new TestPackageFile("payload/vi.pak", TranslationDestination, "translation"u8.ToArray()),
            new TestPackageFile(
                "prerequisites/helper.dll",
                prerequisiteDestination,
                prerequisite,
                PackageFileRole.Prerequisite,
                "signature-helper",
                RequiresConsent: true));
        var install = await context.Install.ExecuteAsync(
            context.CreateRequest(package, acceptedPrerequisites: ["signature-helper"]));
        Assert.IsTrue(install.Succeeded);
        Assert.AreEqual(
            InstalledFileOwnership.Preserved,
            install.Receipt?.Files.Single(file => file.Role == PackageFileRole.Prerequisite).Ownership);

        var uninstall = await context.Uninstall.ExecuteAsync(
            context.Installation.GameId,
            context.Installation.InstallationId);

        Assert.IsTrue(uninstall.Succeeded);
        CollectionAssert.AreEqual(prerequisite, File.ReadAllBytes(context.GetGamePath(prerequisiteDestination)));
        Assert.IsFalse(File.Exists(context.GetGamePath(TranslationDestination)));

        var statusUseCase = new DetectInstallationStatusUseCase(
            new FileSystemGameInstallationDetector(),
            context.ReceiptRepository,
            new Sha256InstalledFileStateReader());
        var status = await statusUseCase.ExecuteAsync(package, context.Installation);
        Assert.AreEqual(InstallationStatus.NotInstalled, status);
    }

    private sealed class LockSecondFileAfterFirstCommitProgress(string path)
        : IProgress<InstallationProgress>, IDisposable
    {
        private FileStream? lockStream;

        public void Report(InstallationProgress value)
        {
            if (lockStream is null
                && value.Stage == InstallationProgressStage.Installing
                && value.CompletedItems == 1)
            {
                lockStream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
        }

        public void Dispose()
        {
            lockStream?.Dispose();
        }
    }
}
