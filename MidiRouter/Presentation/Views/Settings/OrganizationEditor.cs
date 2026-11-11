using System.Windows;
using System.Windows.Controls;

namespace MidiRouter;

internal sealed class OrganizationEditor : StackPanel
{
    internal sealed class Item
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public bool Visible { get; set; }
    }
    public List<Item> Items { get; }
    private readonly StackPanel rows = new();
    private OrganizationEditor? parents;
    private Func<Item, string, bool>? belongs;
    private event Action? Edited;

    public OrganizationEditor(string title, IEnumerable<Item> items)
    {
        Items = items.ToList();
        Children.Add(new TextBlock { Text = title, FontSize = 16, Margin = new Thickness(0, 2, 0, 8) });
        Children.Add(rows);
        Render();
    }

    public void GroupBy(OrganizationEditor tags, Func<Item, string, bool> membership)
    {
        parents = tags;
        belongs = membership;
        tags.Edited += Render;
        Render();
    }

    private DockPanel Row(Item item, List<Item> order, Action changed)
    {
        var row = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
        foreach (var delta in new[] { 1, -1 })
        {
            var button = new Button { Content = delta < 0 ? "↑" : "↓", Width = 34, Padding = new Thickness(4), FontSize = 14,
                IsEnabled = order.IndexOf(item) + delta >= 0 && order.IndexOf(item) + delta < order.Count };
            DockPanel.SetDock(button, Dock.Right);
            button.Click += (_, _) =>
            {
                int index = order.IndexOf(item);
                var neighbor = order[index + delta];
                // Keep one canonical order and one shared visibility value for repeated objects.
                var canonical = Items.Contains(item) ? Items : parents!.Items;
                int first = canonical.IndexOf(item), second = canonical.IndexOf(neighbor);
                (canonical[first], canonical[second]) = (canonical[second], canonical[first]);
                changed();
            };
            row.Children.Add(button);
        }
        var visible = new CheckBox { Content = item.Name, IsChecked = item.Visible, FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center, ToolTip = "Pokaż / ukryj", Margin = new Thickness(0, 4, 8, 4) };
        visible.Checked += (_, _) => { item.Visible = true; changed(); };
        visible.Unchecked += (_, _) => { item.Visible = false; changed(); };
        row.Children.Add(visible);
        return row;
    }

    private void Changed()
    {
        Render();
        Edited?.Invoke();
    }

    private void Render()
    {
        rows.Children.Clear();
        if (parents == null)
        {
            foreach (var item in Items) rows.Children.Add(Row(item, Items, Changed));
            return;
        }
        foreach (var tag in parents.Items)
        {
            var group = new StackPanel { Margin = new Thickness(0, 4, 0, 10) };
            group.Children.Add(Row(tag, parents.Items, parents.Changed));
            var children = new StackPanel { Margin = new Thickness(20, 0, 0, 0), Opacity = tag.Visible ? 1 : .55 };
            var members = Items.Where(item => belongs!(item, tag.Id)).ToList();
            foreach (var item in members) children.Children.Add(Row(item, members, Changed));
            group.Children.Add(children);
            rows.Children.Add(group);
        }
    }
}
