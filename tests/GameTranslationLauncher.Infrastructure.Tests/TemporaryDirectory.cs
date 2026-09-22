namespace GameTranslationLauncher.Infrastructure.Tests;

internal sealed class TemporaryDirectory : IDisposable
{
    private static readonly string TestRoot = Path.GetFullPath(Path.Combine(
        Path.GetTempPath(),
        "GameTranslationLauncher.Tests"));

    public TemporaryDirectory()
    {
        RootPath = Path.Combine(TestRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public string GetPath(params string[] segments)
    {
        return segments.Aggregate(RootPath, Path.Combine);
    }

    public void Dispose()
    {
        var fullPath = Path.GetFullPath(RootPath);
        var expectedPrefix = Path.TrimEndingDirectorySeparator(TestRoot)
            + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Từ chối xóa test directory ngoài test root.");
        }

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
        }
    }
}
