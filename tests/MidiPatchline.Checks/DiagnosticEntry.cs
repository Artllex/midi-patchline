using System.IO;
namespace MidiRouter;

public static class DiagnosticEntry
{
    public static void Run(App app, string[] args)
    {
        app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
        app.DispatcherUnhandledException += (_, error) =>
        {
            var check = args.First(arg => arg.EndsWith("check")).TrimStart('-');
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, check + "-error.txt"), error.Exception.ToString());
            error.Handled = true;
            app.Shutdown(1);
        };
        if (args.Contains("--self-test"))
        {
            try
            {
                ModelChecks.Run();
                app.Shutdown(0);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "self-test-error.txt"), ex.ToString());
                app.Shutdown(1);
            }
            return;
        }
        new UiChecks(app).Run(args);
    }
}
