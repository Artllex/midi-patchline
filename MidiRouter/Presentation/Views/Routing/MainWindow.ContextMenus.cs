using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
namespace MidiRouter;

public partial class MainWindow
{
    private readonly DispatcherTimer holdTimer = new() { Interval = TimeSpan.FromMilliseconds(650) };
    private FrameworkElement? holdItem; private Point holdOrigin; private bool held; private int holdTouchId;
    private void TouchStarted(object sender, TouchEventArgs e)
    {
        holdTimer.Stop();
        wheelTimer.Stop();
        held = false;
        holdItem = (FrameworkElement)sender;
        holdOrigin = e.GetTouchPoint(this).Position;
        holdTouchId = e.TouchDevice.Id;
        holdTimer.Tick -= HoldElapsed;
        holdTimer.Tick += HoldElapsed;
        holdTimer.Start();
    }
    private void TouchMoved(object sender, TouchEventArgs e)
    {
        if (e.TouchDevice.Id == holdTouchId && holdItem != null && (e.GetTouchPoint(this).Position - holdOrigin).Length > 12)
        {
            holdTimer.Stop();
            holdItem = null;
        }
    }
    private void TouchEnded(object sender, TouchEventArgs e)
    {
        if (e.TouchDevice.Id != holdTouchId)
            return;
        holdTimer.Stop();
        holdItem = null;
        if (held)
        {
            e.Handled = true;
            held = false;
        }
    }
    private void HoldElapsed(object? sender, EventArgs e)
    {
        holdTimer.Stop();
        if (holdItem != null)
        {
            held = true;
            ShowItemMenu(holdItem);
        }
    }
    private void OpenItemMenu(object sender, MouseButtonEventArgs e)
    {
        ShowItemMenu((FrameworkElement)sender);
        e.Handled = true;
    }
    private void ShowItemMenu(FrameworkElement item)
    {
        var menu = new ContextMenu();
        void Add(string name, Action action)
        {
            var entry = new MenuItem { Header = name };
            entry.Click += (_, _) => action();
            menu.Items.Add(entry);
        }
        if (item.DataContext is Source source)
        {
            Add("Wybór pojedynczy", () => Model.ChooseSource(source, false));
            Add("Wybór rozszerzony", () => Model.ChooseSource(source, true));
            Add("Ustawienia", () => new SourceSettingsWindow(Model, source) { Owner = this }.ShowDialog());
            Add("Zmień nazwę", () => source.BeginRename(source.Name));
            Add("Usuń", () => { if (SplitHost.Visibility == Visibility.Visible) CloseSplitView(); Model.DeleteSource(source); });
        }
        else if (item.DataContext is Receiver receiver)
        {
            Add(receiver.IsRouted ? "Odznacz" : "Wybierz", () => Model.ToggleReceiver.Execute(receiver));
            Add("Ustawienia", () => new SettingsWindow(Model, receiver.Endpoint.Id) { Owner = this }.ShowDialog());
            Add(receiver.Endpoint.Hidden ? "Pokaż" : "Ukryj", () => { var endpoints = Model.State.Endpoints.Select(e => e.Copy()).ToList(); endpoints.Single(e => e.Id == receiver.Endpoint.Id).Hidden = !receiver.Endpoint.Hidden; Model.CommitSettings(endpoints, new()); });
            Add("Zmień nazwę", () => receiver.BeginRename(receiver.Name));
            Add("Usuń", () => Model.DeleteReceiver(receiver));
        }
        menu.PlacementTarget = item;
        menu.IsOpen = true;
    }

}
