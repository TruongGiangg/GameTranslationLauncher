namespace GameTranslationLauncher.Wpf.ViewModels;

/// <summary>
/// Chuỗi hiển thị của banner cập nhật Launcher theo ngôn ngữ presentation đã chọn.
/// </summary>
public sealed record LauncherUpdateText(
    string UpdateAvailableFormat,
    string DownloadAndInstallLabel,
    string MandatoryNoticeLabel,
    string DownloadFailed,
    string LaunchFailed,
    string IntegrityFailed);
