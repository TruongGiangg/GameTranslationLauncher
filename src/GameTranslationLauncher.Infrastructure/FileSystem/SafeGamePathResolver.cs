using GameTranslationLauncher.Infrastructure.Serialization;

namespace GameTranslationLauncher.Infrastructure.FileSystem;

internal static class SafeGamePathResolver
{
    public static string Resolve(string gameRoot, string relativePath)
    {
        if (!ContractValueValidator.IsAbsolutePath(gameRoot))
        {
            throw new ArgumentException("Game root phải là đường dẫn tuyệt đối.", nameof(gameRoot));
        }

        if (!ContractValueValidator.IsSafeRelativePath(relativePath))
        {
            throw new ArgumentException("Đường dẫn con không hợp lệ.", nameof(relativePath));
        }

        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(gameRoot));
        var platformRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var resolvedPath = Path.GetFullPath(Path.Combine(normalizedRoot, platformRelativePath));
        var rootPrefix = normalizedRoot + Path.DirectorySeparatorChar;

        if (!resolvedPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Đường dẫn đã resolve nằm ngoài game root.");
        }

        return resolvedPath;
    }
}
