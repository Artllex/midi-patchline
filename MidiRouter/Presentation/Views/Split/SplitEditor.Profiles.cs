using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MidiRouter;

public sealed partial class SplitEditor
{
    Button ProfileButton(string label, Action click)
    {
        var button = new Button { Content = label, Height = 36, FontSize = 14, Padding = new Thickness(14, 6, 14, 6) };
        if (Application.Current.MainWindow?.TryFindResource("FlatCardButton") is Style style)
            button.Style = style;
        button.Click += (_, _) => click();
        return button;
    }

    void BuildProfileBar()
    {
        ProfileBar.ColumnDefinitions.Add(new ColumnDefinition());
        ProfileBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ProfileBar.Children.Add(new ScrollViewer
        {
            Content = pinnedProfiles,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            PanningMode = PanningMode.HorizontalOnly,
            Margin = new Thickness(0, 0, 16, 0)
        });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(actions, 1);
        ProfileBar.Children.Add(actions);
        actions.Children.Add(ProfileButton("Lista", () =>
         new CardCatalogWindow(Profiles, SelectProfile, SetPinned, ExportProfile) { Owner = Window.GetWindow(this) }.ShowDialog()));
        actions.Children.Add(ProfileButton("Zapisz", SaveProfile));
        actions.Children.Add(ProfileButton("+ Dodaj", AddProfile));
    }

    void SelectProfile(SplitProfile profile)
    {
        model.EditSplit(source, profile);
        Render();
    }

    void SetPinned(SplitProfile profile, bool pinned)
    {
        var previous = profile.IsPinned;
        profile.IsPinned = pinned;
        if (!model.Save())
            profile.IsPinned = previous;
        RenderPinnedProfiles();
    }

    void SaveProfile()
    {
        var profile = model.EditingSplit!;
        new RenameWindow(profile.Name, name =>
        {
            var previous = profile.Name;
            profile.Name = name;
            if (!model.Save())
            {
                profile.Name = previous;
                throw new InvalidOperationException(model.Status);
            }
            Render();
        })
        {
            Owner = Window.GetWindow(this),
            Title = "Zapisz Split"
        }.ShowDialog();
    }

    void AddProfile()
    {
        var profile = new SplitProfile { Name = RouterViewModel.NextSplitName(Profiles) };
        Profiles.Add(profile);
        SelectProfile(profile);
    }

    void ExportProfile(SplitProfile profile) =>
     StudioFileDialogs.Export(Window.GetWindow(this), profile.Name, () => StudioFiles.ExportSplit(model, source, profile));

    void RenderPinnedProfiles()
    {
        pinnedProfiles.Children.Clear();
        foreach (var profile in Profiles.Where(profile => profile.IsPinned))
        {
            var button = ProfileButton(profile.Name, () => SelectProfile(profile));
            if (pinnedProfiles.Children.Count == 0)
                button.BorderThickness = new Thickness(0);
            if (profile == model.EditingSplit)
                button.Background = new SolidColorBrush(Color.FromRgb(35, 74, 56));
            SceneMenu.Attach(button, () => profile.IsPinned, pinned => SetPinned(profile, pinned), export: () => ExportProfile(profile));
            pinnedProfiles.Children.Add(button);
        }
    }

}
