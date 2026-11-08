using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
namespace MidiRouter;
public partial class MainWindow : Window
{
 public RouterViewModel Model {get;}=new();
 public MainWindow(){InitializeComponent();DataContext=Model;Closing+=(_,_)=>Model.Save();Loaded+=(_,_)=>RestoreSectionOrder();}
 private readonly DispatcherTimer wheelTimer=new(){Interval=TimeSpan.FromMilliseconds(16)};
 private ScrollViewer? wheelViewer;private double wheelTarget;private long wheelLastTick;
 private void SmoothWheel(object sender,MouseWheelEventArgs e){
  var viewer=(ScrollViewer)sender;if(viewer.ScrollableHeight<=0)return;
  if(!wheelTimer.IsEnabled||wheelViewer!=viewer){wheelTarget=viewer.VerticalOffset;wheelTimer.Tick-=WheelTick;wheelTimer.Tick+=WheelTick;}
  wheelViewer=viewer;wheelTarget=Math.Clamp(wheelTarget-e.Delta/120.0*36,Math.Max(0,viewer.VerticalOffset-180),Math.Min(viewer.ScrollableHeight,viewer.VerticalOffset+180));
  wheelLastTick=Environment.TickCount64;wheelTimer.Start();e.Handled=true;
 }
 private void WheelTick(object? sender,EventArgs e){
  if(wheelViewer==null||!IsVisible){wheelTimer.Stop();return;}var now=Environment.TickCount64;double dt=Math.Clamp(now-wheelLastTick,1,64);wheelLastTick=now;
  wheelTarget=Math.Clamp(wheelTarget,0,wheelViewer.ScrollableHeight);double remaining=wheelTarget-wheelViewer.VerticalOffset;
  if(Math.Abs(remaining)<.4){wheelViewer.ScrollToVerticalOffset(wheelTarget);wheelTimer.Stop();return;}
  wheelViewer.ScrollToVerticalOffset(wheelViewer.VerticalOffset+remaining*(1-Math.Exp(-dt/55)));
 }
 private void StopWheel(object sender,MouseButtonEventArgs e)=>wheelTimer.Stop();
 private readonly DispatcherTimer holdTimer=new(){Interval=TimeSpan.FromMilliseconds(650)};
 private FrameworkElement? holdItem;private Point holdOrigin;private bool held;private int holdTouchId;
 private void TouchStarted(object sender,TouchEventArgs e){holdTimer.Stop();wheelTimer.Stop();held=false;holdItem=(FrameworkElement)sender;holdOrigin=e.GetTouchPoint(this).Position;holdTouchId=e.TouchDevice.Id;holdTimer.Tick-=HoldElapsed;holdTimer.Tick+=HoldElapsed;holdTimer.Start();}
 private void TouchMoved(object sender,TouchEventArgs e){if(e.TouchDevice.Id==holdTouchId&&holdItem!=null&&(e.GetTouchPoint(this).Position-holdOrigin).Length>12){holdTimer.Stop();holdItem=null;}}
 private void TouchEnded(object sender,TouchEventArgs e){if(e.TouchDevice.Id!=holdTouchId)return;holdTimer.Stop();holdItem=null;if(held){e.Handled=true;held=false;}}
 private void HoldElapsed(object? sender,EventArgs e){holdTimer.Stop();if(holdItem!=null){held=true;ShowItemMenu(holdItem);}}
 private void OpenItemMenu(object sender,MouseButtonEventArgs e){ShowItemMenu((FrameworkElement)sender);e.Handled=true;}
 private void ShowItemMenu(FrameworkElement item){
  var menu=new ContextMenu();
  void Add(string name,Action action){var entry=new MenuItem{Header=name};entry.Click+=(_,_)=>action();menu.Items.Add(entry);}
  if(item.DataContext is Source source){
   Add("Wybór pojedynczy",()=>Model.ChooseSource(source,false));Add("Wybór rozszerzony",()=>Model.ChooseSource(source,true));
   Add("Ustawienia",()=>new SourceSettingsWindow(Model,source){Owner=this}.ShowDialog());
   Add("Zmień nazwę",()=>source.BeginRename(source.Name));
  }else if(item.DataContext is Receiver receiver){
   Add(receiver.IsRouted?"Odznacz":"Wybierz",()=>Model.ToggleReceiver.Execute(receiver));
   Add("Ustawienia",()=>new SettingsWindow(Model,receiver.Endpoint.Id){Owner=this}.ShowDialog());
   Add(receiver.Endpoint.Hidden?"Pokaż":"Ukryj",()=>{var endpoints=Model.State.Endpoints.Select(e=>e.Copy()).ToList();endpoints.Single(e=>e.Id==receiver.Endpoint.Id).Hidden=!receiver.Endpoint.Hidden;Model.CommitSettings(endpoints,new());});
   Add("Zmień nazwę",()=>receiver.BeginRename(receiver.Name));
  }
  menu.PlacementTarget=item;menu.IsOpen=true;
 }
 private Expander? movingSection;
 private FrameworkElement? sectionGrip;
 private Point sectionStart;
 private bool sectionDragged;
 private static Expander? FindSection(DependencyObject? item) {
  while(item!=null){if(item is Expander section)return section;item=System.Windows.Media.VisualTreeHelper.GetParent(item);}
  return null;
 }
 private void RestoreSectionOrder(){
  var sections=ReceiverSections.Children.OfType<Expander>().ToList();
  var order=Model.State.SectionOrder??new();
  var sorted=sections.OrderBy(s=>{int index=order.IndexOf(s.Header.ToString()!);return index<0?int.MaxValue:index;}).ToList();
  ReceiverSections.Children.Clear();foreach(var section in sorted)ReceiverSections.Children.Add(section);
 }
 private void StartSection(FrameworkElement grip,Point point){
  wheelTimer.Stop();holdTimer.Stop();movingSection=FindSection(grip);sectionGrip=grip;sectionStart=point;sectionDragged=false;
 }
 private void MoveSection(Point point){
  if(movingSection==null || movingSection.Tag?.ToString()=="hidden")return;
  if((point-sectionStart).Length>8){sectionDragged=true;movingSection.Opacity=.55;}
 }
 private void FinishSection(Point point){
  var section=movingSection;if(section==null)return;movingSection=null;section.Opacity=1;
  if(!sectionDragged){section.IsExpanded=!section.IsExpanded;return;}
  if(section.Tag?.ToString()=="source"){
   var sourceOthers=VisualExpanders(SourceSectionList).Where(s=>s!=section).ToList();int position=sourceOthers.Count;
   for(int i=0;i<sourceOthers.Count;i++)if(point.Y<sourceOthers[i].TranslatePoint(new Point(0,20),this).Y){position=i;break;}
   Model.ReorderSourceSection(section.Header.ToString()!,position);return;
  }
  var others=ReceiverSections.Children.OfType<Expander>().Where(s=>s!=section && s.Tag?.ToString()!="hidden").ToList();
  int index=others.Count;
  for(int i=0;i<others.Count;i++){
   var header=others[i].TranslatePoint(new Point(0,20),this);
   if(point.Y<header.Y){index=i;break;}
  }
  ReceiverSections.Children.Remove(section);ReceiverSections.Children.Insert(index,section);
  Model.State.SectionOrder=ReceiverSections.Children.OfType<Expander>().Where(s=>s.Tag?.ToString()!="hidden").Select(s=>s.Header.ToString()!).ToList();Model.Save();
 }
 private static IEnumerable<Expander> VisualExpanders(DependencyObject root){
  for(int i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);i++){
   var child=System.Windows.Media.VisualTreeHelper.GetChild(root,i);
   if(child is Expander expander && expander.Tag?.ToString()=="source")yield return expander;
   else foreach(var nested in VisualExpanders(child))yield return nested;
  }
 }
 private void SectionMouseDown(object sender,MouseButtonEventArgs e){
  if(e.StylusDevice!=null)return;StartSection((FrameworkElement)sender,e.GetPosition(this));sectionGrip?.CaptureMouse();e.Handled=true;
 }
 private void SectionMouseMove(object sender,MouseEventArgs e){if(movingSection!=null&&e.LeftButton==MouseButtonState.Pressed){MoveSection(e.GetPosition(this));e.Handled=true;}}
 private void SectionMouseUp(object sender,MouseButtonEventArgs e){if(movingSection==null)return;sectionGrip?.ReleaseMouseCapture();FinishSection(e.GetPosition(this));e.Handled=true;}
 private void SectionTouchDown(object sender,TouchEventArgs e){StartSection((FrameworkElement)sender,e.GetTouchPoint(this).Position);sectionGrip?.CaptureTouch(e.TouchDevice);e.Handled=true;}
 private void SectionTouchMove(object sender,TouchEventArgs e){if(movingSection!=null){MoveSection(e.GetTouchPoint(this).Position);e.Handled=true;}}
 private void SectionTouchUp(object sender,TouchEventArgs e){if(movingSection==null)return;sectionGrip?.ReleaseTouchCapture(e.TouchDevice);FinishSection(e.GetTouchPoint(this).Position);e.Handled=true;}
 private void RenameShown(object sender,DependencyPropertyChangedEventArgs e){
  if(sender is TextBox box&&box.IsVisible)Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,new Action(()=>{if(box.IsVisible){box.Focus();box.SelectAll();}}));
 }
 private void RenameFocusLost(object sender,KeyboardFocusChangedEventArgs e){if(sender is FrameworkElement{DataContext:Observable item})item.CancelRename();}
 private void RenameKey(object sender,KeyEventArgs e){
  if(sender is not FrameworkElement{DataContext:Observable item})return;
  if(e.Key==Key.Escape){item.CancelRename();e.Handled=true;return;}
  if(e.Key!=Key.Enter)return;e.Handled=true;if(string.IsNullOrWhiteSpace(item.DraftName))return;
  if(item is Source source)Model.UpdateSource(source,item.DraftName,source.ImagePath);
  else if(item is Receiver receiver){var endpoints=Model.State.Endpoints.Select(x=>x.Copy()).ToList();endpoints.Single(x=>x.Id==receiver.Endpoint.Id).Name=item.DraftName.Trim();Model.CommitSettings(endpoints,new());}
  item.CancelRename();
 }
 private void SourceMouseDown(object sender,MouseButtonEventArgs e){if(e.ClickCount==2&&sender is FrameworkElement{DataContext:Source source}){Model.ChooseSource(source,true);e.Handled=true;}}
 private void OpenGeneralSettings(object sender,RoutedEventArgs e)=>new GeneralSettingsWindow(Model){Owner=this}.ShowDialog();
 private void NewCard(object sender,RoutedEventArgs e)=>Model.CreateCard(false);
 private void CopyCard(object sender,RoutedEventArgs e)=>Model.CreateCard(true);
 private void SaveCard(object sender,RoutedEventArgs e)
 {
  new RenameWindow(Model.ActiveCard.Name,name=>{
   var previous=Model.ActiveCard.Name;
   Model.ActiveCard.Name=name;
   if(!Model.SaveCard()){Model.ActiveCard.Name=previous;throw new InvalidOperationException("Nie udało się zapisać presetu.");}
  }){Owner=this,Title="Zapisz preset"}.ShowDialog();
 }
 private void OpenCatalog(object sender,RoutedEventArgs e)=>new CardCatalogWindow(Model){Owner=this}.ShowDialog();
 private void AddSource(object sender,RoutedEventArgs e){
  var draft=new Source{Id=Guid.NewGuid().ToString("N"),Name=""};
  new SourceSettingsWindow(Model,draft){Owner=this,Title="Dodaj źródło"}.ShowDialog();
 }
 private void AddReceiver(object sender,RoutedEventArgs e)=>new SettingsWindow(Model,adding:true){Owner=this}.ShowDialog();
 private void OpenTags(object sender,RoutedEventArgs e){
  var menu=new ContextMenu();var all=new MenuItem{Header="Wszystkie tagi"};all.Click+=(_,_)=>{Model.SelectedTags.Clear();Model.RefreshFilter();};menu.Items.Add(all);
  foreach(var tag in Model.AllTags){var entry=new MenuItem{Header=tag,IsCheckable=true,IsChecked=Model.SelectedTags.Contains(tag),StaysOpenOnClick=true};entry.Click+=(_,_)=>{if(entry.IsChecked)Model.SelectedTags.Add(tag);else Model.SelectedTags.Remove(tag);Model.RefreshFilter();};menu.Items.Add(entry);}
  menu.PlacementTarget=(UIElement)sender;menu.IsOpen=true;
 }
 private void MinimizeWindow(object sender,RoutedEventArgs e)=>SystemCommands.MinimizeWindow(this);
 private void MaximizeWindow(object sender,RoutedEventArgs e){if(WindowState==WindowState.Maximized)SystemCommands.RestoreWindow(this);else SystemCommands.MaximizeWindow(this);}
 private void CloseWindow(object sender,RoutedEventArgs e)=>SystemCommands.CloseWindow(this);
 protected override void OnStateChanged(EventArgs e){
  base.OnStateChanged(e);
 }
 protected override void OnSourceInitialized(EventArgs e){base.OnSourceInitialized(e);NativeTheme.Apply(this);}
 protected override void OnClosed(EventArgs e){wheelTimer.Stop();holdTimer.Stop();base.OnClosed(e);}
}








