using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MidiRouter;
public sealed class GeneralSettingsWindow : Window
{
 public GeneralSettingsWindow(RouterViewModel model)
 {
  Title = "Ustawienia";
  Width = 520;
  Height = 590;
  MinWidth = 460;
  MinHeight = 360;
  WindowStartupLocation = WindowStartupLocation.CenterOwner;
  Background = new SolidColorBrush(Color.FromRgb(24, 27, 32));
  Foreground = Brushes.White;

  var layout = new DockPanel { Margin = new Thickness(22) };
  Content = layout;
  var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
  DockPanel.SetDock(footer, Dock.Bottom);
  layout.Children.Add(footer);
  footer.Children.Add(new Button { Content = "Anuluj", IsCancel = true, FontSize = 13, Margin = new Thickness(0, 0, 8, 0) });
  var save = new Button { Content = "Zapisz", IsDefault = true, FontSize = 13 };
  footer.Children.Add(save);

  var error = new TextBlock { Foreground = Brushes.Salmon, TextWrapping = TextWrapping.Wrap };
  DockPanel.SetDock(error, Dock.Bottom);
  layout.Children.Add(error);
  var panel = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
  layout.Children.Add(new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, PanningMode = PanningMode.VerticalOnly });
  panel.Children.Add(new TextBlock { Text = "Główny interfejs MIDI", FontSize = 16, Margin = new Thickness(0, 0, 0, 8) });
  var options = MidiPorts.Outputs(model.State.MainInterface);
  options.RemoveAll(p => p.Value == "");
  if (!options.Any(p => p.Value == "Windows MIDI"))
   options.Insert(0, new() { Value = "Windows MIDI", Label = "Windows MIDI · wszystkie porty", Present = true });
  var choice = new ComboBox { ItemsSource = options, DisplayMemberPath = "Label", SelectedValuePath = "Value", SelectedValue = model.State.MainInterface };
  panel.Children.Add(choice);
  panel.Children.Add(new TextBlock { Text = "Grafika", FontSize = 17, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 20, 0, 8) });

  var graphics = model.Graphics;
  var fields = new List<(string Label, TextBox Box, int Min, int Max)>();
  void AddNumber(string label, int value, int min, int max)
  {
   var row = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
   var input = new TextBox { Text = value.ToString(), Width = 72, FontSize = 14, Padding = new Thickness(6, 4, 6, 4), ToolTip = $"{min}–{max}" };
   System.Windows.Automation.AutomationProperties.SetName(input, label);
   DockPanel.SetDock(input, Dock.Right);
   row.Children.Add(input);
   row.Children.Add(new TextBlock { Text = label, FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
   panel.Children.Add(row);
   fields.Add((label, input, min, max));
  }

  AddNumber("Wysokość paneli źródeł", graphics.SourceHeight, 88, 300);
  AddNumber("Wysokość paneli odbiorców", graphics.ReceiverHeight, 44, 240);
  AddNumber("Odstęp pionowy", graphics.VerticalGap, 0, 64);
  AddNumber("Odstęp poziomy", graphics.HorizontalGap, 0, 64);
  AddNumber("Grubość obwódek", graphics.OutlineWidth, 0, 8);
  var sourceThumbnails = new CheckBox { Content = "Pokaż miniaturki źródeł", IsChecked = graphics.ShowSourceThumbnails, FontSize = 14 };
  var receiverThumbnails = new CheckBox { Content = "Pokaż miniaturki odbiorców", IsChecked = graphics.ShowReceiverThumbnails, FontSize = 14 };
  panel.Children.Add(sourceThumbnails);
  panel.Children.Add(receiverThumbnails);

  save.Click += (_, _) =>
  {
   var values = new int[fields.Count];
   for (int i = 0; i < fields.Count; i++)
   {
    var (label, box, min, max) = fields[i];
    if (!int.TryParse(box.Text, out values[i]) || values[i] < min || values[i] > max)
    {
     error.Text = $"{label} — wpisz liczbę od {min} do {max}.";
     box.Focus();
     box.SelectAll();
     return;
    }
   }

   var updated = new GraphicsPreferences
   {
    SourceHeight = values[0],
    ReceiverHeight = values[1],
    VerticalGap = values[2],
    HorizontalGap = values[3],
    OutlineWidth = values[4],
    ShowSourceThumbnails = sourceThumbnails.IsChecked == true,
    ShowReceiverThumbnails = receiverThumbnails.IsChecked == true
   };
   var midiInterface = choice.SelectedValue as string ?? model.State.MainInterface;
   if (model.UpdateGeneralSettings(midiInterface, updated))
    DialogResult = true;
   else
    error.Text = model.Status;
  };
 }
}
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

 private CardCatalogWindow(string title, string emptyText, Func<IEnumerable<Entry>> entries){
  Title=title;Width=480;Height=520;MinWidth=360;MinHeight=320;WindowStartupLocation=WindowStartupLocation.CenterOwner;
  Background=new SolidColorBrush(Color.FromRgb(24,27,32));Foreground=Brushes.White;
  var grid=new Grid{Margin=new Thickness(22)};Content=grid;
  grid.RowDefinitions.Add(new(){Height=GridLength.Auto});grid.RowDefinitions.Add(new());
  var searchBox=new Grid{Margin=new Thickness(0,0,0,14)};
  var search=new TextBox{FontSize=14,Padding=new Thickness(34,9,9,9),ToolTip="Szukaj"};
  searchBox.Children.Add(search);
  var glass=new System.Windows.Shapes.Path{Data=Geometry.Parse("M 10,6 A 4,4 0 1 1 2,6 A 4,4 0 1 1 10,6 M 9,9 L 14,14"),Stroke=new SolidColorBrush(Color.FromRgb(148,161,175)),StrokeThickness=1.5,Width=16,Height=16,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(12,0,0,0),IsHitTestVisible=false};
  searchBox.Children.Add(glass);
  var hint=new TextBlock{Text="Szukaj",FontSize=14,Foreground=new SolidColorBrush(Color.FromRgb(148,161,175)),Margin=new Thickness(34,0,0,0),VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false};
  searchBox.Children.Add(hint);
  search.TextChanged+=(_,_)=>hint.Visibility=string.IsNullOrEmpty(search.Text)?Visibility.Visible:Visibility.Collapsed;
  grid.Children.Add(searchBox);
  var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,PanningMode=PanningMode.VerticalOnly};Grid.SetRow(scroll,1);grid.Children.Add(scroll);
  var list=new StackPanel();scroll.Content=list;
  void Refresh(){
   list.Children.Clear();
   foreach(var card in entries().Where(c=>c.Name.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase))){
    var row=new Grid{Margin=new Thickness(0,0,0,8)};
    row.ColumnDefinitions.Add(new());row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
    var entry=new Button{Content=card.Name,HorizontalContentAlignment=HorizontalAlignment.Left,FontSize=14,Margin=new Thickness(0,0,10,0)};
    var caption=new Grid();caption.ColumnDefinitions.Add(new());caption.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
    caption.Children.Add(new TextBlock{Text=card.Name,VerticalAlignment=VerticalAlignment.Center});
    if(card.IsPinned()){
     var pinIcon=new System.Windows.Shapes.Path{Data=Geometry.Parse("M 5,1 L 11,1 L 10,5 L 13,8 L 8,8 L 8,14 M 8,8 L 3,8 L 6,5 Z"),Stroke=Brushes.LightGray,StrokeThickness=1.4,Width=16,Height=16,Margin=new Thickness(12,0,0,0),VerticalAlignment=VerticalAlignment.Center,ToolTip="Przypięta"};
     Grid.SetColumn(pinIcon,1);caption.Children.Add(pinIcon);
    }
    entry.HorizontalContentAlignment=HorizontalAlignment.Stretch;entry.Content=caption;
    row.Children.Add(entry);
    SceneMenu.Attach(entry,card.IsPinned,card.Pin,card.Delete,Refresh,card.Export);
    entry.Click+=(_,_)=>{card.Select();DialogResult=true;};list.Children.Add(row);
   }
   if(list.Children.Count==0)list.Children.Add(new TextBlock{Text=emptyText,Foreground=Brushes.LightSlateGray});
  }
  search.TextChanged+=(_,_)=>Refresh();Refresh();Loaded+=(_,_)=>search.Focus();
 }
}





