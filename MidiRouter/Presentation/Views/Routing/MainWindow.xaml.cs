using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
namespace MidiRouter;

public partial class MainWindow : Window
{
    public RouterViewModel Model { get; }
    public MainWindow() : this(new RouterViewModel()) { }

    public MainWindow(RouterViewModel model)
    {
        Model = model;
        InitializeComponent();
        DataContext = Model;
        AddHandler(UIElement.PreviewTouchMoveEvent, new EventHandler<System.Windows.Input.TouchEventArgs>(TouchMoved), true);
        AddHandler(UIElement.PreviewTouchUpEvent, new EventHandler<System.Windows.Input.TouchEventArgs>(TouchEnded), true);
        PreviewKeyDown += (_, _) => UpdateControlSelection();
        PreviewKeyUp += (_, _) => UpdateControlSelection();
        Activated += (_, _) => UpdateControlSelection();
        Deactivated += (_, _) => Model.SetControlSelection(false);
        Model.PropertyChanged += (_, args) => { if (args.PropertyName == "ActiveCard" && SplitHost.Visibility == Visibility.Visible) { CloseSplitView(); } };

        Closing += (_, _) => Model.Save();
        Loaded += (_, _) => { RenderReceiverSections(); RestoreSectionOrder(); };
        Model.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(Model.ReceiverTags) && IsLoaded) RenderReceiverSections(); };
    }
    private void UpdateControlSelection() => Model.SetControlSelection((Keyboard.Modifiers & ModifierKeys.Control) != 0);
    private void SceneLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: RoutingCard card } target && target.Tag?.ToString() != "scene-menu")
        {
            target.Tag = "scene-menu";
            SceneMenu.Attach(target, Model, card);
        }
    }
    private void NewCard(object sender, RoutedEventArgs e) => Model.CreateCard(false);
    private void SaveCard(object sender, RoutedEventArgs e)
    {
        new RenameWindow(Model.ActiveCard.Name, name =>
        {
            var previous = Model.ActiveCard.Name;
            Model.ActiveCard.Name = name;
            if (!Model.SaveCard())
            {
                Model.ActiveCard.Name = previous;
                throw new InvalidOperationException("Nie udało się zapisać presetu.");
            }
        })
        {
            Owner = this,
            Title = "Zapisz preset"
        }.ShowDialog();
    }
    private void OpenCatalog(object sender, RoutedEventArgs e) => new CardCatalogWindow(Model) { Owner = this }.ShowDialog();
    private void AddSource(object sender, RoutedEventArgs e)
    {
        var draft = new Source { Id = Guid.NewGuid().ToString("N"), Name = "" };
        new SourceSettingsWindow(Model, draft) { Owner = this, Title = "Dodaj źródło" }.ShowDialog();
    }
    private void AddReceiver(object sender, RoutedEventArgs e) => new SettingsWindow(Model, adding: true) { Owner = this }.ShowDialog();
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NativeTheme.Apply(this);
    }
    protected override void OnClosed(EventArgs e)
    {
        wheelTimer.Stop();
        holdTimer.Stop();
        base.OnClosed(e);
    }
}
