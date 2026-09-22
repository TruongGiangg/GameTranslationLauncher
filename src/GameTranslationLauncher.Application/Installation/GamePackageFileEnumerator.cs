using GameTranslationLauncher.Domain.Packages;

namespace GameTranslationLauncher.Application.Installation;

internal static class GamePackageFileEnumerator
{
    public static IEnumerable<GamePackageFile> Enumerate(GamePackage package)
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
}
