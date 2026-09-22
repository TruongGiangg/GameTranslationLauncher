using System.Security.Cryptography;
using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Installation;

namespace GameTranslationLauncher.Infrastructure.FileSystem;

/// <summary>
/// Đọc trạng thái và SHA-256 của một file đích đã được giới hạn trong game root.
/// </summary>
public sealed class Sha256InstalledFileStateReader : IInstalledFileStateReader
{
    public async Task<InstalledFileState> ReadAsync(
        string gameRoot,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var filePath = SafeGamePathResolver.Resolve(gameRoot, relativePath);
        if (!File.Exists(filePath))
        {
            return InstalledFileState.Missing;
        }

        try
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var sizeBytes = stream.Length;
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);

            return new InstalledFileState(
                true,
                sizeBytes,
                Convert.ToHexStringLower(hash));
        }
        catch (FileNotFoundException)
        {
            return InstalledFileState.Missing;
        }
        catch (DirectoryNotFoundException)
        {
            return InstalledFileState.Missing;
        }
    }
}
