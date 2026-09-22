using System.Diagnostics;

namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Mở URL web hoặc mailto bằng ứng dụng mặc định của Windows sau thao tác chủ động của người dùng.
/// </summary>
public sealed class WindowsExternalUriLauncher
{
    public bool TryOpen(string uriText, out string errorMessage)
    {
        if (!Uri.TryCreate(uriText, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("https" or "mailto"))
        {
            errorMessage = "Liên kết không hợp lệ.";
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            errorMessage = "Windows không thể mở liên kết này.";
            return false;
        }
    }
}
