using System.IO;
using System.Reflection;
namespace MidiRouter;

internal static class DiagnosticBridge
{
    public static bool TryRun(App app, string[] arguments)
    {
        if (!arguments.Any(arg => arg is "--self-test" or "--startup-check" or "--settings-check"))
            return false;

        try
        {
            var assemblyPath = Path.Combine(AppContext.BaseDirectory, "MidiPatchline.Checks.dll");
            var assembly = Assembly.LoadFrom(assemblyPath);
            var entry = assembly.GetType("MidiRouter.DiagnosticEntry", throwOnError: true)!;
            entry.GetMethod("Run")!.Invoke(null, new object[] { app, arguments });
        }
        catch (Exception ex)
        {
            var error = ex is TargetInvocationException { InnerException: not null } invocation ? invocation.InnerException! : ex;
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "diagnostic-error.txt"), error.ToString());
            app.Shutdown(1);
        }
        return true;
    }
}
