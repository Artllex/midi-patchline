using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MidiRouter;

public sealed partial class SplitEditor
{
    void RenderGroups()
    {
        groups.Children.Clear();
        foreach (var group in model.EditingSplit!.Groups)
        {
            var panel = new StackPanel { Margin = new Thickness(12) };
            var border = new Border { Background = new SolidColorBrush(Color.FromRgb(38, 43, 50)), CornerRadius = new CornerRadius(14), Margin = model.Graphics.SourceMargin, BorderThickness = model.Graphics.Outline, BorderBrush = group == model.EditingGroup ? new SolidColorBrush(Color.FromRgb(53, 203, 125)) : Brushes.Transparent, Child = panel };
            groups.Children.Add(border);
            var select = new Button { Content = group.Name, HorizontalContentAlignment = HorizontalAlignment.Left, FontSize = 16, Background = Brushes.Transparent, Padding = new Thickness(0, 4, 0, 4) };
            var titleTemplate = new ControlTemplate(typeof(Button));
            var titleSurface = new FrameworkElementFactory(typeof(Border));
            titleSurface.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            titleSurface.SetValue(Border.PaddingProperty, new Thickness(0, 4, 0, 4));
            var titleContent = new FrameworkElementFactory(typeof(ContentPresenter));
            titleContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            titleContent.SetBinding(ContentPresenter.ContentProperty, new System.Windows.Data.Binding("Content") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            titleSurface.AppendChild(titleContent);
            titleTemplate.VisualTree = titleSurface;
            select.Template = titleTemplate;
            select.ToolTip = group.IsExpanded ? "Zwiń grupę" : "Rozwiń grupę";
            select.Click += (_, _) =>
            {
                var previous = group.IsExpanded;
                group.IsExpanded = !previous;
                if (!model.Save())
                    group.IsExpanded = previous;
                model.SelectSplitGroup(group);
                RenderGroups();
            };
            panel.Children.Add(select);
            var nameHost = new Grid { Height = 32 };
            nameHost.ColumnDefinitions.Add(new ColumnDefinition());
            nameHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            panel.Children.Remove(select);
            panel.Children.Add(nameHost);
            nameHost.Children.Add(select);
            select.Padding = new Thickness(0);
            select.VerticalAlignment = VerticalAlignment.Center;
            var rename = new TextBox { Text = group.Name, FontSize = 16, Visibility = Visibility.Collapsed, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Center };
            var editTemplate = new ControlTemplate(typeof(TextBox));
            var editContent = new FrameworkElementFactory(typeof(ScrollViewer));
            editContent.Name = "PART_ContentHost";
            editTemplate.VisualTree = editContent;
            rename.Template = editTemplate;
            rename.FocusVisualStyle = null;
            nameHost.Children.Add(rename);
            var toggle = new Button { FontSize = 12, Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(8, 0, 0, 0), MinWidth = 54 };
            Grid.SetColumn(toggle, 1);
            nameHost.Children.Add(toggle);
            var settings = new StackPanel { Visibility = group.IsExpanded ? Visibility.Visible : Visibility.Collapsed };
            panel.Children.Add(settings);

            void UpdateGroupAppearance()
            {
                toggle.Content = group.IsEnabled ? "Wł." : "Wył.";
                toggle.ToolTip = group.IsEnabled ? "Wyłącz grupę" : "Włącz grupę";
                System.Windows.Automation.AutomationProperties.SetName(toggle, toggle.ToolTip.ToString()!);
                border.Opacity = group.IsEnabled ? 1 : .55;
                border.BorderBrush = group != model.EditingGroup ? Brushes.Transparent
                 : group.IsEnabled ? new SolidColorBrush(Color.FromRgb(53, 203, 125)) : Brushes.Gray;
                settings.Opacity = group.IsEnabled ? 1 : .75;
            }
            toggle.Click += (_, _) =>
            {
                model.SetSplitGroupEnabled(group, !group.IsEnabled);
                UpdateGroupAppearance();
            };
            UpdateGroupAppearance();
            bool cancelRename = false;
            void FinishRename()
            {
                if (rename.Visibility != Visibility.Visible)
                    return;
                if (!cancelRename && !string.IsNullOrWhiteSpace(rename.Text))
                {
                    group.Name = rename.Text.Trim();
                    select.Content = group.Name;
                    model.Save();
                }
                rename.Visibility = Visibility.Collapsed;
                select.Visibility = Visibility.Visible;
                cancelRename = false;
            }
            rename.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) { FinishRename(); e.Handled = true; } else if (e.Key == System.Windows.Input.Key.Escape) { cancelRename = true; FinishRename(); e.Handled = true; } };
            rename.LostKeyboardFocus += (_, _) => FinishRename();
            void OpenMenu()
            {
                var menu = new ContextMenu { PlacementTarget = border };
                var change = new MenuItem { Header = "Zmień nazwę" };
                change.Click += (_, _) => { select.Visibility = Visibility.Collapsed; rename.Text = group.Name; rename.Visibility = Visibility.Visible; rename.Dispatcher.BeginInvoke(new Action(() => { rename.Focus(); rename.SelectAll(); })); };
                var delete = new MenuItem { Header = "Usuń", IsEnabled = model.EditingSplit!.Groups.Count > 1 };
                delete.Click += (_, _) => { model.EditingSplit.Groups.Remove(group); model.SelectSplitGroup(model.EditingSplit.Groups[0]); model.Save(); RenderGroups(); };
                menu.Items.Add(change);
                menu.Items.Add(delete);
                menu.IsOpen = true;
            }
            border.PreviewMouseRightButtonUp += (_, e) => { OpenMenu(); e.Handled = true; };
            var hold = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(650) };
            Point touchOrigin = new();
            bool held = false;
            System.Windows.Input.Stylus.SetIsPressAndHoldEnabled(border, false);
            border.PreviewTouchDown += (_, e) => { touchOrigin = e.GetTouchPoint(border).Position; held = false; hold.Start(); };
            border.PreviewTouchMove += (_, e) => { if ((e.GetTouchPoint(border).Position - touchOrigin).Length > 12) hold.Stop(); };
            border.PreviewTouchUp += (_, e) => { hold.Stop(); if (held) e.Handled = true; };
            border.Unloaded += (_, _) => hold.Stop();
            hold.Tick += (_, _) => { hold.Stop(); held = true; OpenMenu(); };

