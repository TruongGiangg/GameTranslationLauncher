using System.Windows.Controls;
using GameTranslationLauncher.Wpf.ViewModels;

namespace GameTranslationLauncher.Wpf.Views;

/// <summary>
/// Màn hình presentation cho thông tin cấu hình của Launcher.
/// </summary>
public partial class LauncherSettingsView
{
    public LauncherSettingsView()
    {
        InitializeComponent();
    }

    private async void AccentThemeSelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
    {
        if (!IsLoaded ||
            DataContext is not GameLibraryViewModel library ||
            sender is not ComboBox { SelectedItem: AccentThemeOption option })
        {
            return;
        }

        await library.Settings.SelectAccentThemeAsync(option);
    }
}
