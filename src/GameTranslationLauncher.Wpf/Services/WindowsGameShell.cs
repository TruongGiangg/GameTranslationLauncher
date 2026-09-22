using System.Diagnostics;
using System.IO;
using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Wpf.ViewModels;

namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Mở thư mục hoặc executable được khai báo trong marker của package sau thao tác do người dùng yêu cầu.
/// </summary>
public sealed class WindowsGameShell
{
    private LauncherOperationText text = LauncherTextFactory.Create("vi").Operation;

    public void ApplyText(LauncherOperationText value)
    {
        text = value ?? throw new ArgumentNullException(nameof(value));
    }

    public bool TryOpenGameFolder(string? gameRoot, out string message)
    {
        if (string.IsNullOrWhiteSpace(gameRoot) || !Directory.Exists(gameRoot))
        {
            message = text.OpenFolderMissing;
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = gameRoot,
                UseShellExecute = true
            });
            message = text.OpenFolderSucceeded;
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            message = text.OpenFolderFailed;
            return false;
        }
    }

    public bool TryLaunchGame(GameCatalogItem game, out string message)
    {
        ArgumentNullException.ThrowIfNull(game);
        var gameRoot = game.GameRoot;
        if (string.IsNullOrWhiteSpace(gameRoot) || !Directory.Exists(gameRoot))
        {
            message = text.LaunchMissing;
            return false;
        }

        var executableMarker = game.AvailablePackage.Package.InstallDetection.RequiredPaths
            .FirstOrDefault(marker => marker.Kind == GameDetectionMarkerKind.File
                && marker.Path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        if (executableMarker is null)
        {
            message = text.LaunchExecutableNotConfigured;
            return false;
        }

        var executablePath = ResolveUnderGameRoot(gameRoot, executableMarker.Path);
        if (!File.Exists(executablePath))
        {
            message = text.LaunchExecutableNotFound;
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = Path.GetDirectoryName(executablePath),
                UseShellExecute = true
            });
            message = text.LaunchSucceeded;
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            message = text.LaunchFailed;
            return false;
        }
    }

    private string ResolveUnderGameRoot(string gameRoot, string relativePath)
    {
        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(gameRoot));
        var candidate = Path.GetFullPath(Path.Combine(
            normalizedRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = normalizedRoot + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(text.ExecutableOutsideGameRoot);
        }

        return candidate;
    }
}
