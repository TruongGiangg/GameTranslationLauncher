namespace GameTranslationLauncher.Wpf;

/// <summary>
/// Điểm vào và composition root của ứng dụng WPF.
/// </summary>
public partial class App : System.Windows.Application
{
    protected override async void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        var artworkResolver = new Services.GameArtworkResolver();
        var viewModel = Composition.LauncherCompositionRoot.CreateGameLibraryViewModel(artworkResolver);
        var artworkPreparation = artworkResolver.PreloadAsync();
        await viewModel.InitializeAsync();
        var window = new MainWindow { DataContext = viewModel };
        var closing = false;
        var selectionSaved = false;
        var selectionSaveFailed = false;
        window.Closing += async (_, args) =>
        {
            if (selectionSaved || selectionSaveFailed || viewModel.SelectedGame is null)
            {
                return;
            }

            args.Cancel = true;
            if (closing)
            {
                return;
            }

            closing = true;
            window.IsEnabled = false;
            var saved = await viewModel.Settings.SaveLastSelectedGameAsync(viewModel.SelectedGame.Id);
            window.IsEnabled = true;
            closing = false;
            if (saved)
            {
                selectionSaved = true;
                // NOTE(close-reentry): An unchanged selection may complete synchronously inside Closing.
                _ = window.Dispatcher.BeginInvoke(new Action(window.Close));
            }
            else
            {
                // NOTE(close-save-failure): Show the existing error once; the next close may exit even if storage stays unavailable.
                selectionSaveFailed = true;
                viewModel.ShowSettingsCommand.Execute(null);
            }
        };
        window.Show();

        await Task.WhenAll(window.ShowStartupAsync(async () =>
        {
            await artworkPreparation;
            await viewModel.LoadAsync();
        }), viewModel.CheckForUpdatesAsync());
    }
}
