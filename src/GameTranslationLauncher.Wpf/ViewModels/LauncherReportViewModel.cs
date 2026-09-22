using System.Collections.ObjectModel;
using System.Windows.Input;
using GameTranslationLauncher.Wpf.Commands;
using GameTranslationLauncher.Wpf.Services;

namespace GameTranslationLauncher.Wpf.ViewModels;

/// <summary>
/// Quản lý form report và chỉ mở email nháp sau khi dữ liệu hợp lệ.
/// </summary>
public sealed class LauncherReportViewModel : ObservableObject
{
    private const string ReportRecipient = "giangcuathoidai@gmail.com";
    private readonly WindowsExternalUriLauncher externalUriLauncher;
    private LauncherSettingsText text;
    private string reportType = "bug";
    private ReportGameOption? selectedReportGame;
    private string requestedGameName = string.Empty;
    private string reportMessage = string.Empty;
    private string actionStatusText = string.Empty;
    private bool isActionError;

    public LauncherReportViewModel(
        WindowsExternalUriLauncher externalUriLauncher,
        LauncherSettingsText text)
    {
        this.externalUriLauncher = externalUriLauncher ?? throw new ArgumentNullException(nameof(externalUriLauncher));
        this.text = text ?? throw new ArgumentNullException(nameof(text));
        SelectBugReportCommand = new RelayCommand(() => SetReportType("bug"));
        SelectUpdateReportCommand = new RelayCommand(() => SetReportType("update"));
        SelectRequestReportCommand = new RelayCommand(() => SetReportType("request"));
        SendReportCommand = new RelayCommand(OpenReportEmail);
    }

    public ObservableCollection<ReportGameOption> AvailableGames { get; } = [];

    public ReportGameOption? SelectedReportGame
    {
        get => selectedReportGame;
        set
        {
            if (SetProperty(ref selectedReportGame, value))
            {
                OnPropertyChanged(nameof(ReportGameName));
            }
        }
    }

    public string RequestedGameName
    {
        get => requestedGameName;
        set
        {
            if (SetProperty(ref requestedGameName, value))
            {
                OnPropertyChanged(nameof(ReportGameName));
            }
        }
    }

    public string ReportGameName => IsCatalogGameSelectionRequired
        ? SelectedReportGame?.DisplayName ?? string.Empty
        : RequestedGameName;

    public string ReportMessage
    {
        get => reportMessage;
        set => SetProperty(ref reportMessage, value);
    }

    public string ActionStatusText
    {
        get => actionStatusText;
        private set => SetProperty(ref actionStatusText, value);
    }

    public bool IsActionError
    {
        get => isActionError;
        private set => SetProperty(ref isActionError, value);
    }

    public bool IsBugReportSelected => reportType == "bug";
    public bool IsUpdateReportSelected => reportType == "update";
    public bool IsRequestReportSelected => reportType == "request";
    public bool IsCatalogGameSelectionRequired => reportType is "bug" or "update";
    public bool IsCustomGameNameRequired => reportType == "request";

    public string SelectedReportTypeLabel => reportType switch
    {
        "update" => text.ReportUpdateLabel,
        "request" => text.ReportRequestLabel,
        _ => text.ReportBugLabel
    };

    public ICommand SelectBugReportCommand { get; }
    public ICommand SelectUpdateReportCommand { get; }
    public ICommand SelectRequestReportCommand { get; }
    public ICommand SendReportCommand { get; }

    public void ApplyText(LauncherSettingsText value)
    {
        text = value ?? throw new ArgumentNullException(nameof(value));
        OnPropertyChanged(nameof(SelectedReportTypeLabel));
    }

    public void SetAvailableGames(IEnumerable<ReportGameOption> games)
    {
        ArgumentNullException.ThrowIfNull(games);
        var selectedGameId = SelectedReportGame?.GameId;
        AvailableGames.Clear();
        foreach (var game in games
                     .GroupBy(game => game.GameId, StringComparer.OrdinalIgnoreCase)
                     .Select(group => group.First())
                     .OrderBy(game => game.DisplayName, StringComparer.CurrentCultureIgnoreCase))
        {
            AvailableGames.Add(game);
        }

        SelectedReportGame = AvailableGames.FirstOrDefault(game => string.Equals(
            game.GameId,
            selectedGameId,
            StringComparison.OrdinalIgnoreCase));
    }

    private void SetReportType(string value)
    {
        reportType = value;
        foreach (var propertyName in new[]
                 {
                     nameof(IsBugReportSelected), nameof(IsUpdateReportSelected), nameof(IsRequestReportSelected),
                     nameof(IsCatalogGameSelectionRequired), nameof(IsCustomGameNameRequired), nameof(SelectedReportTypeLabel),
                     nameof(ReportGameName)
                 })
        {
            OnPropertyChanged(propertyName);
        }
    }

    private void OpenReportEmail()
    {
        if (string.IsNullOrWhiteSpace(ReportGameName))
        {
            SetActionStatus(text.ReportGameRequired, isError: true);
            return;
        }

        if (string.IsNullOrWhiteSpace(ReportMessage))
        {
            SetActionStatus(text.ReportMessageRequired, isError: true);
            return;
        }

        var subject = $"[TG Launcher] {SelectedReportTypeLabel}: {ReportGameName.Trim()}";
        var body = $"{text.ReportTypeLabel}: {SelectedReportTypeLabel}\r\n"
            + $"{text.ReportGameLabel}: {ReportGameName.Trim()}\r\n\r\n"
            + $"{text.ReportMessageLabel}:\r\n{ReportMessage.Trim()}\r\n\r\n"
            + "TG Launcher V1.0.0";
        var mailto = $"mailto:{ReportRecipient}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
        if (externalUriLauncher.TryOpen(mailto, out var errorMessage))
        {
            SetActionStatus(text.ReportOpened, isError: false);
            return;
        }

        SetActionStatus(errorMessage, isError: true);
    }

    private void SetActionStatus(string message, bool isError)
    {
        ActionStatusText = message;
        IsActionError = isError;
    }
}
