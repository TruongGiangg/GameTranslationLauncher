using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Domain.Packages;

namespace GameTranslationLauncher.Infrastructure.FileSystem;

/// <summary>
/// Xác minh marker file/directory của game bằng file system local.
/// </summary>
public sealed class FileSystemGameInstallationDetector : IGameInstallationDetector
{
    public Task<bool> IsMatchAsync(
        string gameRoot,
        GameInstallDetection detection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists(gameRoot))
        {
            return Task.FromResult(false);
        }

        foreach (var marker in detection.RequiredPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = SafeGamePathResolver.Resolve(gameRoot, marker.Path);
            var exists = marker.Kind switch
            {
                GameDetectionMarkerKind.File => File.Exists(path),
                GameDetectionMarkerKind.Directory => Directory.Exists(path),
                _ => false
            };

            if (!exists)
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }
}