            var type = new ComboBox { ItemsSource = SplitGroup.Types, SelectedItem = group.Type, MaxDropDownHeight = 360 };
            type.SelectionChanged += (_, _) => { group.Type = type.SelectedItem?.ToString() ?? "Zakres klawiszy"; model.Save(); RenderGroups(); };
            settings.Children.Add(type);
            var keyboard = new Canvas { Height = 32, ClipToBounds = true, Margin = new Thickness(0, 8, 0, 8) };
            keyboard.Visibility = group.Type == "Zakres klawiszy" ? Visibility.Visible : Visibility.Collapsed;
            settings.Children.Add(keyboard);
            void Draw() => SplitKeyboardPreview.Draw(keyboard, group.Low, group.High);
            keyboard.SizeChanged += (_, _) => Draw();
            void Range(string label, int low, int high, int min, int max, Action<int, int> set, bool notes = false)
            {
                TextBox Input(string text)
                {
                    var input = new TextBox { Text = text, FontSize = 14, FocusVisualStyle = null };
                    var template = new ControlTemplate(typeof(TextBox));
                    var surface = new FrameworkElementFactory(typeof(Border));
                    surface.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
                    surface.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
                    surface.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                    surface.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
                    var content = new FrameworkElementFactory(typeof(ScrollViewer)) { Name = "PART_ContentHost" };
                    surface.AppendChild(content);
                    template.VisualTree = surface;
                    input.Template = template;
                    return input;
                }
                void Mark(TextBox input, bool valid)
                {
                    input.BorderBrush = valid
                     ? new SolidColorBrush(input.IsKeyboardFocused ? Color.FromRgb(86, 157, 229) : Color.FromRgb(69, 69, 69))
                     : Brushes.Salmon;
                }

                settings.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = Brushes.LightSlateGray, Margin = new Thickness(0, 6, 0, 4) });
                var row = new Grid();
                row.ColumnDefinitions.Add(new());
                row.ColumnDefinitions.Add(new()
                {
                    Width = new GridLength(12)
                });
                row.ColumnDefinitions.Add(new());
                settings.Children.Add(row);
                if (!notes)
                {
                    var lowBox = Input(low.ToString());
                    var highBox = Input(high.ToString());
                    row.Children.Add(lowBox);
                    Grid.SetColumn(highBox, 2);
                    row.Children.Add(highBox);
                    void Validate(TextBox? changed = null)
                    {
                        bool valid = int.TryParse(lowBox.Text, out var x) && x >= min && x <= max;
                        bool validHigh = int.TryParse(highBox.Text, out var y) && y >= min && y <= max;
                        bool ordered = valid && validHigh && x <= y;
                        bool reversed = valid && validHigh && x > y;
                        Mark(lowBox, valid && !(reversed && changed == lowBox));
                        Mark(highBox, validHigh && !(reversed && changed == highBox));
                        lowBox.ToolTip = highBox.ToolTip = "Od " + min + " do " + max + "; dolna wartość nie może przekraczać górnej.";
                        if (ordered)
                        {
                            set(x, y);
                            model.Save();
                        }
                    }
                    lowBox.TextChanged += (_, _) => Validate(lowBox);
                    highBox.TextChanged += (_, _) => Validate(highBox);
                    lowBox.GotKeyboardFocus += (_, _) => Validate(lowBox);
                    highBox.GotKeyboardFocus += (_, _) => Validate(highBox);
                    void Restore()
                    {
                        lowBox.Text = Math.Clamp(int.TryParse(lowBox.Text, out var x) ? x : low, min, max).ToString();
                        highBox.Text = Math.Clamp(int.TryParse(highBox.Text, out var y) ? y : high, min, max).ToString();
                        if (int.Parse(lowBox.Text) > int.Parse(highBox.Text))
                            highBox.Text = lowBox.Text;
                        Validate();
                    }
                    lowBox.LostKeyboardFocus += (_, _) => Restore();
                    highBox.LostKeyboardFocus += (_, _) => Restore();
                    return;
                }
                var from = Input(MidiNote.Name(low));
                var to = Input(MidiNote.Name(high));
                row.Children.Add(from);
                Grid.SetColumn(to, 2);
                row.Children.Add(to);
                int savedLow = low, savedHigh = high;
                void UpdateNotes(TextBox? changed = null)
                {
                    bool validFrom = MidiNote.TryParse(from.Text, out var x);
                    bool validTo = MidiNote.TryParse(to.Text, out var y);
                    bool reversed = validFrom && validTo && x > y;
                    bool valid = validFrom && validTo && !reversed;
                    Mark(from, validFrom && !(reversed && changed == from));
                    Mark(to, validTo && !(reversed && changed == to));
                    if (valid)
                    {
                        MidiNote.TryParse(to.Text, out var endNote);
                        savedLow = x;
                        savedHigh = endNote;
                        set(savedLow, savedHigh);
                        Draw();
                        model.Save();
                    }
                }
                from.ToolTip = to.ToolTip = "Wpisz nutę, np. C1, C#4 lub G6.";
                from.TextChanged += (_, _) => UpdateNotes(from);
                to.TextChanged += (_, _) => UpdateNotes(to);
                from.GotKeyboardFocus += (_, _) => UpdateNotes(from);
                to.GotKeyboardFocus += (_, _) => UpdateNotes(to);
                void Finish()
                {
                    from.Text = MidiNote.Name(savedLow);
                    to.Text = MidiNote.Name(savedHigh);
                }
                from.LostKeyboardFocus += (_, _) => Finish();
                to.LostKeyboardFocus += (_, _) => Finish();

            }
            if (group.Type == "Zakres klawiszy")
            {
                var first = source.Details.FirstNote;
                var count = source.Details.KeyCount;
                string bounds = first.HasValue && count.HasValue && first.Value + count.Value <= 128
                 ? MidiNote.Name(first.Value) + "-" + MidiNote.Name(first.Value + count.Value - 1) : "C-1-G9 (MIDI)";
                Range("Zakres klawiszy: " + bounds, group.Low, group.High, 0, 127, (a, b) => { group.Low = a; group.High = b; }, true);
            }
            if (group.Type == "Velocity")
                Range("Velocity", group.VelocityLow, group.VelocityHigh, 1, 127, (a, b) => { group.VelocityLow = a; group.VelocityHigh = b; });
            if (group.Type == "Modwheel")
                Range("Modwheel · CC1", group.ModLow, group.ModHigh, 0, 127, (a, b) => { group.ModLow = a; group.ModHigh = b; });
            if (group.Type != "Zakres klawiszy" && group.Type != "Velocity" && group.Type != "Modwheel")
                Range(group.Type + " · od — do", group.ParameterRange[0], group.ParameterRange[1], group.ParameterMin, group.ParameterMax, (a, b) => group.ParameterRanges[group.Type] = new[] { a, b });

        }
    }
}
