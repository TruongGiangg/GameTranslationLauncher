using GameTranslationLauncher.Infrastructure.Serialization;

namespace GameTranslationLauncher.Infrastructure.FileSystem;

internal static class SafePackagePathResolver
{
    public static string Resolve(string manifestPath, string source)
    {
        var fullManifestPath = Path.GetFullPath(manifestPath);
        var packageRoot = Path.GetDirectoryName(fullManifestPath)
            ?? throw new ArgumentException("Manifest không có thư mục cha.", nameof(manifestPath));
        if (!ContractValueValidator.IsSafeRelativePath(source))
        {
            throw new ArgumentException("Source package không hợp lệ.", nameof(source));
        }

        var relativePath = source.Replace('/', Path.DirectorySeparatorChar);
        var sourcePath = Path.GetFullPath(Path.Combine(packageRoot, relativePath));
        var rootPrefix = Path.TrimEndingDirectorySeparator(packageRoot) + Path.DirectorySeparatorChar;
        if (!sourcePath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Source package vượt package root.", nameof(source));
        }

        return sourcePath;
    }
}
