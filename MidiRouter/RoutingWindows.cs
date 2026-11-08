using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MidiRouter;
public sealed class GeneralSettingsWindow : Window
{
 public GeneralSettingsWindow(RouterViewModel model){
  Title="Ustawienia";Width=430;Height=260;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;
  Background=new SolidColorBrush(Color.FromRgb(24,27,32));Foreground=Brushes.White;
  var panel=new StackPanel{Margin=new Thickness(22)};Content=panel;
  panel.Children.Add(new TextBlock{Text="Główny interfejs MIDI",FontSize=16,Margin=new Thickness(0,0,0,12)});
  var options=MidiPorts.Outputs(model.State.MainInterface);
  options.RemoveAll(p=>p.Value=="");
  if(!options.Any(p=>p.Value=="Windows MIDI"))options.Insert(0,new(){Value="Windows MIDI",Label="Windows MIDI · wszystkie porty",Present=true});
  var choice=new ComboBox{ItemsSource=options,DisplayMemberPath="Label",SelectedValuePath="Value",SelectedValue=model.State.MainInterface};panel.Children.Add(choice);
  var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,22,0,0)};panel.Children.Add(footer);
  footer.Children.Add(new Button{Content="Anuluj",IsCancel=true,FontSize=13,Padding=new Thickness(12,7,12,7),Margin=new Thickness(0,0,8,0)});
  var save=new Button{Content="Zapisz",IsDefault=true,FontSize=13,Padding=new Thickness(12,7,12,7)};footer.Children.Add(save);
  save.Click+=(_,_)=>{if(choice.SelectedValue is string value){model.SetMainInterface(value);DialogResult=true;}};
 }
}
public sealed class CardCatalogWindow : Window
{
 public CardCatalogWindow(RouterViewModel model){
  Title="Katalog";Width=480;Height=520;MinWidth=360;MinHeight=320;WindowStartupLocation=WindowStartupLocation.CenterOwner;
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
   foreach(var card in model.Cards.Where(c=>c.Name.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase))){
    var row=new Grid{Margin=new Thickness(0,0,0,8)};
    row.ColumnDefinitions.Add(new());row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
    var entry=new Button{Content=card.Name,HorizontalContentAlignment=HorizontalAlignment.Left,FontSize=14,Margin=new Thickness(0,0,10,0)};
    row.Children.Add(entry);
    var pin=new Button{Content=card.IsPinned?"Odepnij":"Przypnij",FontSize=13,Width=86,Padding=new Thickness(10,8,10,8),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,0,0,0),ToolTip="Przypnij lub odepnij zakładkę",Background=card.IsPinned?new SolidColorBrush(Color.FromRgb(35,74,56)):new SolidColorBrush(Color.FromRgb(38,43,50))};
    Grid.SetColumn(pin,1);row.Children.Add(pin);
    pin.Click+=(_,_)=>{model.SetCardPinned(card,!card.IsPinned);pin.Content=card.IsPinned?"Odepnij":"Przypnij";pin.Background=card.IsPinned?new SolidColorBrush(Color.FromRgb(35,74,56)):new SolidColorBrush(Color.FromRgb(38,43,50));};
    entry.Click+=(_,_)=>{model.ActivateCard(card.Id);DialogResult=true;};list.Children.Add(row);
   }
   if(list.Children.Count==0)list.Children.Add(new TextBlock{Text="Brak kart",Foreground=Brushes.LightSlateGray});
  }
  search.TextChanged+=(_,_)=>Refresh();Refresh();Loaded+=(_,_)=>search.Focus();
 }
}



