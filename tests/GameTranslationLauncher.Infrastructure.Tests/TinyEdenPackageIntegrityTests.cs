using System.Security.Cryptography;
using GameTranslationLauncher.Infrastructure.Catalog;

namespace GameTranslationLauncher.Infrastructure.Tests;

[TestClass]
public sealed class TinyEdenPackageIntegrityTests
{
    [TestMethod]
    public async Task PackagedFiles_SizeAndSha256_MatchManifest()
    {
        var repository = new JsonGamePackageRepository();
        var package = await repository.LoadAsync(TestRepositoryPaths.TinyEdenManifestPath);
        var packageRoot = Path.GetDirectoryName(TestRepositoryPaths.TinyEdenManifestPath)!;
        var files = package.PayloadFiles
            .Concat(package.Prerequisites.SelectMany(prerequisite => prerequisite.Files));

        foreach (var file in files)
        {
            var sourcePath = Path.Combine(
                packageRoot,
                file.Source.Replace('/', Path.DirectorySeparatorChar));
            var fileInfo = new FileInfo(sourcePath);

            Assert.IsTrue(fileInfo.Exists, $"Thiếu package file: {file.Source}");
            Assert.AreEqual(file.SizeBytes, fileInfo.Length, $"Sai kích thước: {file.Source}");

            await using var stream = fileInfo.OpenRead();
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream)).ToLowerInvariant();
            Assert.AreEqual(file.Sha256, hash, $"Sai SHA-256: {file.Source}");
        }
    }
}
