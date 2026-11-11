using System.Windows;
using System.Windows.Controls;
namespace MidiRouter;

public sealed class RenameWindow : Window
{
    public RenameWindow(string value, Action<string> commit)
    {
        Title = L.T("Zmień nazwę");
        Width = 345;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 27, 32));
        Foreground = System.Windows.Media.Brushes.White;
        var panel = new StackPanel { Margin = new Thickness(19.2), LayoutTransform = new System.Windows.Media.ScaleTransform(0.8, 0.8) };
        Content = panel;
        panel.Children.Add(new TextBlock { Text = "Nazwa", FontSize = 16, Margin = new Thickness(0, 0, 0, 10) });
        var input = new TextBox { Text = value, MaxLength = 100 };
        panel.Children.Add(input);
        var error = new TextBlock { Foreground = System.Windows.Media.Brushes.Salmon, TextWrapping = TextWrapping.Wrap };
        panel.Children.Add(error);
        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        panel.Children.Add(footer);
        footer.Children.Add(new Button { Content = "Anuluj", IsCancel = true, Margin = new Thickness(0, 0, 10, 0) });
        var save = new Button { Content = "Zapisz", IsDefault = true };
        footer.Children.Add(save);
        save.Click += (_, _) => { if (string.IsNullOrWhiteSpace(input.Text)) { error.Text = "Wpisz nazwę."; return; } try { commit(input.Text.Trim()); DialogResult = true; } catch (Exception ex) { error.Text = ex.Message; } };
        Loaded += (_, _) => { input.Focus(); input.SelectAll(); };
    }
}
