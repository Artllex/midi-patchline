using System.Windows;
namespace MidiRouter;
public partial class App : Application
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetMessageExtraInfo();
    private bool updatingCursor;
    private void SetPointer(System.Windows.Input.Cursor? cursor)
    {
        if(updatingCursor || ReferenceEquals(System.Windows.Input.Mouse.OverrideCursor,cursor))return;
        updatingCursor=true;
        try {System.Windows.Input.Mouse.OverrideCursor=cursor;}
        finally {updatingCursor=false;}
    }
    private void UpdatePointer(object sender,System.Windows.Input.PreProcessInputEventArgs e)
    {
        if(updatingCursor)return;
        var input=e.StagingItem.Input;
        if(input is System.Windows.Input.MouseEventArgs mouse)
        {
            // Cursor changes themselves raise QueryCursor. Never change a cursor
            // in response to that event: only react to actual pointer actions.
            if(mouse.RoutedEvent!=System.Windows.Input.Mouse.PreviewMouseMoveEvent &&
               mouse.RoutedEvent!=System.Windows.Input.Mouse.PreviewMouseDownEvent &&
               mouse.RoutedEvent!=System.Windows.Input.Mouse.PreviewMouseWheelEvent)return;
            long extra=GetMessageExtraInfo().ToInt64();
            bool promoted=(extra & 0xFFFFFF00L)==0xFF515700L;
            if(!promoted)SetPointer(null);
        }
        else if(input is System.Windows.Input.StylusEventArgs stylus &&
                !stylus.InAir && stylus.StylusDevice!=null &&
                stylus.StylusDevice.TabletDevice.Type==System.Windows.Input.TabletDeviceType.Touch)
            SetPointer(System.Windows.Input.Cursors.None);
    }
    protected override void OnExit(ExitEventArgs e)
    {
        System.Windows.Input.InputManager.Current.PreProcessInput-=UpdatePointer;
        System.Windows.Input.Mouse.OverrideCursor=null;
        base.OnExit(e);
    }
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--self-test"))
        {
            try { ModelChecks.Run(); Shutdown(0); }
            catch (Exception ex) { System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"self-test-error.txt"), ex.ToString()); Shutdown(1); }
            return;
        }
        EventManager.RegisterClassHandler(typeof(Window), UIElement.PreviewTouchDownEvent,
            new EventHandler<System.Windows.Input.TouchEventArgs>((_,_) =>
                SetPointer(System.Windows.Input.Cursors.None)),true);
        System.Windows.Input.InputManager.Current.PreProcessInput+=UpdatePointer;
        new MainWindow().Show();
        if(e.Args.Contains("--startup-check"))
        {
            var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)};
            int steps=0;
            timer.Tick+=(_,_)=>{
                // Exercise both cursor states while the real input hook is active.
                SetPointer(steps%2==0?System.Windows.Input.Cursors.None:null);
                System.Windows.Input.Mouse.UpdateCursor();
                if(++steps==8){
                    if(!NativeTheme.HasNativeCaptionButtons(MainWindow))throw new InvalidOperationException("Native caption buttons missing");
                    timer.Stop();SetPointer(null);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"startup-check-result.txt"),"PASS: window startup and repeated cursor updates under active input hook");
                    Shutdown(0);
                }
            };
            timer.Start();
        }
    }
}






