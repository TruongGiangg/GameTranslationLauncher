namespace GameTranslationLauncher.Infrastructure.Tests;

internal static class TestRepositoryPaths
{
    public static string LauncherRoot { get; } = FindLauncherRoot();

    public static string RepositoryRoot { get; } = Directory.GetParent(LauncherRoot)!.FullName;

    public static string TinyEdenManifestPath { get; } = Path.Combine(
        RepositoryRoot,
        "games",
        "tiny-eden",
        "dist",
        "launcher",
        "launcher-package.json");

    private static string FindLauncherRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GameTranslationLauncher.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Không tìm thấy thư mục Launcher từ test output.");
    }
}
