using System.Windows;
namespace MidiRouter;

internal sealed class PointerVisibilityController : IDisposable
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetMessageExtraInfo();
    private bool updatingCursor;
    public void SetPointer(System.Windows.Input.Cursor? cursor)
    {
        if (updatingCursor || ReferenceEquals(System.Windows.Input.Mouse.OverrideCursor, cursor))
            return;
        updatingCursor = true;
        try
        {
            System.Windows.Input.Mouse.OverrideCursor = cursor;
        }
        finally { updatingCursor = false; }
    }
    private void UpdatePointer(object sender, System.Windows.Input.PreProcessInputEventArgs e)
    {
        if (updatingCursor)
            return;
        var input = e.StagingItem.Input;
        if (input is System.Windows.Input.MouseEventArgs mouse)
        {
            // Cursor changes themselves raise QueryCursor. Never change a cursor
            // in response to that event: only react to actual pointer actions.
            if (mouse.RoutedEvent != System.Windows.Input.Mouse.PreviewMouseMoveEvent &&
               mouse.RoutedEvent != System.Windows.Input.Mouse.PreviewMouseDownEvent &&
               mouse.RoutedEvent != System.Windows.Input.Mouse.PreviewMouseWheelEvent)
                return;
            long extra = GetMessageExtraInfo().ToInt64();
            bool promoted = (extra & 0xFFFFFF00L) == 0xFF515700L;
            if (!promoted)
                SetPointer(null);
        }
        else if (input is System.Windows.Input.StylusEventArgs stylus &&
                !stylus.InAir && stylus.StylusDevice != null &&
                stylus.StylusDevice.TabletDevice.Type == System.Windows.Input.TabletDeviceType.Touch)
            SetPointer(System.Windows.Input.Cursors.None);
    }

    public PointerVisibilityController()
    {
        EventManager.RegisterClassHandler(typeof(Window), UIElement.PreviewTouchDownEvent,
            new EventHandler<System.Windows.Input.TouchEventArgs>((_, _) => SetPointer(System.Windows.Input.Cursors.None)), true);
        System.Windows.Input.InputManager.Current.PreProcessInput += UpdatePointer;
    }

    public void Dispose()
    {
        System.Windows.Input.InputManager.Current.PreProcessInput -= UpdatePointer;
        System.Windows.Input.Mouse.OverrideCursor = null;
    }
}
