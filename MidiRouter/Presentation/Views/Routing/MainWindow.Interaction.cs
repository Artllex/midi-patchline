using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
namespace MidiRouter;

public partial class MainWindow
{
    private void RenameShown(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is TextBox box && box.IsVisible)
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => { if (box.IsVisible) { box.Focus(); box.SelectAll(); } }));
    }
    private void RenameFocusLost(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: Observable item })
            item.CancelRename();
    }
    private void RenameKey(object sender, KeyEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: Observable item })
            return;
        if (e.Key == Key.Escape)
        {
            item.CancelRename();
            e.Handled = true;
            return;
        }
        if (e.Key != Key.Enter)
            return;
        e.Handled = true;
        if (string.IsNullOrWhiteSpace(item.DraftName))
            return;
        if (item is Source source)
            Model.UpdateSource(source, item.DraftName, source.ImagePath);
        else if (item is Receiver receiver)
        {
            var endpoints = Model.State.Endpoints.Select(x => x.Copy()).ToList();
            endpoints.Single(x => x.Id == receiver.Endpoint.Id).Name = item.DraftName.Trim();
            Model.CommitSettings(endpoints, new());
        }
        item.CancelRename();
    }
    private List<Route>? receiverClickSnapshot;
    private string? clickSource, clickCard;
    private Receiver? clickReceiver;
    private void ReceiverClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: Receiver receiver })
            Model.ToggleReceiver.Execute(receiver);
    }
    private void ReceiverMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: Receiver receiver })
            return;
        if (e.ClickCount == 2)
        {
            if (receiverClickSnapshot != null && clickReceiver == receiver && clickSource == Model.State.SelectedSourceId && clickCard == Model.State.ActiveCardId)
                Model.RestoreClickRoutes(receiverClickSnapshot);
            receiverClickSnapshot = null;
            Model.ToggleSolo(receiver);
            e.Handled = true;
            return;
        }
        receiverClickSnapshot = Model.EditingRoutes.Select(route => new Route { SourceId = route.SourceId, ReceiverId = route.ReceiverId, Octaves = route.Octaves }).ToList();
        clickSource = Model.State.SelectedSourceId;
        clickCard = Model.State.ActiveCardId;
        clickReceiver = receiver;
    }
    private void OpenSplit(Source source)
    {
        Model.EditSplit(source);
        var editor = new SplitEditor(Model, source, CloseSplitView);
        SplitHost.Content = editor;
        SplitProfileHost.Content = editor.ProfileBar;
        SplitReceiverActions.Content = editor.ReceiverHeadingActions;
        SplitSourceName.Text = source.Name;
        SplitSourceName.ToolTip = source.Name;
        SourcePanel.Visibility = SceneBar.Visibility = AddReceiverButton.Visibility = Visibility.Collapsed;
        SplitHost.Visibility = SplitBar.Visibility = SplitSourceName.Visibility = SplitReceiverActions.Visibility = Visibility.Visible;
    }

    private void CloseSplitView()
    {
        Model.CloseSplit();
        SplitHost.Content = SplitProfileHost.Content = SplitReceiverActions.Content = null;
        SplitHost.Visibility = SplitBar.Visibility = SplitSourceName.Visibility = SplitReceiverActions.Visibility = Visibility.Collapsed;
        SourcePanel.Visibility = SceneBar.Visibility = AddReceiverButton.Visibility = Visibility.Visible;
    }
    private void SplitClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: Source source } button)
        {
            if (button.Tag?.ToString() == "held")
            {
                button.Tag = "ready";
                return;
            }
            OpenSplit(source);
        }
    }
    private void SplitButtonLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement button || button.Tag?.ToString() == "ready")
            return;
        button.Tag = "ready";
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(650) };
        Point origin = new();
        bool pressing = false;
        void Begin(Point point)
        {
            button.Tag = "ready";
            origin = point;
            pressing = true;
            timer.Start();
            holdTimer.Stop();
        }
        button.PreviewMouseLeftButtonDown += (_, args) => { if (args.StylusDevice == null) Begin(args.GetPosition(button)); };
        button.PreviewTouchDown += (_, args) => Begin(args.GetTouchPoint(button).Position);
        button.PreviewMouseMove += (_, args) => { if ((args.GetPosition(button) - origin).Length > 12) { timer.Stop(); pressing = false; } };
        button.PreviewTouchMove += (_, args) => { if ((args.GetTouchPoint(button).Position - origin).Length > 12) { timer.Stop(); pressing = false; } };
        button.PreviewMouseLeftButtonUp += (_, _) => { timer.Stop(); pressing = false; };
        button.PreviewTouchUp += (_, _) => { timer.Stop(); pressing = false; };
        button.Unloaded += (_, _) => timer.Stop();
        timer.Tick += (_, _) => { timer.Stop(); if (pressing && button.DataContext is Source source) { button.Tag = "held"; OpenSplit(source); } };
    }
    private void SourceMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is FrameworkElement { DataContext: Source source })
        {
            Model.ChooseSource(source, true);
            e.Handled = true;
        }
    }

}
