using System.Windows;
using System.Windows.Controls;
using System.IO;
namespace MidiRouter;

public static class ArtworkPicker
{
    public static ArtworkChoice[] Choices => new[] {
        new ArtworkChoice{Key="",Name="Brak grafiki"},new(){Key="roli",Name="ROLI Seaboard 2"},new(){Key="nektar",Name="Nektar GXP61"},
        new(){Key="jd800",Name="JD-800"},new(){Key="roland",Name="JD-800"},new(){Key="jp",Name="JP-8080"},new(){Key="virus",Name="Virus TI"},new(){Key="kobol",Name="Kobol"},
        new(){Key="model-d",Name="Model D"},new(){Key="system-8",Name="System-8"},new(){Key="cz5000",Name="CZ-5000"},new(){Key="nord",Name="Nord Rack 2"},new(){Key="neutron",Name="Neutron"}
    };
    public static string? UriFor(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;
        if (Path.IsPathRooted(key) || key.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
            return key;
        if (!Choices.Any(c => c.Key == key))
            return null;
        return "pack://application:,,,/Assets/" + (key == "jp" ? "jp8080" : key == "roland" ? "jd800" : key) + ".png";
    }
    public static System.Windows.Media.Imaging.BitmapSource? Load(string? key)
    {
        var uri = UriFor(key);
        if (uri == null)
            return null;
        try
        {
            var image = new System.Windows.Media.Imaging.BitmapImage();
            image.BeginInit();
            image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(uri);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or UriFormatException or System.IO.FileFormatException) { return null; }
    }
    public static string? Pick()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = L.T("Grafiki|*.png;*.jpg;*.jpeg;*.webp;*.bmp") };
        if (dialog.ShowDialog() != true)
            return null;
        // WPF does not decode WebP: prepared artwork is supplied as PNG.
        if (Path.GetExtension(dialog.FileName).Equals(".webp", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(L.T("Wybierz PNG, JPG lub BMP. Grafiki WebP przygotuj najpierw jako PNG."));
            return null;
        }
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MidiRouter", "Artwork");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, Guid.NewGuid().ToString("N") + Path.GetExtension(dialog.FileName));
        File.Copy(dialog.FileName, file);
        return file;
    }
}
