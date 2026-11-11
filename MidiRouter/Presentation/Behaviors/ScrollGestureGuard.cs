using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MidiRouter;

internal static class ScrollGestureGuard
{
    private static readonly List<WeakReference<DispatcherTimer>> Timers = new();

    public static DispatcherTimer Watch(DispatcherTimer timer)
    {
        Timers.Add(new WeakReference<DispatcherTimer>(timer));
        return timer;
    }

    public static void Initialize()
    {
        EventManager.RegisterClassHandler(typeof(ScrollViewer), ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler((_, e) =>
            {
                if (e.VerticalChange == 0 && e.HorizontalChange == 0) return;
                for (int i = Timers.Count - 1; i >= 0; i--)
                    if (Timers[i].TryGetTarget(out var timer)) timer.Stop();
                    else Timers.RemoveAt(i);
            }), true);
    }
}
