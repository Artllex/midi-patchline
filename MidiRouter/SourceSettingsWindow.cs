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
    public static string? UriFor(string? key) {
        if(string.IsNullOrWhiteSpace(key))return null;
        if(Path.IsPathRooted(key)||key.StartsWith("pack://",StringComparison.OrdinalIgnoreCase))return key;
        if(!Choices.Any(c=>c.Key==key))return null;
        return "pack://application:,,,/Assets/"+(key=="jp"?"jp8080":key=="roland"?"jd800":key)+".png";
    }
    public static System.Windows.Media.Imaging.BitmapSource? Load(string? key) {
        var uri=UriFor(key);if(uri==null)return null;
        try { var image=new System.Windows.Media.Imaging.BitmapImage();image.BeginInit();image.CacheOption=System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;image.UriSource=new Uri(uri);image.EndInit();image.Freeze();return image; }
        catch(Exception ex) when(ex is IOException or NotSupportedException or UriFormatException or System.IO.FileFormatException){return null;}
    }
    public static string? Pick()
    {
        var dialog=new Microsoft.Win32.OpenFileDialog { Filter="Grafiki|*.png;*.jpg;*.jpeg;*.webp;*.bmp" };
        if(dialog.ShowDialog()!=true)return null;
        // WPF does not decode WebP: prepared artwork is supplied as PNG.
        if(Path.GetExtension(dialog.FileName).Equals(".webp",StringComparison.OrdinalIgnoreCase)) {
            MessageBox.Show("Wybierz PNG, JPG lub BMP. Grafiki WebP przygotuj najpierw jako PNG.");return null;
        }
        var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MidiRouter","Artwork");
        Directory.CreateDirectory(dir);
        var file=Path.Combine(dir,Guid.NewGuid().ToString("N")+Path.GetExtension(dialog.FileName));
        File.Copy(dialog.FileName,file);return file;
    }
}
public sealed class ArtworkChoice
{
    public string Key {get;init;}="";
    public string Name {get;init;}="";
}
public sealed class SourceSettingsWindow : SettingsWindow { public SourceSettingsWindow(RouterViewModel model,Source source):base(model,source){} }
public sealed class RenameWindow : Window
{
    public RenameWindow(string value,Action<string> commit)
    {
        Title="Zmień nazwę";Width=345;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        Background=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(24,27,32));Foreground=System.Windows.Media.Brushes.White;
        var panel=new StackPanel{Margin=new Thickness(19.2),LayoutTransform=new System.Windows.Media.ScaleTransform(0.8,0.8)};Content=panel;
        panel.Children.Add(new TextBlock{Text="Nazwa",FontSize=16,Margin=new Thickness(0,0,0,10)});
        var input=new TextBox{Text=value,MaxLength=100};panel.Children.Add(input);
        var error=new TextBlock{Foreground=System.Windows.Media.Brushes.Salmon,TextWrapping=TextWrapping.Wrap};panel.Children.Add(error);
        var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,12,0,0)};panel.Children.Add(footer);
        footer.Children.Add(new Button{Content="Anuluj",IsCancel=true,Margin=new Thickness(0,0,10,0)});
        var save=new Button{Content="Zapisz",IsDefault=true};footer.Children.Add(save);
        save.Click+=(_,_)=>{if(string.IsNullOrWhiteSpace(input.Text)){error.Text="Wpisz nazwę.";return;}try{commit(input.Text.Trim());DialogResult=true;}catch(Exception ex){error.Text=ex.Message;}};
        Loaded+=(_,_)=>{input.Focus();input.SelectAll();};
    }
}











