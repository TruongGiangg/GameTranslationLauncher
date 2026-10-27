using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GameTranslationLauncher.Wpf;

/// <summary>
/// Cửa sổ shell của Launcher; nghiệp vụ được cung cấp qua ViewModel.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        if (SystemParameters.ClientAreaAnimation)
        {
            StartupOverlay.Visibility = Visibility.Visible;
            MainContent.Opacity = 0;
        }
    }

    public async Task ShowStartupAsync(Func<Task> loadCatalog)
    {
        ArgumentNullException.ThrowIfNull(loadCatalog);
        if (!SystemParameters.ClientAreaAnimation)
        {
            StartupOverlay.Visibility = Visibility.Collapsed;
            MainContent.Opacity = 1;
            await loadCatalog();
            return;
        }

        var renderSource = PresentationSource.FromVisual(this) as HwndSource;
        var renderTarget = renderSource?.CompositionTarget;
        var previousRenderMode = renderTarget?.RenderMode;
        try
        {
            // WPF can decode media without displaying frames on some GPU paths.
            if (renderTarget is not null)
            {
                renderTarget.RenderMode = RenderMode.SoftwareOnly;
            }

            await Dispatcher.Yield(DispatcherPriority.Background);
            var intro = PlayIntroAsync();
            await Task.WhenAll(loadCatalog(), intro);
            if (!await intro)
            {
                await ShowLogoFallbackAsync();
                return;
            }

            var reveal = TimeSpan.FromMilliseconds(180);
            MainContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, reveal));
            StartupOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, reveal));
            await Task.Delay(reveal);
        }
        finally
        {
            StartupVideo.Close();
            StartupVideo.Source = null;
            MainContent.BeginAnimation(OpacityProperty, null);
            MainContent.Opacity = 1;
            StartupOverlay.IsHitTestVisible = false;
            StartupOverlay.Visibility = Visibility.Collapsed;
            if (renderSource is { IsDisposed: false } && renderTarget is not null && previousRenderMode is { } renderMode)
            {
                renderTarget.RenderMode = renderMode;
            }
        }
    }

    private async Task<bool> PlayIntroAsync()
    {
        var completion = new TaskCompletionSource<bool>();
        void Opened(object sender, RoutedEventArgs e)
        {
            StartupVideo.Visibility = Visibility.Visible;
            StartupLogo.Visibility = Visibility.Collapsed;
        }
        void Ended(object sender, RoutedEventArgs e) => completion.TrySetResult(true);
        void Failed(object? sender, ExceptionRoutedEventArgs e) => completion.TrySetResult(false);

        StartupVideo.MediaOpened += Opened;
        StartupVideo.MediaEnded += Ended;
        StartupVideo.MediaFailed += Failed;
        try
        {
            // NOTE(intro-content): WPF media needs a content file beside the app, not a pack resource.
            StartupVideo.Source = new Uri(System.IO.Path.Combine(
                AppContext.BaseDirectory, "Resources", "Media", "tg-dragon-intro.mp4"));
            StartupVideo.Play();
            var completed = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(4)));
            return completed == completion.Task && await completion.Task;
        }
        finally
        {
            StartupVideo.MediaOpened -= Opened;
            StartupVideo.MediaEnded -= Ended;
            StartupVideo.MediaFailed -= Failed;
        }
    }

    private async Task ShowLogoFallbackAsync()
    {
        StartupVideo.Close();
        StartupVideo.Visibility = Visibility.Collapsed;
        StartupLogo.Visibility = Visibility.Visible;
        StartupLogo.BeginAnimation(OpacityProperty, new DoubleAnimation(0.12, 1, TimeSpan.FromMilliseconds(220)));
        try
        {
            var completion = new TaskCompletionSource();
            var fade = CreateStartupExitAnimation(1, 0);
            fade.Completed += (_, _) => completion.SetResult();
            MainContent.BeginAnimation(OpacityProperty, CreateStartupExitAnimation(0, 1));
            StartupLogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, CreateStartupExitAnimation(1, 0.82));
            StartupLogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, CreateStartupExitAnimation(1, 0.82));
            StartupOverlay.BeginAnimation(OpacityProperty, fade);
            await completion.Task;
        }
        finally
        {
            StartupLogo.BeginAnimation(OpacityProperty, null);
        }
    }

    private static DoubleAnimation CreateStartupExitAnimation(double from, double to) => new(
        from, to, TimeSpan.FromMilliseconds(250))
    {
        BeginTime = TimeSpan.FromMilliseconds(550),
        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
    };

    private void TitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<Button>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        DragMove();
    }

    private static T? FindAncestor<T>(DependencyObject? element)
        where T : DependencyObject
    {
        while (element is not null)
        {
            if (element is T result)
            {
                return result;
            }

            element = VisualTreeHelper.GetParent(element);
        }

        return null;
    }

    private void MinimizeButtonClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void MaximizeButtonClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void CloseButtonClick(object sender, RoutedEventArgs e) => Close();
}
