using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
namespace MidiRouter;

public sealed partial class SplitEditor : UserControl
{
    readonly RouterViewModel model; readonly Source source;
    readonly StackPanel groups = new();
    readonly StackPanel pinnedProfiles = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
    readonly TextBlock status = new() { Foreground = Brushes.LightGray, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
    public Grid ProfileBar { get; } = new() { Height = 42 };
    public StackPanel ReceiverHeadingActions { get; } = new() { Orientation = Orientation.Horizontal };
    List<SplitProfile> Profiles => model.State.SplitProfiles[source.Id];

    public SplitEditor(RouterViewModel model, Source source, Action close)
    {
        this.model = model;
        this.source = source;
        DataContext = model;
        BuildProfileBar();

        var left = new DockPanel();
        Content = left;
        var back = new Button { Content = "← Routing", HorizontalAlignment = HorizontalAlignment.Stretch, Height = 40, Margin = new Thickness(0, 14, 0, 20), FontSize = 14 };
        back.Click += (_, _) => close();
        DockPanel.SetDock(back, Dock.Top);
        left.Children.Add(back);
        var add = new Button { Content = "+ Grupa", HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 12, 0, 10), FontSize = 14 };
        DockPanel.SetDock(add, Dock.Bottom);
        left.Children.Add(add);
        add.Click += (_, _) => { int number = 1; while (model.EditingSplit!.Groups.Any(item => item.Name == "Warstwa " + number)) number++; var g = new SplitGroup { Name = "Warstwa " + number }; model.EditingSplit.Groups.Add(g); model.SelectSplitGroup(g); model.Save(); RenderGroups(); };
        left.Children.Add(CreatePanelScroll(groups));
        var toggleProfile = new Button { Content = "Włącz / wyłącz", FontSize = 14, Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 0, 10, 0) };
        if (Application.Current.MainWindow?.TryFindResource("RectangularButton") is Style style)
            toggleProfile.Style = style;
        toggleProfile.Click += (_, _) => { model.ToggleSplit(source, model.EditingSplit!); Render(); };
        status.MaxWidth = 180;
        status.TextTrimming = TextTrimming.CharacterEllipsis;
        ReceiverHeadingActions.Children.Add(toggleProfile);
        ReceiverHeadingActions.Children.Add(status);
        Render();
    }
    static ScrollViewer CreatePanelScroll(object content)
    {
        var scroll = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        if (Application.Current.MainWindow?.TryFindResource("PanelScroll") is Style style)
            scroll.Style = style;
        return scroll;
    }

    void Render()
    {
        var profile = model.EditingSplit!;
        status.Text = profile.Name + (model.ActiveCard.SplitIds.GetValueOrDefault(source.Id) == profile.Id ? " · Włączony" : " · Wyłączony");
        RenderPinnedProfiles();
        RenderGroups();
    }
}