public static class SceneMenu
{
 public static void Attach(FrameworkElement target,RouterViewModel model,RoutingCard card,Action? refresh=null) =>
  Attach(target, () => card.IsPinned, pinned => model.SetCardPinned(card, pinned), () => model.DeleteCard(card), refresh,
   () => StudioFileDialogs.Export(Window.GetWindow(target), card.Name, () => StudioFiles.ExportScene(model, card)));

 public static void Attach(FrameworkElement target, Func<bool> isPinned, Action<bool> setPinned, Action? remove = null, Action? refresh = null, Action? export = null){
  var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(650)};
  Point origin=new();bool held=false;int touchId=-1;
  void Open(){
   var menu=new ContextMenu{PlacementTarget=target};
   var pin = new MenuItem { Header = isPinned() ? "Odepnij" : "Przypnij" };
   pin.Click += (_, _) => { setPinned(!isPinned()); refresh?.Invoke(); };
   menu.Items.Add(pin);
   if (remove != null)
   {
    var delete = new MenuItem { Header = "Usuń" };
    delete.Click += (_, _) => { remove(); refresh?.Invoke(); };
    menu.Items.Add(delete);
   }
   if (export != null)
   {
    var exportItem = new MenuItem { Header = "Eksportuj" };
    exportItem.Click += (_, _) => export();
    menu.Items.Add(exportItem);
   }
   menu.IsOpen = true;
  }
  target.PreviewMouseRightButtonUp+=(_,e)=>{Open();e.Handled=true;};
  System.Windows.Input.Stylus.SetIsPressAndHoldEnabled(target,false);
  target.PreviewTouchDown+=(_,e)=>{timer.Stop();origin=e.GetTouchPoint(target).Position;touchId=e.TouchDevice.Id;held=false;timer.Start();};
  target.PreviewTouchMove+=(_,e)=>{if(e.TouchDevice.Id==touchId&&(e.GetTouchPoint(target).Position-origin).Length>12)timer.Stop();};
  target.PreviewTouchUp+=(_,e)=>{timer.Stop();if(held){e.Handled=true;held=false;}};
  target.PreviewMouseLeftButtonUp+=(_,e)=>{if(held){e.Handled=true;held=false;}};
  timer.Tick+=(_,_)=>{timer.Stop();held=true;Open();};
  target.Unloaded+=(_,_)=>timer.Stop();
 }
}
