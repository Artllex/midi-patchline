using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MidiRouter;

public static class SceneMenu
{
    public static void Attach(FrameworkElement target, RouterViewModel model, RoutingCard card, Action? refresh = null) =>
     Attach(target, () => card.IsPinned, pinned => model.SetCardPinned(card, pinned), () => model.DeleteCard(card), refresh,
      () => StudioFileDialogs.Export(Window.GetWindow(target), card.Name, () => StudioFiles.ExportScene(model, card)));

    public static void Attach(FrameworkElement target, Func<bool> isPinned, Action<bool> setPinned, Action? remove = null, Action? refresh = null, Action? export = null)
    {
        var timer = ScrollGestureGuard.Watch(new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(650) });
        Point origin = new();
        bool held = false;
        int touchId = -1;
        void Open()
        {
            var menu = new ContextMenu { PlacementTarget = target };
            var pin = new MenuItem { Header = isPinned() ? "Odepnij" : "Przypnij" };
            pin.Click += (_, _) => { setPinned(!isPinned()); refresh?.Invoke(); };
            menu.Items.Add(pin);
            if (remove != null)
            {
                var delete = new MenuItem { Header = "Usuń" };
                delete.Click += (_, _) => { remove(); refresh?.Invoke(); };
                menu.Items.Add(delete);
            }
            if (export != null)
            {
                var exportItem = new MenuItem { Header = "Eksportuj" };
                exportItem.Click += (_, _) => export();
                menu.Items.Add(exportItem);
            }
            menu.IsOpen = true;
        }
        target.PreviewMouseRightButtonUp += (_, e) => { if (e.StylusDevice != null) { e.Handled = true; return; } Open(); e.Handled = true; };
        System.Windows.Input.Stylus.SetIsPressAndHoldEnabled(target, false);
        target.PreviewTouchDown += (_, e) => { timer.Stop(); origin = e.GetTouchPoint(Window.GetWindow(target)).Position; touchId = e.TouchDevice.Id; held = false; timer.Start(); };
        target.PreviewTouchMove += (_, e) => { if (e.TouchDevice.Id == touchId && (e.GetTouchPoint(Window.GetWindow(target)).Position - origin).Length > 8) timer.Stop(); };
        target.PreviewTouchUp += (_, e) => { timer.Stop(); if (held) { e.Handled = true; held = false; } };
        target.PreviewMouseLeftButtonUp += (_, e) => { if (held) { e.Handled = true; held = false; } };
        timer.Tick += (_, _) => { timer.Stop(); held = true; Open(); };
        target.Unloaded += (_, _) => timer.Stop();
    }
}
