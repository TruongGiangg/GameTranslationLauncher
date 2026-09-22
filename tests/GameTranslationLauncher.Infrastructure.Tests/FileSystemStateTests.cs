using System.Security.Cryptography;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Infrastructure.FileSystem;

namespace GameTranslationLauncher.Infrastructure.Tests;

[TestClass]
public sealed class FileSystemStateTests
{
    [TestMethod]
    public async Task IsMatchAsync_AllMarkersExist_ReturnsTrue()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        Directory.CreateDirectory(temporaryDirectory.GetPath("Game", "Content"));
        File.WriteAllText(temporaryDirectory.GetPath("Game", "Game.exe"), "marker");
        var detection = new GameInstallDetection(
            [
                new GameDetectionMarker("Game/Game.exe", GameDetectionMarkerKind.File),
                new GameDetectionMarker("Game/Content", GameDetectionMarkerKind.Directory)
            ],
            []);
        var detector = new FileSystemGameInstallationDetector();

        var result = await detector.IsMatchAsync(temporaryDirectory.RootPath, detection);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ReadAsync_FileExists_ReturnsSizeAndSha256()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var bytes = "nội dung kiểm tra"u8.ToArray();
        var filePath = temporaryDirectory.GetPath("Game", "Mods", "vi.pak");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, bytes);
        var reader = new Sha256InstalledFileStateReader();

        var result = await reader.ReadAsync(
            temporaryDirectory.RootPath,
            "Game/Mods/vi.pak");

        Assert.IsTrue(result.Exists);
        Assert.AreEqual(bytes.Length, result.SizeBytes);
        Assert.AreEqual(Convert.ToHexStringLower(SHA256.HashData(bytes)), result.Sha256);
    }
}
