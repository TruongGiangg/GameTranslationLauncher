using System.Diagnostics;

namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Khởi chạy bộ cài đã tải về rồi tắt tiến trình Launcher hiện tại, để bộ cài (đã cấu hình
/// Major Upgrade trong Advanced Installer) tự gỡ bản cũ và cài đè bản mới.
/// </summary>
public sealed class LauncherUpdateInstaller
{
    public bool TryLaunch(string installerPath, out string errorMessage)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            });
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            errorMessage = "Windows không thể khởi chạy bộ cài đặt vừa tải về.";
            return false;
        }
    }
}
