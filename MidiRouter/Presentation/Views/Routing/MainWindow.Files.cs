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
            if (label == "Ustawienia") item.Icon = new TextBlock { Text = "\uE713", FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"), FontSize = 14 };
            item.Click += (_, _) => StudioFileDialogs.Run(this, label, action);
            menu.Items.Add(item);
        }

        Add("Nowy", NewStudioFile);
        Add("Otwórz", OpenStudioFile);
        Add("Zapisz", () => SaveStudioFile());
        Add("Zapisz jako", () => SaveStudioFile(saveAs: true));
        Add("Importuj", ImportStudioItems);
        Add("Makieta", LoadMockup);
        Add("Ustawienia", () => new GeneralSettingsWindow(Model) { Owner = this }.ShowDialog());
        menu.IsOpen = true;
    }

    private void LoadMockup()
    {
        if (!ConfirmStudioChange()) return;
        var backup = StudioStore.DefaultPath + ".before-mockup-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
        StudioFiles.Write(backup, Model.State);
        var mockup = MockupData.CreateStudio();
        mockup.Language = Model.State.Language;
        mockup.Graphics = Model.Graphics;
        if (!Model.ReplaceStudio(mockup)) throw new IOException(Model.Status);
        studioFile = null;
        savedStudio = StudioFiles.Snapshot(Model.State);
        CloseSplitView();
        RestoreSectionOrder();
    }

    private void NewStudioFile()
    {
        if (!ConfirmStudioChange())
            return;
        if (!Model.ReplaceStudio(RouterViewModel.NewStudio()))
            throw new IOException(Model.Status);
        studioFile = null;
        savedStudio = StudioFiles.Snapshot(Model.State);
        RestoreSectionOrder();
    }

    private void OpenStudioFile()
    {
        var dialog = new OpenFileDialog { Title = L.T("Otwórz studio"), Filter = L.T(StudioFileDialogs.StudioFilter) };
        if (dialog.ShowDialog(this) != true)
            return;
        var studio = RouterViewModel.ReadStudio(dialog.FileName);
        if (!ConfirmStudioChange())
            return;
        if (string.Equals(studioFile, dialog.FileName, StringComparison.OrdinalIgnoreCase))
            studio = RouterViewModel.ReadStudio(dialog.FileName);
        if (!Model.ReplaceStudio(studio))
            throw new IOException(Model.Status);
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
                Title = L.T(saveAs ? "Zapisz studio jako" : "Zapisz studio"),
                Filter = L.T(StudioFileDialogs.StudioFilter),
                DefaultExt = ".patchline.json",
                FileName = path == null ? "Studio" : Path.GetFileName(path)
            };
            if (dialog.ShowDialog(this) != true)
                return false;
            path = dialog.FileName;
        }

        StudioFiles.Write(path, Model.State);
        studioFile = path;
        savedStudio = StudioFiles.Snapshot(Model.State);
        return true;
    }

    private bool ConfirmStudioChange()
    {
        if (savedStudio == StudioFiles.Snapshot(Model.State))
            return true;
        return MessageBox.Show(this, L.T("Zapisać bieżące studio przed jego zmianą?"), L.T("Studio MIDI Patchline"),
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question) switch
        {
            MessageBoxResult.Yes => SaveStudioFile(),
            MessageBoxResult.No => true,
            _ => false
        };
    }

    private void ImportStudioItems()
    {
        var dialog = new OpenFileDialog { Title = L.T("Importuj scenę lub Split"), Filter = L.T(StudioFileDialogs.ExchangeFilter) };
        if (dialog.ShowDialog(this) != true)
            return;
        var items = StudioFiles.ReadExchange(dialog.FileName);
        var updated = StudioFiles.Merge(Model, items);
        if (!Model.ReplaceStudio(updated))
            throw new IOException(Model.Status);
        RestoreSectionOrder();
    }
}
