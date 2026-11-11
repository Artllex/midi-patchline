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

        var layout = new DockPanel { Margin = new Thickness(22), LayoutTransform = new ScaleTransform(.85, .85) };
        Content = layout;
        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        DockPanel.SetDock(footer, Dock.Bottom);
        layout.Children.Add(footer);
        footer.Children.Add(new Button { Content = "Anuluj", IsCancel = true, FontSize = 13, Margin = new Thickness(0, 0, 8, 0) });
        var apply = new Button { Content = "Zastosuj", FontSize = 14, Margin = new Thickness(0, 0, 8, 0) };
        footer.Children.Add(apply);
        var save = new Button { Content = "Zapisz", IsDefault = true, FontSize = 13 };
        footer.Children.Add(save);
        SettingsFooter.Attach(footer);

        var error = new TextBlock { Foreground = Brushes.Salmon, TextWrapping = TextWrapping.Wrap };
        DockPanel.SetDock(error, Dock.Bottom);
        layout.Children.Add(error);
        var tabs = new TabControl { FontSize = 14 };
        layout.Children.Add(tabs);
        StackPanel Tab(string title)
        {
            var content = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
            tabs.Items.Add(new TabItem
            {
                Header = title,
                Content = new ScrollViewer
                {
                    Content = content,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    PanningMode = PanningMode.VerticalOnly
                }
            });
            return content;
        }
        var general = Tab("Ogólne");
        var panel = Tab("Interfejs");
        var appearance = Tab("Wygląd");
        general.Children.Add(new TextBlock { Text = "Język", FontSize = 16, Margin = new Thickness(0, 0, 0, 8) });
        var language = new ComboBox { ItemsSource = L.LanguageLabels, SelectedIndex = Math.Max(0, Array.IndexOf(L.LanguageCodes, model.State.Language)), Width = 230, HorizontalAlignment = HorizontalAlignment.Left };
        general.Children.Add(language);
        var sources = new OrganizationEditor("Źródła", model.State.VisibleSourceIds.Select(id => model.Sources.FirstOrDefault(source => source.Id == id))
            .OfType<Source>().Select(source => new OrganizationEditor.Item { Id = source.Id, Name = source.Name, Visible = !model.State.HiddenSourceIds.Contains(source.Id) }));
        var receivers = new OrganizationEditor("Odbiorcy", model.State.Endpoints.Select(endpoint =>
            new OrganizationEditor.Item { Id = endpoint.Id, Name = endpoint.Name, Visible = !endpoint.Hidden }));
        var sourceTags = new OrganizationEditor("Tagi źródeł", model.Sources.Where(source => model.State.VisibleSourceIds.Contains(source.Id))
            .SelectMany(source => source.Tags.Count == 0 ? new[] { "Bez tagów" } : source.Tags.AsEnumerable()).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => { int i = model.State.SourceSectionOrder.IndexOf(tag); return i < 0 ? int.MaxValue : i; })
            .Select(tag => new OrganizationEditor.Item { Id = tag, Name = tag, Visible = !model.State.HiddenSourceTags.Contains(tag) }));
        var receiverTags = new OrganizationEditor("Tagi odbiorców", model.State.Endpoints
            .SelectMany(endpoint => endpoint.Tags is { Count: > 0 } tags ? tags : new List<string> { "Bez tagów" }).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => { int i = model.State.SectionOrder.IndexOf(tag); return i < 0 ? int.MaxValue : i; })
            .Select(tag => new OrganizationEditor.Item { Id = tag, Name = tag, Visible = !model.State.HiddenReceiverTags.Contains(tag) }));
        sources.GroupBy(sourceTags, (item, tag) =>
        {
            var tags = model.Sources.Single(source => source.Id == item.Id).Tags;
            return tags.Count == 0 ? tag == "Bez tagów" : tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
        });
        receivers.GroupBy(receiverTags, (item, tag) =>
        {
            var tags = model.State.Endpoints.Single(endpoint => endpoint.Id == item.Id).Tags;
            return tags == null || tags.Count == 0 ? tag == "Bez tagów" : tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
        });
        general.Children.Add(new TextBlock { Text = "Layout", FontSize = 16, Margin = new Thickness(0, 18, 0, 8) });
        var organization = new TabControl { Margin = new Thickness(0), FontSize = 14 };
        var tagOrder = new StackPanel();
        tagOrder.Children.Add(sourceTags);
        tagOrder.Children.Add(receiverTags);
        var objectOrder = new StackPanel();
        objectOrder.Children.Add(sources);
        objectOrder.Children.Add(receivers);
        organization.Items.Add(new TabItem { Header = "Tagi", Content = tagOrder });
        organization.Items.Add(new TabItem { Header = "Obiekty", Content = objectOrder });
        organization.SelectedIndex = 0;
        general.Children.Add(organization);

        panel.Children.Add(new TextBlock { Text = "Główny interfejs MIDI", FontSize = 16, Margin = new Thickness(0, 0, 0, 8) });
        var options = MidiPorts.Outputs(model.State.MainInterface);
        options.RemoveAll(p => p.Value == "");
        if (!options.Any(p => p.Value == "Windows MIDI"))
            options.Insert(0, new()
            {
                Value = "Windows MIDI",
                Label = "Windows MIDI · wszystkie porty",
                Present = true
            });
        var choice = new ComboBox { ItemsSource = options, DisplayMemberPath = "Label", SelectedValuePath = "Value", SelectedValue = model.State.MainInterface };
        panel.Children.Add(choice);
        appearance.Children.Add(new TextBlock { Text = "Motyw kolorystyczny", FontSize = 16, Margin = new Thickness(0, 0, 0, 8) });
        var themeKeys = new[] { "Default", "Graphite", "Midnight", "Plum", "Light", "WhiteLight", "GrayLight" };
        var theme = new ComboBox
        {
            ItemsSource = new[] { "Domyślny", "Grafitowy", "Granatowy", "Śliwkowy", "Jasny", "Jasny biały", "Jasny szary" },
            SelectedIndex = Math.Max(0, Array.IndexOf(themeKeys, model.Graphics.Theme)),
            Width = 210,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 18)
        };
        appearance.Children.Add(theme);

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
            appearance.Children.Add(row);
            fields.Add((label, input, min, max));
        }

        AddNumber("Wysokość paneli źródeł", graphics.SourceHeight, 88, 300);
        AddNumber("Wysokość paneli odbiorców", graphics.ReceiverHeight, 44, 240);
        AddNumber("Odstęp pionowy", graphics.VerticalGap, 0, 64);
        AddNumber("Odstęp poziomy", graphics.HorizontalGap, 0, 64);
        AddNumber("Grubość obwódek", graphics.OutlineWidth, 0, 8);
        var sourceThumbnails = new CheckBox { Content = "Pokaż miniaturki źródeł", IsChecked = graphics.ShowSourceThumbnails, FontSize = 14 };
        var receiverThumbnails = new CheckBox { Content = "Pokaż miniaturki odbiorców", IsChecked = graphics.ShowReceiverThumbnails, FontSize = 14 };
        appearance.Children.Add(sourceThumbnails);
        appearance.Children.Add(receiverThumbnails);

        bool ApplyChanges()
        {
            var values = new int[fields.Count];
            for (int i = 0; i < fields.Count; i++)
            {
                var (label, box, min, max) = fields[i];
                if (!int.TryParse(box.Text, out values[i]) || values[i] < min || values[i] > max)
                {
                    error.Text = $"{label} — wpisz liczbę od {min} do {max}.";
                    tabs.SelectedIndex = 2;
                    box.Focus();
                    box.SelectAll();
                    return false;
                }
            }

            var updated = new GraphicsPreferences
            {
                Theme = themeKeys[Math.Max(0, theme.SelectedIndex)],
                SourceHeight = values[0],
                ReceiverHeight = values[1],
                VerticalGap = values[2],
                HorizontalGap = values[3],
                OutlineWidth = values[4],
                ShowSourceThumbnails = sourceThumbnails.IsChecked == true,
                ShowReceiverThumbnails = receiverThumbnails.IsChecked == true
            };
            var midiInterface = choice.SelectedValue as string ?? model.State.MainInterface;
            if (!model.UpdateGeneralSettings(midiInterface, updated, L.LanguageCodes[Math.Max(0, language.SelectedIndex)]))
            {
                error.Text = model.Status;
                return false;
            }
            var state = model.State;
            var oldSources = state.VisibleSourceIds;
            var oldHidden = state.HiddenSourceIds;
            var oldEndpoints = state.Endpoints;
            var oldSourceTags = state.SourceSectionOrder;
            var oldReceiverTags = state.SectionOrder;
            var oldHiddenSourceTags = state.HiddenSourceTags;
            var oldHiddenReceiverTags = state.HiddenReceiverTags;
            state.VisibleSourceIds = sources.Items.Select(item => item.Id).ToList();
            state.HiddenSourceIds = sources.Items.Where(item => !item.Visible).Select(item => item.Id).ToList();
            state.Endpoints = receivers.Items.Select(item => { var endpoint = oldEndpoints.Single(value => value.Id == item.Id).Copy(); endpoint.Hidden = !item.Visible; return endpoint; }).ToList();
            state.SourceSectionOrder = sourceTags.Items.Select(item => item.Id).ToList();
            state.SectionOrder = receiverTags.Items.Select(item => item.Id).ToList();
            state.HiddenSourceTags = sourceTags.Items.Where(item => !item.Visible).Select(item => item.Id).ToList();
            state.HiddenReceiverTags = receiverTags.Items.Where(item => !item.Visible).Select(item => item.Id).ToList();
            if (!model.Save())
            {
                state.VisibleSourceIds = oldSources; state.HiddenSourceIds = oldHidden; state.Endpoints = oldEndpoints;
                state.SourceSectionOrder = oldSourceTags; state.SectionOrder = oldReceiverTags;
                state.HiddenSourceTags = oldHiddenSourceTags; state.HiddenReceiverTags = oldHiddenReceiverTags;
                error.Text = model.Status;
                return false;
            }
            model.RefreshOrganization();
            error.Text = "";
            return true;
        }
        apply.Click += (_, _) => ApplyChanges();
        save.Click += (_, _) => { if (ApplyChanges()) DialogResult = true; };
    }
}
