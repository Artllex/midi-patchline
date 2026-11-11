using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MidiRouter;

internal static class ScrollBehavior
{
    public static void Initialize()
    {
        EventManager.RegisterClassHandler(typeof(ScrollViewer), UIElement.PreviewMouseWheelEvent,
            new MouseWheelEventHandler(OnWheel));
    }

    private static void OnWheel(object sender, MouseWheelEventArgs e)
    {
        var viewer = (ScrollViewer)sender;
        if (viewer.ScrollableHeight <= 0 || e.Handled) return;
        // Preserve high-resolution deltas; do not turn each touchpad event into a full line.
        double distance = e.Delta * (18.0 / 120.0);
        viewer.ScrollToVerticalOffset(Math.Clamp(viewer.VerticalOffset - distance, 0, viewer.ScrollableHeight));
        e.Handled = true;
    }
}
