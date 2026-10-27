using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GameTranslationLauncher.Wpf.Controls;

/// <summary>
/// Cuộn mượt bằng bánh xe chuột/touchpad cho ItemsControl có ScrollViewer cuộn theo pixel.
/// </summary>
/// <remarks>
/// The distance is proportional to the wheel delta, so precision-touchpad events (small deltas) no longer jump a
/// full notch. Each frame eases the offset toward a target; the target restarts from the real offset whenever the
/// animation is idle, so panning or keyboard scrolling in between can never leave it stale (the old one-way bug).
/// </remarks>
public static class SmoothWheelScroll
{
    private const double PixelsPerWheelDelta = 0.9; // one 120-delta notch ≈ 108px, about one game tile
    private const double EasePerFrame = 0.22;

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(SmoothWheelScroll), new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State", typeof(ScrollState), typeof(SmoothWheelScroll));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not UIElement target)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            target.PreviewMouseWheel += OnPreviewMouseWheel;
        }
        else
        {
            target.PreviewMouseWheel -= OnPreviewMouseWheel;
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var host = (DependencyObject)sender;
        if (FindScrollViewer(host) is not { } viewer)
        {
            return;
        }

        e.Handled = true;
        var distance = -e.Delta * PixelsPerWheelDelta;
        if (!SystemParameters.ClientAreaAnimation)
        {
            viewer.ScrollToVerticalOffset(viewer.VerticalOffset + distance);
            return;
        }

        if (host.GetValue(StateProperty) is not ScrollState state)
        {
            state = new ScrollState(viewer);
            host.SetValue(StateProperty, state);
        }

        state.Add(distance);
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject element)
    {
        if (element is ScrollViewer viewer)
        {
            return viewer;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            if (FindScrollViewer(VisualTreeHelper.GetChild(element, i)) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private sealed class ScrollState(ScrollViewer viewer)
    {
        private double target;
        private bool isAnimating;

        public void Add(double distance)
        {
            var start = isAnimating ? target : viewer.VerticalOffset;
            target = Math.Clamp(start + distance, 0, viewer.ScrollableHeight);
            if (!isAnimating)
            {
                isAnimating = true;
                CompositionTarget.Rendering += OnRendering;
            }
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            // Re-clamp: filtering the list can shrink the extent mid-animation.
            target = Math.Clamp(target, 0, viewer.ScrollableHeight);
            var remaining = target - viewer.VerticalOffset;
            if (Math.Abs(remaining) < 0.5 || !viewer.IsLoaded)
            {
                viewer.ScrollToVerticalOffset(target);
                isAnimating = false;
                CompositionTarget.Rendering -= OnRendering;
                return;
            }

            viewer.ScrollToVerticalOffset(viewer.VerticalOffset + remaining * EasePerFrame);
        }
    }
}
