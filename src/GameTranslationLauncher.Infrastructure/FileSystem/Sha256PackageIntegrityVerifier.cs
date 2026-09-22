using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Packages;

namespace GameTranslationLauncher.Infrastructure.FileSystem;

/// <summary>
/// Xác minh manifest và toàn bộ file nguồn trước khi thư mục game bị thay đổi.
/// </summary>
public sealed class Sha256PackageIntegrityVerifier : IPackageIntegrityVerifier
{
    public async Task VerifyAsync(
        AvailableGamePackage availablePackage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(availablePackage);
        try
        {
            await VerifyManifestAsync(availablePackage, cancellationToken);
            foreach (var file in EnumerateFiles(availablePackage.Package))
            {
                await VerifyFileAsync(availablePackage.ManifestPath, file, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InstallationOperationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.PackageCorrupted,
                "Không thể đọc đầy đủ package để xác minh.",
                exception);
        }
    }

    private static async Task VerifyManifestAsync(
        AvailableGamePackage availablePackage,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(availablePackage.ManifestPath))
        {
            throw Corrupted("Manifest package không còn tồn tại.");
        }

        var hash = await Sha256File.ComputeAsync(availablePackage.ManifestPath, cancellationToken);
        if (!string.Equals(hash, availablePackage.ManifestSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw Corrupted("Hash manifest package không khớp catalog đã tải.");
        }
    }

    private static async Task VerifyFileAsync(
        string manifestPath,
        GamePackageFile file,
        CancellationToken cancellationToken)
    {
        var sourcePath = SafePackagePathResolver.Resolve(manifestPath, file.Source);
        var fileInfo = new FileInfo(sourcePath);
        if (!fileInfo.Exists || fileInfo.Length != file.SizeBytes)
        {
            throw Corrupted($"File nguồn '{file.Source}' bị thiếu hoặc sai kích thước.");
        }

        var hash = await Sha256File.ComputeAsync(sourcePath, cancellationToken);
        if (!string.Equals(hash, file.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw Corrupted($"Hash file nguồn '{file.Source}' không khớp manifest.");
        }
    }

    private static IEnumerable<GamePackageFile> EnumerateFiles(GamePackage package)
    {
        foreach (var file in package.PayloadFiles)
        {
            yield return file;
        }

        foreach (var prerequisite in package.Prerequisites)
        {
            foreach (var file in prerequisite.Files)
            {
                yield return file;
            }
        }
    }

    private static InstallationOperationException Corrupted(string message) =>
        new(InstallationErrorCode.PackageCorrupted, message);
}
