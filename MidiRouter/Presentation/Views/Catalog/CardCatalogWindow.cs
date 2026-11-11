using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MidiRouter;

public sealed class CardCatalogWindow : Window
{
    private sealed record Entry(string Name, Func<bool> IsPinned, Action<bool> Pin, Action Select, Action? Delete = null, Action? Export = null);

    public CardCatalogWindow(RouterViewModel model)
     : this("Katalog", "Brak kart", () => model.Cards.Select(card => new Entry(card.Name, () => card.IsPinned,
         pinned => model.SetCardPinned(card, pinned), () => model.ActivateCard(card.Id), () => model.DeleteCard(card),
         () => StudioFileDialogs.Export(Application.Current.MainWindow, card.Name, () => StudioFiles.ExportScene(model, card)))))
    {
    }

    public CardCatalogWindow(IEnumerable<SplitProfile> profiles, Action<SplitProfile> select, Action<SplitProfile, bool> pin, Action<SplitProfile> export)
     : this("Splity", "Brak profili Split", () => profiles.Select(profile => new Entry(profile.Name, () => profile.IsPinned,
         pinned => pin(profile, pinned), () => select(profile), Export: () => export(profile))))
    {
    }

    private CardCatalogWindow(string title, string emptyText, Func<IEnumerable<Entry>> entries)
    {
        Title = title;
        Width = 480;
        Height = 520;
        MinWidth = 360;
        MinHeight = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(24, 27, 32));
        Foreground = Brushes.White;
        var grid = new Grid { Margin = new Thickness(22) };
        Content = grid;
        grid.RowDefinitions.Add(new()
        {
            Height = GridLength.Auto
        });
        grid.RowDefinitions.Add(new());
        double searchHeight = 28;
        if (Application.Current.MainWindow is MainWindow main &&
            main.FindName("MainSearch") is FrameworkElement mainSearch && mainSearch.ActualHeight > 0)
            searchHeight = mainSearch.TransformToAncestor(main).TransformBounds(new Rect(mainSearch.RenderSize)).Height;
        var searchBox = new Grid { Height = searchHeight, Margin = new Thickness(0, 0, 0, 14) };
        var search = new TextBox { Style = (Style)FindResource("SearchBox"), FontSize = 14, Padding = new Thickness(10, 0, 10, 0), VerticalContentAlignment = VerticalAlignment.Center, ToolTip = "Szukaj" };
        searchBox.Children.Add(search);
        var glass = new System.Windows.Shapes.Path { Data = Geometry.Parse("M 10,6 A 4,4 0 1 1 2,6 A 4,4 0 1 1 10,6 M 9,9 L 14,14"), Stroke = new SolidColorBrush(Color.FromRgb(148, 161, 175)), StrokeThickness = 1.5, Width = 16, Height = 16, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0), IsHitTestVisible = false };
        searchBox.Children.Add(glass);
        var hint = new TextBlock { Text = "Szukaj", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(148, 161, 175)), Margin = new Thickness(34, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        searchBox.Children.Add(hint);
        search.TextChanged += (_, _) => { hint.Visibility = glass.Visibility = string.IsNullOrEmpty(search.Text) ? Visibility.Visible : Visibility.Collapsed; };
        grid.Children.Add(searchBox);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, PanningMode = PanningMode.VerticalOnly };
        Grid.SetRow(scroll, 1);
        grid.Children.Add(scroll);
        var list = new StackPanel();
        scroll.Content = list;
        void Refresh()
        {
            list.Children.Clear();
            foreach (var card in entries().Where(c => c.Name.Contains(search.Text, StringComparison.CurrentCultureIgnoreCase)))
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                row.ColumnDefinitions.Add(new());
                row.ColumnDefinitions.Add(new()
                {
                    Width = GridLength.Auto
                });
                var entry = new Button { Content = card.Name, HorizontalContentAlignment = HorizontalAlignment.Left, FontSize = 14, Margin = new Thickness(0, 0, 10, 0) };
                var caption = new Grid();
                caption.ColumnDefinitions.Add(new());
                caption.ColumnDefinitions.Add(new()
                {
                    Width = GridLength.Auto
                });
                caption.Children.Add(new TextBlock { Text = card.Name, VerticalAlignment = VerticalAlignment.Center });
                if (card.IsPinned())
                {
                    var pinIcon = new System.Windows.Shapes.Path { Data = Geometry.Parse("M 5,1 L 11,1 L 10,5 L 13,8 L 8,8 L 8,14 M 8,8 L 3,8 L 6,5 Z"), Stroke = Brushes.LightGray, StrokeThickness = 1.4, Width = 16, Height = 16, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, ToolTip = "Przypięta" };
                    Grid.SetColumn(pinIcon, 1);
                    caption.Children.Add(pinIcon);
                }
                entry.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                entry.Content = caption;
                row.Children.Add(entry);
                SceneMenu.Attach(entry, card.IsPinned, card.Pin, card.Delete, Refresh, card.Export);
                entry.Click += (_, _) => { card.Select(); DialogResult = true; };
                list.Children.Add(row);
            }
            if (list.Children.Count == 0)
                list.Children.Add(new TextBlock { Text = emptyText, Foreground = Brushes.LightSlateGray });
        }
        search.TextChanged += (_, _) => Refresh();
        Refresh();
        Loaded += (_, _) => search.Focus();
    }
}
