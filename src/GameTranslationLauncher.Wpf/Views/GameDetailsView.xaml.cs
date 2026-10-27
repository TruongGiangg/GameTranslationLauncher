using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using GameTranslationLauncher.Wpf.Services;
using GameTranslationLauncher.Wpf.ViewModels;

namespace GameTranslationLauncher.Wpf.Views;

/// <summary>
/// Màn hình chi tiết game; code-behind chỉ điều khiển chuyển động của hero/nội dung và báo hero cho kính Glassy.
/// </summary>
public partial class GameDetailsView : UserControl
{
    private static readonly Duration HeroFadeDuration = TimeSpan.FromMilliseconds(650);
    private static readonly Duration KenBurnsDuration = TimeSpan.FromSeconds(26);
    private static readonly Duration EntranceDuration = TimeSpan.FromMilliseconds(520);
    private const double KenBurnsScale = 1.06;
    private const double EntranceOffset = 14;

    private ImageSource? shownHero;

    public GameDetailsView()
    {
        InitializeComponent();
        HeroPrevious.RenderTransform = GlassBackdrop.Zoom;
        HeroCurrent.RenderTransform = GlassBackdrop.Zoom;
        Loaded += (_, _) => GlassBackdrop.Host = HeroHost;
    }

    private static bool IsMotionEnabled => SystemParameters.ClientAreaAnimation;

    private void HeroTargetUpdated(object? sender, DataTransferEventArgs e)
    {
        var next = HeroCurrent.Source;
        if (ReferenceEquals(next, shownHero))
        {
            return;
        }

        // The old hero stays underneath while the new one fades in; with no new image the fallback brush shows.
        HeroPrevious.Source = next is null ? null : shownHero;
        shownHero = next;
        GlassBackdrop.SetHero(next, (DataContext as GameLibraryViewModel)?.Details.HeroBrush);
        Animate(HeroCurrent, OpacityProperty, 0, 1, HeroFadeDuration, TimeSpan.Zero);
        Animate(GlassBackdrop.Zoom, ScaleTransform.ScaleXProperty, 1, KenBurnsScale, KenBurnsDuration, TimeSpan.Zero);
        Animate(GlassBackdrop.Zoom, ScaleTransform.ScaleYProperty, 1, KenBurnsScale, KenBurnsDuration, TimeSpan.Zero);
    }

    private void TitleTargetUpdated(object? sender, DataTransferEventArgs e)
    {
        var delay = TimeSpan.Zero;
        foreach (var element in new FrameworkElement[] { TitleRow, DescriptionText, NoticeBorder, Dock })
        {
            if (element.RenderTransform is not TranslateTransform offset)
            {
                offset = new TranslateTransform();
                element.RenderTransform = offset;
            }

            Animate(element, OpacityProperty, 0, 1, EntranceDuration, delay);
            Animate(offset, TranslateTransform.YProperty, EntranceOffset, 0, EntranceDuration, delay);
            delay += TimeSpan.FromMilliseconds(60);
        }
    }

    private static void Animate(
        IAnimatable target,
        DependencyProperty property,
        double from,
        double to,
        Duration duration,
        TimeSpan delay)
    {
        if (!IsMotionEnabled)
        {
            // Windows "show animations" is off: jump straight to the end state.
            target.BeginAnimation(property, new DoubleAnimation(to, TimeSpan.Zero));
            return;
        }

        // Key frames hold the start value during the stagger delay; a delayed BeginTime would flash the end value first.
        var animation = new DoubleAnimationUsingKeyFrames();
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(delay)));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            to,
            KeyTime.FromTimeSpan(delay + duration.TimeSpan),
            new CubicEase { EasingMode = EasingMode.EaseOut }));
        target.BeginAnimation(property, animation);
    }
}
