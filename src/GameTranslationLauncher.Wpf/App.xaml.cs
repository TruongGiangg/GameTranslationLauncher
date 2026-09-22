namespace GameTranslationLauncher.Wpf;

/// <summary>
/// Điểm vào và composition root của ứng dụng WPF.
/// </summary>
public partial class App : System.Windows.Application
{
    protected override async void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        var viewModel = Composition.LauncherCompositionRoot.CreateGameLibraryViewModel();
        await viewModel.InitializeAsync();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();

        await Task.WhenAll(viewModel.LoadAsync(), viewModel.CheckForUpdatesAsync());
    }
}
