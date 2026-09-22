using System.Security.Cryptography;
using GameTranslationLauncher.Application.Catalog;

namespace GameTranslationLauncher.Infrastructure.Catalog;

/// <summary>
/// Quét đúng cấu trúc games/&lt;slug&gt;/dist/launcher/launcher-package.json của catalog local.
/// </summary>
public sealed class LocalGameCatalogRepository : IGameCatalogRepository
{
    private readonly string gamesRoot;
    private readonly IGamePackageRepository packageRepository;

    public LocalGameCatalogRepository(string gamesRoot, IGamePackageRepository packageRepository)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gamesRoot);
        this.gamesRoot = Path.GetFullPath(gamesRoot);
        this.packageRepository = packageRepository;
    }

    public async Task<GameCatalogSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(gamesRoot))
        {
            return new GameCatalogSnapshot([], []);
        }

        var packages = new List<AvailableGamePackage>();
        var errors = new List<GameCatalogError>();
        var gameIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var gameDirectory in Directory.EnumerateDirectories(gamesRoot).Order())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var manifestPath = Path.Combine(
                gameDirectory,
                "dist",
                "launcher",
                "launcher-package.json");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            await TryAddPackageAsync(manifestPath, packages, errors, gameIds, cancellationToken);
        }

        return new GameCatalogSnapshot(
            packages
                .OrderBy(item => item.Package.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(item => item.Package.GameId, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            errors.ToArray());
    }

    private async Task TryAddPackageAsync(
        string manifestPath,
        List<AvailableGamePackage> packages,
        List<GameCatalogError> errors,
        HashSet<string> gameIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var package = await packageRepository.LoadAsync(manifestPath, cancellationToken);
            if (!gameIds.Add(package.GameId))
            {
                errors.Add(new GameCatalogError(
                    manifestPath,
                    $"Game ID bị trùng trong catalog: '{package.GameId}'"));
                return;
            }

            var manifestSha256 = await ComputeSha256Async(manifestPath, cancellationToken);
            packages.Add(new AvailableGamePackage(package, manifestPath, manifestSha256));
        }
        catch (GamePackageContractException exception)
        {
            errors.Add(new GameCatalogError(manifestPath, exception.Message));
        }
        catch (IOException exception)
        {
            errors.Add(new GameCatalogError(manifestPath, exception.Message));
        }
        catch (UnauthorizedAccessException exception)
        {
            errors.Add(new GameCatalogError(manifestPath, exception.Message));
        }
    }

    private static async Task<string> ComputeSha256Async(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hash);
    }
}
