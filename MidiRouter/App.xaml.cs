using System.Windows;
namespace MidiRouter;

public partial class App : Application
{
    private PointerVisibilityController? pointer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        UiPresentation.Initialize();
        ScrollBehavior.Initialize();
        pointer = new PointerVisibilityController();
        if (DiagnosticBridge.TryRun(this, e.Args))
            return;
        new MainWindow().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        pointer?.Dispose();
        base.OnExit(e);
    }

    internal void SetDiagnosticPointer(System.Windows.Input.Cursor? cursor) => pointer?.SetPointer(cursor);
}
