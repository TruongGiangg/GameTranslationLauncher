using System.ComponentModel;
using System.Diagnostics;

namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Kết quả chạy bộ cài: phân biệt lỗi khi khởi chạy tiến trình bộ cài
/// và trường hợp người dùng thoát/hủy wizard trước khi cài xong.
/// </summary>
public enum LauncherInstallOutcome
{
    Success,
    LaunchFailed,
    InstallFailed
}

/// <summary>
/// Khởi chạy bộ cài đã tải về (đầy đủ wizard — chế độ rút gọn UI của Advanced Installer
/// bị lỗi race condition khi tự giải nén payload lớn, xem lịch sử debug). Đợi wizard đóng
/// rồi tự khởi chạy lại Launcher mới, để người dùng không phải tự mở lại app sau khi cài.
/// </summary>
public sealed class LauncherUpdateInstaller
{
    private const int ExitCodeSuccess = 0;
    private const int ExitCodeSuccessRebootRequired = 3010;

    public async Task<LauncherInstallOutcome> InstallAndRelaunchAsync(
        string installerPath,
        CancellationToken cancellationToken = default)
    {
        var currentExePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentExePath))
        {
            return LauncherInstallOutcome.LaunchFailed;
        }

        Process? process;
        try
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            });
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            return LauncherInstallOutcome.LaunchFailed;
        }

        if (process is null)
        {
            return LauncherInstallOutcome.LaunchFailed;
        }

        using (process)
        {
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != ExitCodeSuccess && process.ExitCode != ExitCodeSuccessRebootRequired)
            {
                return LauncherInstallOutcome.InstallFailed;
            }
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = currentExePath,
                UseShellExecute = true
            });
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            // Cài đặt đã thành công; không tự mở lại được không phải lỗi cài đặt.
        }

        return LauncherInstallOutcome.Success;
    }
}
