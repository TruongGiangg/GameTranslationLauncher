using System.Collections.ObjectModel;
using System.Windows.Input;
using GameTranslationLauncher.Application.Settings;
using GameTranslationLauncher.Wpf.Commands;
using GameTranslationLauncher.Wpf.Services;

namespace GameTranslationLauncher.Wpf.ViewModels;

/// <summary>
/// Quản lý preference presentation và liên kết ngoài của modal Settings.
/// </summary>
public sealed class LauncherSettingsViewModel : ObservableObject
{
    private const string FacebookUrl = "https://www.facebook.com/ntgiang3007/";
    private const string GitHubUrl = "https://github.com/TruongGiangg/GameTranslationProject";
    private const string InstagramUrl = "https://www.instagram.com/__.n.t.g.__/";

    private readonly LoadLauncherPreferencesUseCase loadPreferencesUseCase;
    private readonly SaveLauncherPreferencesUseCase savePreferencesUseCase;
    private readonly LauncherThemeManager themeManager;
    private readonly WindowsExternalUriLauncher externalUriLauncher;
    private LauncherSettingsText text = LauncherTextFactory.Create("vi");
    private string displayLanguage = "vi";
    private string accentTheme = "crimson";
    private string actionStatusText = string.Empty;
    private bool isActionError;
    private AccentThemeOption? selectedAccentOption;

    public LauncherSettingsViewModel(
        LoadLauncherPreferencesUseCase loadPreferencesUseCase,
        SaveLauncherPreferencesUseCase savePreferencesUseCase,
        LauncherThemeManager themeManager,
        WindowsExternalUriLauncher externalUriLauncher)
    {
        this.loadPreferencesUseCase = loadPreferencesUseCase ?? throw new ArgumentNullException(nameof(loadPreferencesUseCase));
        this.savePreferencesUseCase = savePreferencesUseCase ?? throw new ArgumentNullException(nameof(savePreferencesUseCase));
        this.themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
        this.externalUriLauncher = externalUriLauncher ?? throw new ArgumentNullException(nameof(externalUriLauncher));
        ReplaceAccentOptions(Text);
        Report = new LauncherReportViewModel(externalUriLauncher, Text);

        SelectVietnameseCommand = new AsyncRelayCommand(() => SavePreferencesAsync("vi", AccentTheme));
        SelectEnglishCommand = new AsyncRelayCommand(() => SavePreferencesAsync("en", AccentTheme));
        OpenFacebookCommand = new RelayCommand(() => OpenLink(FacebookUrl));
        OpenGitHubCommand = new RelayCommand(() => OpenLink(GitHubUrl));
        OpenInstagramCommand = new RelayCommand(() => OpenLink(InstagramUrl));
    }

    public event Action<LauncherSettingsText>? PresentationChanged;

    public LauncherReportViewModel Report { get; }

    public ObservableCollection<AccentThemeOption> AccentOptions { get; } = [];

    public LauncherSettingsText Text
    {
        get => text;
        private set => SetProperty(ref text, value);
    }

    public string DisplayLanguage
    {
        get => displayLanguage;
        private set => SetProperty(ref displayLanguage, value);
    }

    public string AccentTheme
    {
        get => accentTheme;
        private set => SetProperty(ref accentTheme, value);
    }

    public AccentThemeOption? SelectedAccentOption
    {
        get => selectedAccentOption;
        set => SetProperty(ref selectedAccentOption, value);
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

    public bool IsVietnameseSelected => DisplayLanguage == "vi";
    public bool IsEnglishSelected => DisplayLanguage == "en";

    public ICommand SelectVietnameseCommand { get; }
    public ICommand SelectEnglishCommand { get; }
    public ICommand OpenFacebookCommand { get; }
    public ICommand OpenGitHubCommand { get; }
    public ICommand OpenInstagramCommand { get; }

    public async Task InitializeAsync()
    {
        try
        {
            var preferences = await loadPreferencesUseCase.ExecuteAsync();
            ApplyPresentation(preferences.DisplayLanguage, preferences.AccentTheme);
        }
        catch (Exception)
        {
            ApplyPresentation("vi", "crimson");
            SetActionStatus(Text.PreferenceSaveFailed, isError: true);
        }
    }

    public void SetAvailableGames(IEnumerable<ReportGameOption> games) => Report.SetAvailableGames(games);

    public Task SelectAccentThemeAsync(AccentThemeOption? option) =>
        option is null || string.Equals(option.ThemeId, AccentTheme, StringComparison.OrdinalIgnoreCase)
            ? Task.CompletedTask
            : SavePreferencesAsync(DisplayLanguage, option.ThemeId);

    private async Task SavePreferencesAsync(string language, string theme)
    {
        ApplyPresentation(language, theme);
        try
        {
            var preferences = await savePreferencesUseCase.ExecuteAsync(language, theme);
            ApplyPresentation(preferences.DisplayLanguage, preferences.AccentTheme);
            SetActionStatus(string.Empty, isError: false);
        }
        catch (Exception)
        {
            SetActionStatus(Text.PreferenceSaveFailed, isError: true);
        }
    }

    private void ApplyPresentation(string language, string theme)
    {
        DisplayLanguage = language;
        AccentTheme = theme;
        Text = LauncherTextFactory.Create(language);
        Report.ApplyText(Text);
        ReplaceAccentOptions(Text);
        themeManager.Apply(theme);
        RaisePresentationProperties();
        PresentationChanged?.Invoke(Text);
    }

    private void OpenLink(string url)
    {
        if (!externalUriLauncher.TryOpen(url, out var errorMessage))
        {
            SetActionStatus(errorMessage, isError: true);
        }
    }

    private void RaisePresentationProperties()
    {
        foreach (var propertyName in new[]
                 {
                     nameof(IsVietnameseSelected), nameof(IsEnglishSelected)
                 })
        {
            OnPropertyChanged(propertyName);
        }
    }

    private void ReplaceAccentOptions(LauncherSettingsText settingsText)
    {
        AccentOptions.Clear();
        AccentOptions.Add(new AccentThemeOption("crimson", settingsText.CrimsonLabel));
        AccentOptions.Add(new AccentThemeOption("blue", settingsText.BlueLabel));
        AccentOptions.Add(new AccentThemeOption("emerald", settingsText.EmeraldLabel));
        AccentOptions.Add(new AccentThemeOption("violet", settingsText.VioletLabel));
        AccentOptions.Add(new AccentThemeOption("white", settingsText.WhiteLabel));
        AccentOptions.Add(new AccentThemeOption("orange", settingsText.OrangeLabel));
        AccentOptions.Add(new AccentThemeOption("cyan", settingsText.CyanLabel));
        SelectedAccentOption = AccentOptions.FirstOrDefault(option =>
            string.Equals(option.ThemeId, AccentTheme, StringComparison.OrdinalIgnoreCase));
    }

    private void SetActionStatus(string message, bool isError)
    {
        ActionStatusText = message;
        IsActionError = isError;
    }
}
