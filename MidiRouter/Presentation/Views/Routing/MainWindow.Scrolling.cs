using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
namespace MidiRouter;

public partial class MainWindow
{
    private readonly DispatcherTimer wheelTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private ScrollViewer? wheelViewer; private double wheelTarget; private long wheelLastTick;
    private void SmoothWheel(object sender, MouseWheelEventArgs e)
    {
        var viewer = (ScrollViewer)sender;
        if (viewer.ScrollableHeight <= 0)
            return;
        if (!wheelTimer.IsEnabled || wheelViewer != viewer)
        {
            wheelTarget = viewer.VerticalOffset;
            wheelTimer.Tick -= WheelTick;
            wheelTimer.Tick += WheelTick;
        }
        wheelViewer = viewer;
        wheelTarget = Math.Clamp(wheelTarget - e.Delta / 120.0 * 36, Math.Max(0, viewer.VerticalOffset - 180), Math.Min(viewer.ScrollableHeight, viewer.VerticalOffset + 180));
        wheelLastTick = Environment.TickCount64;
        wheelTimer.Start();
        e.Handled = true;
    }
    private void WheelTick(object? sender, EventArgs e)
    {
        if (wheelViewer == null || !wheelViewer.IsVisible || !IsVisible)
        {
            wheelTimer.Stop();
            return;
        }
        var now = Environment.TickCount64;
        double dt = Math.Clamp(now - wheelLastTick, 1, 64);
        wheelLastTick = now;
        wheelTarget = Math.Clamp(wheelTarget, 0, wheelViewer.ScrollableHeight);
        double remaining = wheelTarget - wheelViewer.VerticalOffset;
        if (Math.Abs(remaining) < .4)
        {
            wheelViewer.ScrollToVerticalOffset(wheelTarget);
            wheelTimer.Stop();
            return;
        }
        wheelViewer.ScrollToVerticalOffset(wheelViewer.VerticalOffset + remaining * (1 - Math.Exp(-dt / 55)));
    }
    private void StopWheel(object sender, MouseButtonEventArgs e) => wheelTimer.Stop();

}
