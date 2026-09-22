using System.IO;
using System.Net.Http;
using System.Windows.Input;
using GameTranslationLauncher.Application.Updates;
using GameTranslationLauncher.Domain.Updates;
using GameTranslationLauncher.Wpf.Commands;
using GameTranslationLauncher.Wpf.Services;

namespace GameTranslationLauncher.Wpf.ViewModels;

/// <summary>
/// Kiểm tra và tải bản cập nhật Launcher từ GitHub Releases. Cập nhật là bắt buộc: một khi
/// phát hiện bản mới, Launcher hiện overlay chặn toàn bộ thư viện game cho tới khi người dùng
/// bấm "Tải và cài đặt" — không có lựa chọn bỏ qua.
/// </summary>
public sealed class UpdateViewModel : ObservableObject
{
    private readonly CheckForLauncherUpdateUseCase checkForUpdate;
    private readonly DownloadLauncherUpdateUseCase downloadUpdate;
    private readonly LauncherUpdateInstaller installer;
    private readonly Version currentVersion;
    private LauncherUpdateText text = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
    private LauncherReleaseInfo? availableRelease;
    private bool isUpdateAvailable;
    private bool isDownloading;
    private int downloadProgress;
    private string statusMessage = string.Empty;

    public UpdateViewModel(
        CheckForLauncherUpdateUseCase checkForUpdate,
        DownloadLauncherUpdateUseCase downloadUpdate,
        LauncherUpdateInstaller installer,
        Version currentVersion)
    {
        this.checkForUpdate = checkForUpdate ?? throw new ArgumentNullException(nameof(checkForUpdate));
        this.downloadUpdate = downloadUpdate ?? throw new ArgumentNullException(nameof(downloadUpdate));
        this.installer = installer ?? throw new ArgumentNullException(nameof(installer));
        this.currentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
        DownloadAndInstallCommand = new AsyncRelayCommand(
            DownloadAndInstallAsync,
            () => IsUpdateAvailable && !IsDownloading);
    }

    public bool IsUpdateAvailable
    {
        get => isUpdateAvailable;
        private set
        {
            if (!SetProperty(ref isUpdateAvailable, value))
            {
                return;
            }

            ((AsyncRelayCommand)DownloadAndInstallCommand).RaiseCanExecuteChanged();
        }
    }

    public bool IsDownloading
    {
        get => isDownloading;
        private set
        {
            if (!SetProperty(ref isDownloading, value))
            {
                return;
            }

            ((AsyncRelayCommand)DownloadAndInstallCommand).RaiseCanExecuteChanged();
        }
    }

    public int DownloadProgress
    {
        get => downloadProgress;
        private set => SetProperty(ref downloadProgress, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public string LatestVersionLabel => availableRelease is null
        ? string.Empty
        : string.Format(text.UpdateAvailableFormat, availableRelease.Version);

    /// <summary>
    /// Nội dung release notes lấy nguyên văn từ mô tả bản phát hành trên GitHub — liệt kê
    /// bản này sửa gì, thêm game nào, v.v. Rỗng nếu người tạo release không điền mô tả.
    /// </summary>
    public string ReleaseNotes => availableRelease?.ReleaseNotes ?? string.Empty;

    public string DownloadAndInstallLabel => text.DownloadAndInstallLabel;

    public string MandatoryNoticeLabel => text.MandatoryNoticeLabel;

    public ICommand DownloadAndInstallCommand { get; }

    public void ApplyText(LauncherUpdateText value)
    {
        text = value ?? throw new ArgumentNullException(nameof(value));
        OnPropertyChanged(nameof(LatestVersionLabel));
        OnPropertyChanged(nameof(DownloadAndInstallLabel));
        OnPropertyChanged(nameof(MandatoryNoticeLabel));
    }

    public async Task CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        var result = await checkForUpdate.ExecuteAsync(currentVersion, cancellationToken);
        if (result.Status != LauncherUpdateCheckStatus.UpdateAvailable)
        {
            return;
        }

        availableRelease = result.Release;
        OnPropertyChanged(nameof(LatestVersionLabel));
        OnPropertyChanged(nameof(ReleaseNotes));
        IsUpdateAvailable = true;
    }

    private async Task DownloadAndInstallAsync()
    {
        if (availableRelease is null)
        {
            return;
        }

        IsDownloading = true;
        DownloadProgress = 0;
        StatusMessage = string.Empty;
        try
        {
            var progress = new Progress<int>(value => DownloadProgress = value);
            var installerPath = await downloadUpdate.ExecuteAsync(availableRelease, progress);
            if (!installer.TryLaunch(installerPath, out var launchError))
            {
                StatusMessage = string.IsNullOrEmpty(launchError) ? text.LaunchFailed : launchError;
                return;
            }

            System.Windows.Application.Current.Shutdown();
        }
        catch (UpdatePackageIntegrityException)
        {
            StatusMessage = text.IntegrityFailed;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or TaskCanceledException)
        {
            StatusMessage = text.DownloadFailed;
        }
        finally
        {
            IsDownloading = false;
        }
    }
}
