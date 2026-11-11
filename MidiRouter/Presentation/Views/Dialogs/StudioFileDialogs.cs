using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace MidiRouter;

public static class StudioFileDialogs
{
    public const string StudioFilter = "Studio MIDI Patchline (*.patchline.json)|*.patchline.json|Pliki JSON (*.json)|*.json";
    public const string ExchangeFilter = "Scena lub Split MIDI Patchline (*.patchline-export.json)|*.patchline-export.json|Pliki JSON (*.json)|*.json";

    public static void Export(Window? owner, string name, Func<StudioExchange> create)
    {
        var safeName = string.Concat(name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch)).Trim().TrimEnd('.');
        var dialog = new SaveFileDialog { Title = L.T("Eksportuj"), Filter = L.T(ExchangeFilter), FileName = safeName, DefaultExt = ".patchline-export.json" };
        if (dialog.ShowDialog(owner) == true)
            Run(owner, "Eksportuj", () => StudioFiles.Write(dialog.FileName, create()));
    }

    public static void Run(Window? owner, string operation, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            MessageBox.Show(owner, L.T(ex.Message), L.T(operation), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
