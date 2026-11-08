using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
namespace MidiRouter;
public sealed class SplitEditor : UserControl {
 readonly RouterViewModel model;readonly Source source;
 readonly StackPanel groups=new();readonly ComboBox profiles=new(){Width=170,DisplayMemberPath="Name"};
 readonly TextBox profileName=new(){Width=150,FontSize=14};
 readonly TextBlock status=new(){Foreground=Brushes.LightGray,FontSize=12};
 bool refreshing;
 public SplitEditor(RouterViewModel model,Source source,DataTemplate receiverTemplate,Action close){
  this.model=model;this.source=source;DataContext=model;
  var root=new Grid{Margin=new Thickness(0,12,0,0)};Content=root;
  root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new());
  var top=new WrapPanel{Margin=new Thickness(0,0,24,14)};root.Children.Add(top);
  void ActionButton(string text,Action action){var b=new Button{Content=text,FontSize=13,Padding=new Thickness(12,7,12,7),Margin=new Thickness(0,0,10,0)};b.Click+=(_,_)=>action();top.Children.Add(b);}
  ActionButton("← Routing",close);
  top.Children.Add(new TextBlock{Text=source.Name,FontSize=20,Margin=new Thickness(0,0,16,0),VerticalAlignment=VerticalAlignment.Center});
  top.Children.Add(profiles);top.Children.Add(profileName);
  profileName.TextChanged+=(_,_)=>{if(!refreshing&&model.EditingSplit!=null){model.EditingSplit.Name=profileName.Text;model.Save();}};
  profiles.SelectionChanged+=(_,_)=>{if(!refreshing&&profiles.SelectedItem is SplitProfile p){model.EditSplit(source,p);Render();}};
  ActionButton("+ Profil",()=>{var p=new SplitProfile();model.State.SplitProfiles[source.Id].Add(p);model.EditSplit(source,p);Render();});
  ActionButton("Domyślny",()=>{model.State.DefaultSplitIds[source.Id]=model.EditingSplit!.Id;model.Save();Render();});
  ActionButton("Włącz / wyłącz",()=>{
   var id=model.EditingSplit!.Id;
   if(model.ActiveCard.SplitIds.GetValueOrDefault(source.Id)==id)model.ActiveCard.SplitIds.Remove(source.Id);else model.ActiveCard.SplitIds[source.Id]=id;
   model.Save();Render();
  });
  top.Children.Add(status);
  var body=new Grid();Grid.SetRow(body,1);root.Children.Add(body);
  body.ColumnDefinitions.Add(new(){Width=new GridLength(30,GridUnitType.Star)});body.ColumnDefinitions.Add(new(){Width=new GridLength(40)});body.ColumnDefinitions.Add(new(){Width=new GridLength(70,GridUnitType.Star)});
  var left=new DockPanel();body.Children.Add(left);
  var add=new Button{Content="+ Grupa",HorizontalAlignment=HorizontalAlignment.Stretch,Margin=new Thickness(0,12,0,10),FontSize=14};
  DockPanel.SetDock(add,Dock.Bottom);left.Children.Add(add);
  add.Click+=(_,_)=>{int number=1;while(model.EditingSplit!.Groups.Any(item=>item.Name=="Warstwa "+number))number++;var g=new SplitGroup{Name="Warstwa "+number};model.EditingSplit.Groups.Add(g);model.SelectSplitGroup(g);model.Save();RenderGroups();};
  left.Children.Add(new ScrollViewer{Content=groups,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,PanningMode=PanningMode.VerticalOnly});
  var right=new DockPanel();Grid.SetColumn(right,2);body.Children.Add(right);
  var title=new TextBlock{Text="ODBIORCY",FontSize=27,Margin=new Thickness(0,0,0,20)};DockPanel.SetDock(title,Dock.Top);right.Children.Add(title);
  var receiverSections=new StackPanel();
  foreach(var pair in new[]{("RACK","Rack"),("KEYBOARDS","Keyboards"),("ON TOP","OnTop")}){
   var items=new ItemsControl{ItemTemplate=receiverTemplate};
   items.SetBinding(ItemsControl.ItemsSourceProperty,pair.Item2);
   var factory=new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.UniformGrid));factory.SetValue(System.Windows.Controls.Primitives.UniformGrid.ColumnsProperty,2);
   items.ItemsPanel=new ItemsPanelTemplate(factory);
   receiverSections.Children.Add(new Expander{Header=pair.Item1,IsExpanded=true,Content=items,Style=Application.Current.MainWindow?.TryFindResource("TagSection") as Style});
  }
  right.Children.Add(new ScrollViewer{Content=receiverSections,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,PanningMode=PanningMode.VerticalOnly});
  Render();
 }
 void Render(){
  refreshing=true;profiles.ItemsSource=null;profiles.ItemsSource=model.State.SplitProfiles[source.Id];profiles.SelectedItem=model.EditingSplit;profileName.Text=model.EditingSplit!.Name;refreshing=false;
  status.Text=(model.State.DefaultSplitIds.GetValueOrDefault(source.Id)==model.EditingSplit.Id?"Domyślny · ":"")+(model.ActiveCard.SplitIds.GetValueOrDefault(source.Id)==model.EditingSplit.Id?"Włączony":"Wyłączony");
  RenderGroups();
 }
 public static string NoteName(int n)=>new[]{"C","C#","D","D#","E","F","F#","G","G#","A","A#","B"}[n%12]+(n/12-1);
 public static bool TryNote(string text,out int note){
  note=0;text=text.Trim().ToUpperInvariant().Replace("♯","#").Replace("♭","b");
  if(text.Length<2)return false;
  int pitch=text[0] switch {'C'=>0,'D'=>2,'E'=>4,'F'=>5,'G'=>7,'A'=>9,'B'=>11,_=>-100};
  if(pitch<0)return false;
  int index=1;
  if(text[index]=='#'){pitch++;index++;}else if(text[index]=='B'||text[index]=='b'){pitch--;index++;}
  if(!int.TryParse(text[index..],out var octave))return false;
  note=(octave+1)*12+pitch;return note>=0&&note<=127;
 }
 static string Note(int n)=>new[]{"C","C♯","D","D♯","E","F","F♯","G","G♯","A","A♯","B"}[n%12]+(n/12-1)+" · "+n;
 void RenderGroups(){
  groups.Children.Clear();
  foreach(var group in model.EditingSplit!.Groups){
   var panel=new StackPanel{Margin=new Thickness(12)};
   var border=new Border{Background=new SolidColorBrush(Color.FromRgb(38,43,50)),CornerRadius=new CornerRadius(14),Margin=new Thickness(0,0,0,10),BorderThickness=new Thickness(2),BorderBrush=group==model.EditingGroup?new SolidColorBrush(Color.FromRgb(53,203,125)):Brushes.Transparent,Child=panel};
   groups.Children.Add(border);
   var select=new Button{Content=group.Name,HorizontalContentAlignment=HorizontalAlignment.Left,FontSize=16,Background=Brushes.Transparent,Padding=new Thickness(0,4,0,4)};
   var titleTemplate=new ControlTemplate(typeof(Button));
   var titleSurface=new FrameworkElementFactory(typeof(Border));
   titleSurface.SetValue(Border.BackgroundProperty,Brushes.Transparent);
   titleSurface.SetValue(Border.PaddingProperty,new Thickness(0,4,0,4));
   var titleContent=new FrameworkElementFactory(typeof(ContentPresenter));
   titleContent.SetValue(ContentPresenter.HorizontalAlignmentProperty,HorizontalAlignment.Left);
   titleContent.SetBinding(ContentPresenter.ContentProperty,new System.Windows.Data.Binding("Content"){RelativeSource=System.Windows.Data.RelativeSource.TemplatedParent});
   titleSurface.AppendChild(titleContent);titleTemplate.VisualTree=titleSurface;select.Template=titleTemplate;
   select.Click+=(_,_)=>{model.SelectSplitGroup(group);RenderGroups();};panel.Children.Add(select);
   var nameHost=new Grid{Height=32};
   panel.Children.Remove(select);panel.Children.Add(nameHost);nameHost.Children.Add(select);
   select.Padding=new Thickness(0);select.VerticalAlignment=VerticalAlignment.Center;
   var rename=new TextBox{Text=group.Name,FontSize=16,Visibility=Visibility.Collapsed,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(0),VerticalAlignment=VerticalAlignment.Center};
   var editTemplate=new ControlTemplate(typeof(TextBox));
   var editContent=new FrameworkElementFactory(typeof(ScrollViewer));editContent.Name="PART_ContentHost";
   editTemplate.VisualTree=editContent;rename.Template=editTemplate;rename.FocusVisualStyle=null;
   nameHost.Children.Add(rename);
   bool cancelRename=false;
   void FinishRename(){
    if(rename.Visibility!=Visibility.Visible)return;
    if(!cancelRename&&!string.IsNullOrWhiteSpace(rename.Text)){group.Name=rename.Text.Trim();select.Content=group.Name;model.Save();}
    rename.Visibility=Visibility.Collapsed;select.Visibility=Visibility.Visible;cancelRename=false;
   }
   rename.KeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Enter){FinishRename();e.Handled=true;}else if(e.Key==System.Windows.Input.Key.Escape){cancelRename=true;FinishRename();e.Handled=true;}};
   rename.LostKeyboardFocus+=(_,_)=>FinishRename();
   void OpenMenu(){
    var menu=new ContextMenu{PlacementTarget=border};
    var change=new MenuItem{Header="Zmień nazwę"};change.Click+=(_,_)=>{select.Visibility=Visibility.Collapsed;rename.Text=group.Name;rename.Visibility=Visibility.Visible;rename.Dispatcher.BeginInvoke(new Action(()=>{rename.Focus();rename.SelectAll();}));};
    var delete=new MenuItem{Header="Usuń",IsEnabled=model.EditingSplit!.Groups.Count>1};
    delete.Click+=(_,_)=>{model.EditingSplit.Groups.Remove(group);model.SelectSplitGroup(model.EditingSplit.Groups[0]);model.Save();RenderGroups();};
    menu.Items.Add(change);menu.Items.Add(delete);menu.IsOpen=true;
   }
   border.PreviewMouseRightButtonUp+=(_,e)=>{OpenMenu();e.Handled=true;};
   var hold=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(650)};
   Point touchOrigin=new();bool held=false;
   System.Windows.Input.Stylus.SetIsPressAndHoldEnabled(border,false);
   border.PreviewTouchDown+=(_,e)=>{touchOrigin=e.GetTouchPoint(border).Position;held=false;hold.Start();};
   border.PreviewTouchMove+=(_,e)=>{if((e.GetTouchPoint(border).Position-touchOrigin).Length>12)hold.Stop();};
   border.PreviewTouchUp+=(_,e)=>{hold.Stop();if(held)e.Handled=true;};
   border.Unloaded+=(_,_)=>hold.Stop();
   hold.Tick+=(_,_)=>{hold.Stop();held=true;OpenMenu();};

   panel.Children.Add(new TextBlock{Text="Typ",FontSize=12,Foreground=Brushes.LightSlateGray,Margin=new Thickness(0,4,0,4)});
   var type=new ComboBox{ItemsSource=SplitGroup.Types,SelectedItem=group.Type,MaxDropDownHeight=360};
   type.SelectionChanged+=(_,_)=>{group.Type=type.SelectedItem?.ToString()??"Zakres klawiszy";model.Save();RenderGroups();};
   panel.Children.Add(type);
   var keyboard=new Canvas{Height=32,ClipToBounds=true,Margin=new Thickness(0,8,0,8)};keyboard.Visibility=group.Type=="Zakres klawiszy"?Visibility.Visible:Visibility.Collapsed;panel.Children.Add(keyboard);
   void Draw(){
    keyboard.Children.Clear();
    bool IsBlack(int n)=>new[]{1,3,6,8,10}.Contains(n%12);
    double width=keyboard.ActualWidth/Enumerable.Range(0,128).Count(n=>!IsBlack(n));
    int whiteIndex=0;
    for(int n=0;n<128;n++){
     if(IsBlack(n))continue;
     var key=new Rectangle{Width=Math.Max(.5,width-.35),Height=32,Fill=n>=group.Low&&n<=group.High?new SolidColorBrush(Color.FromRgb(53,203,125)):Brushes.LightGray};
     Canvas.SetLeft(key,whiteIndex++*width);Canvas.SetTop(key,0);keyboard.Children.Add(key);
    }
    whiteIndex=0;
    for(int n=0;n<128;n++){
     if(!IsBlack(n)){whiteIndex++;continue;}
     var key=new Rectangle{Width=Math.Max(.5,width*.62),Height=20,Fill=n>=group.Low&&n<=group.High?new SolidColorBrush(Color.FromRgb(22,110,67)):new SolidColorBrush(Color.FromRgb(24,27,32))};
     Canvas.SetLeft(key,(whiteIndex-.31)*width);Canvas.SetTop(key,0);keyboard.Children.Add(key);
    }
   }
   keyboard.SizeChanged+=(_,_)=>Draw();
   void Range(string label,int low,int high,int min,int max,Action<int,int> set,bool notes=false){
    panel.Children.Add(new TextBlock{Text=label,FontSize=12,Foreground=Brushes.LightSlateGray,Margin=new Thickness(0,6,0,4)});
    var row=new Grid();row.ColumnDefinitions.Add(new());row.ColumnDefinitions.Add(new(){Width=new GridLength(12)});row.ColumnDefinitions.Add(new());panel.Children.Add(row);
    if(!notes){
     var lowBox=new TextBox{Text=low.ToString(),FontSize=14};
     var highBox=new TextBox{Text=high.ToString(),FontSize=14};
     row.Children.Add(lowBox);Grid.SetColumn(highBox,2);row.Children.Add(highBox);
     void Validate(){
      bool valid=int.TryParse(lowBox.Text,out var x)&&x>=min&&x<=max;
      bool validHigh=int.TryParse(highBox.Text,out var y)&&y>=min&&y<=max;
      bool ordered=valid&&validHigh&&x<=y;
      lowBox.BorderBrush=highBox.BorderBrush=ordered?new SolidColorBrush(Color.FromRgb(69,69,69)):Brushes.Salmon;
      lowBox.ToolTip=highBox.ToolTip="Od "+min+" do "+max+"; dolna wartość nie może przekraczać górnej.";
      if(ordered){set(x,y);model.Save();}
     }
     lowBox.TextChanged+=(_,_)=>Validate();highBox.TextChanged+=(_,_)=>Validate();
     void Restore(){lowBox.Text=Math.Clamp(int.TryParse(lowBox.Text,out var x)?x:low,min,max).ToString();highBox.Text=Math.Clamp(int.TryParse(highBox.Text,out var y)?y:high,min,max).ToString();if(int.Parse(lowBox.Text)>int.Parse(highBox.Text))highBox.Text=lowBox.Text;Validate();}
     lowBox.LostKeyboardFocus+=(_,_)=>Restore();highBox.LostKeyboardFocus+=(_,_)=>Restore();
     return;
    }
    var from=new TextBox{Text=NoteName(low),FontSize=14};
    var to=new TextBox{Text=NoteName(high),FontSize=14};
    row.Children.Add(from);Grid.SetColumn(to,2);row.Children.Add(to);
    int savedLow=low,savedHigh=high;
    void UpdateNotes(){
     bool valid=TryNote(from.Text,out var x)&&TryNote(to.Text,out var y)&&x<=y;
     from.BorderBrush=to.BorderBrush=valid?new SolidColorBrush(Color.FromRgb(69,69,69)):Brushes.Salmon;
     if(valid){TryNote(to.Text,out var endNote);savedLow=x;savedHigh=endNote;set(savedLow,savedHigh);Draw();model.Save();}
    }
    from.ToolTip=to.ToolTip="Wpisz nutę, np. C1, C#4 lub G6.";
    from.TextChanged+=(_,_)=>UpdateNotes();to.TextChanged+=(_,_)=>UpdateNotes();
    void Finish(){from.Text=NoteName(savedLow);to.Text=NoteName(savedHigh);}
    from.LostKeyboardFocus+=(_,_)=>Finish();to.LostKeyboardFocus+=(_,_)=>Finish();

   }
   if(group.Type=="Zakres klawiszy"){
    var first=source.Details.FirstNote;
    var count=source.Details.KeyCount;
    string bounds=first.HasValue&&count.HasValue&&first.Value+count.Value<=128
     ?NoteName(first.Value)+"-"+NoteName(first.Value+count.Value-1):"C-1-G9 (MIDI)";
    Range("Zakres klawiszy: "+bounds,group.Low,group.High,0,127,(a,b)=>{group.Low=a;group.High=b;},true);
   }
   if(group.Type=="Velocity")Range("Velocity",group.VelocityLow,group.VelocityHigh,1,127,(a,b)=>{group.VelocityLow=a;group.VelocityHigh=b;});
   if(group.Type=="Modwheel")Range("Modwheel · CC1",group.ModLow,group.ModHigh,0,127,(a,b)=>{group.ModLow=a;group.ModHigh=b;});
   if(group.Type!="Zakres klawiszy"&&group.Type!="Velocity"&&group.Type!="Modwheel")
    Range(group.Type+" · od — do",group.ParameterRange[0],group.ParameterRange[1],group.ParameterMin,group.ParameterMax,(a,b)=>group.ParameterRanges[group.Type]=new[]{a,b});


  }
 }
}
