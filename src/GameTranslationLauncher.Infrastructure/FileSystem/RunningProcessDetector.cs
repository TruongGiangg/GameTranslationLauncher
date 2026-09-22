using System.Diagnostics;

namespace GameTranslationLauncher.Infrastructure.FileSystem;

internal static class RunningProcessDetector
{
    public static bool IsAnyRunning(IEnumerable<string> processNames)
    {
        foreach (var configuredName in processNames)
        {
            var processName = Path.GetFileNameWithoutExtension(configuredName);
            var processes = Process.GetProcessesByName(processName);
            try
            {
                if (processes.Length > 0)
                {
                    return true;
                }
            }
            finally
            {
                foreach (var process in processes)
                {
                    process.Dispose();
                }
            }
        }

        return false;
    }
}
