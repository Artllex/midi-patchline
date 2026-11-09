using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;

namespace MidiRouter;

public partial class MainWindow
{
    private string? studioFile;
    private string? savedStudio;

    private void OpenFileMenu(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { PlacementTarget = (FrameworkElement)sender, Placement = PlacementMode.Bottom };
        void Add(string label, Action action)
        {
            var item = new MenuItem { Header = label };
            item.Click += (_, _) => StudioFileDialogs.Run(this, label, action);
            menu.Items.Add(item);
        }

        Add("Nowy", NewStudioFile);
        Add("Otwórz", OpenStudioFile);
        Add("Zapisz", () => SaveStudioFile());
        Add("Zapisz jako", () => SaveStudioFile(saveAs: true));
        Add("Importuj", ImportStudioItems);
        Add("Ustawienia", () => new GeneralSettingsWindow(Model) { Owner = this }.ShowDialog());
        menu.IsOpen = true;
    }

    private void NewStudioFile()
    {
        if (!ConfirmStudioChange()) return;
        if (!Model.ReplaceStudio(RouterViewModel.NewStudio())) throw new IOException(Model.Status);
        studioFile = null;
        savedStudio = StudioFiles.Snapshot(Model.State);
        RestoreSectionOrder();
    }

    private void OpenStudioFile()
    {
        var dialog = new OpenFileDialog { Title = "Otwórz studio", Filter = StudioFileDialogs.StudioFilter };
        if (dialog.ShowDialog(this) != true) return;
        var studio = RouterViewModel.ReadStudio(dialog.FileName);
        if (!ConfirmStudioChange()) return;
        if (string.Equals(studioFile, dialog.FileName, StringComparison.OrdinalIgnoreCase))
            studio = RouterViewModel.ReadStudio(dialog.FileName);
        if (!Model.ReplaceStudio(studio)) throw new IOException(Model.Status);
        studioFile = dialog.FileName;
        savedStudio = StudioFiles.Snapshot(Model.State);
        RestoreSectionOrder();
    }

    private bool SaveStudioFile(bool saveAs = false)
    {
        var path = studioFile;
        if (path == null || saveAs)
        {
            var dialog = new SaveFileDialog
            {
                Title = saveAs ? "Zapisz studio jako" : "Zapisz studio",
                Filter = StudioFileDialogs.StudioFilter,
                DefaultExt = ".patchline.json",
                FileName = path == null ? "Studio" : Path.GetFileName(path)
            };
            if (dialog.ShowDialog(this) != true) return false;
            path = dialog.FileName;
        }

        StudioFiles.Write(path, Model.State);
        studioFile = path;
        savedStudio = StudioFiles.Snapshot(Model.State);
        return true;
    }

    private bool ConfirmStudioChange()
    {
        if (savedStudio == StudioFiles.Snapshot(Model.State)) return true;
        return MessageBox.Show(this, "Zapisać bieżące studio przed jego zmianą?", "Studio MIDI Patchline",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question) switch
        {
            MessageBoxResult.Yes => SaveStudioFile(),
            MessageBoxResult.No => true,
            _ => false
        };
    }

    private void ImportStudioItems()
    {
        var dialog = new OpenFileDialog { Title = "Importuj scenę lub Split", Filter = StudioFileDialogs.ExchangeFilter };
        if (dialog.ShowDialog(this) != true) return;
        var items = StudioFiles.ReadExchange(dialog.FileName);
        var updated = StudioFiles.Merge(Model, items);
        if (!Model.ReplaceStudio(updated))
            throw new IOException(Model.Status);
        RestoreSectionOrder();
    }
}
