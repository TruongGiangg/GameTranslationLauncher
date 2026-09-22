using GameTranslationLauncher.Application.Updates;
using GameTranslationLauncher.Domain.Updates;
using GameTranslationLauncher.Infrastructure.FileSystem;
using GameTranslationLauncher.Infrastructure.Persistence;

namespace GameTranslationLauncher.Infrastructure.Updates;

/// <summary>
/// Tải file cài đặt của một bản phát hành về <see cref="LauncherStoragePaths.UpdatesDirectory"/>
/// và xác minh SHA-256 nếu bản phát hành có công bố checksum.
/// </summary>
public sealed class HttpUpdatePackageDownloader : IUpdatePackageDownloader
{
    private static readonly HttpClient HttpClient = new();

    private readonly LauncherStoragePaths storagePaths;

    public HttpUpdatePackageDownloader(LauncherStoragePaths storagePaths)
    {
        this.storagePaths = storagePaths ?? throw new ArgumentNullException(nameof(storagePaths));
    }

    public async Task<string> DownloadAsync(
        LauncherReleaseInfo release,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(release);

        Directory.CreateDirectory(storagePaths.UpdatesDirectory);
        var fileName = release.DownloadUrl.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
        var destinationPath = Path.Combine(storagePaths.UpdatesDirectory, fileName);

        using (var response = await HttpClient.GetAsync(
            release.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            var totalBytes = response.Content.Headers.ContentLength;

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.Asynchronous);

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;
                if (totalBytes is > 0)
                {
                    progress?.Report((int)Math.Clamp(totalRead * 100L / totalBytes.Value, 0, 100));
                }
            }
        }

        if (release.Sha256 is { Length: > 0 } expectedHash)
        {
            var actualHash = await Sha256File.ComputeAsync(destinationPath, cancellationToken);
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(destinationPath);
                throw new UpdatePackageIntegrityException(
                    "File cập nhật tải về không khớp checksum công bố trên bản phát hành.");
            }
        }

        progress?.Report(100);
        return destinationPath;
    }
}
