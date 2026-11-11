using System.Windows;
namespace MidiRouter;

public partial class SettingsWindow : Window
{
    private readonly RouterViewModel model;
    private readonly Source? editingSource;
    public List<EndpointDraft> Drafts { get; }
    public SettingsWindow(RouterViewModel owner, string? receiverId = null, bool adding = false)
    {
        model = owner;
        Drafts = model.State.Endpoints.Where(e => receiverId == null || e.Id == receiverId).Select(e => new EndpointDraft(e, model.ActiveRoute(e.Id))).ToList();
        if (adding)
            Drafts = new() { new EndpointDraft(new() { Id = Guid.NewGuid().ToString("N"), Name = "", Port = "", Group = "ON TOP", Available = false, Tags = new() }, null) };
        foreach (var draft in Drafts)
        {
            if (!string.IsNullOrWhiteSpace(model.State.MainInterface) && !draft.Interfaces.Contains(model.State.MainInterface))
                draft.Interfaces.Add(model.State.MainInterface);
            if (!draft.Interfaces.Contains(draft.Interface))
                draft.Interfaces.Add(draft.Interface);
            if (adding)
                draft.Interface = model.State.MainInterface;
        }
        InitializeComponent();
        DataContext = Drafts;
        if (adding)
            Title = "Dodaj odbiorcę";
        if (receiverId != null)
            Title = "Ustawienia odbiorcy — " + Drafts.Single().Name;
    }
    public SettingsWindow(RouterViewModel owner, Source source)
    {
        model = owner;
        editingSource = source;
        var endpoint = new Endpoint { Id = source.Id, Name = source.Name, Tags = source.Tags.ToList(), ImageKey = source.ImagePath ?? "", Interface = source.Interface, Port = source.Port, Channel = source.Channel };
        var draft = new EndpointDraft(endpoint, null, true) { IsSource = true, SourceType = source.Details.Type, KeyCountText = source.Details.KeyCount?.ToString() ?? "", FirstNoteText = source.Details.FirstNote?.ToString() ?? "" };
        if (string.IsNullOrEmpty(draft.KeyCountText) || draft.SourceType == "Inne")
            draft.DetectKeyboard();
        if (!draft.Interfaces.Contains(source.Interface))
            draft.Interfaces.Add(source.Interface);
        if (!draft.Interfaces.Contains(model.State.MainInterface))
            draft.Interfaces.Add(model.State.MainInterface);
        Drafts = new() { draft };
        InitializeComponent();
        DataContext = Drafts;
        Title = "Ustawienia źródła";
    }
    private void BrowseArtwork(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: EndpointDraft draft })
        {
            try
            {
                var file = ArtworkPicker.Pick();
                if (file != null)
                {
                    draft.ImageKey = file;
                }
            }
            catch (Exception ex) { Error.Text = ex.Message; }
        }
    }
    private void ClearArtwork(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: EndpointDraft draft })
            draft.ImageKey = "";
    }
    private void SettingsFooterLoaded(object sender, RoutedEventArgs e)
    {
        var footer = (System.Windows.Controls.Panel)sender;
        footer.Loaded -= SettingsFooterLoaded;
        SettingsFooter.Attach(footer);
    }
    private void SaveSettings(object sender, RoutedEventArgs e)
    {
        if (editingSource != null)
        {
            var draft = Drafts[0];
            if (!int.TryParse(draft.ChannelText, out var channel) || channel < 1 || channel > 16)
            {
                Error.Text = "Kanał musi być liczbą od 1 do 16.";
                return;
            }
            var details = new SourceDetails { Type = draft.SourceType };
            if (draft.SourceType == "Klawiatura")
            {
                if (!int.TryParse(draft.KeyCountText, out var keys) || keys < 1 || keys > 128)
                {
                    Error.Text = "Podaj liczbę klawiszy od 1 do 128.";
                    return;
                }
                details.KeyCount = keys;
                if (!string.IsNullOrWhiteSpace(draft.FirstNoteText))
                {
                    if (!int.TryParse(draft.FirstNoteText, out var first) || first < 0 || first + keys > 128)
                    {
                        Error.Text = "Zakres klawiatury musi mieścić się w MIDI 0–127.";
                        return;
                    }
                    details.FirstNote = first;
                }
            }
            try
            {
                model.UpdateSource(editingSource, draft.Name, ArtworkPicker.UriFor(draft.ImageKey), draft.TagsText.Split(','), draft.Interface, draft.Port ?? "", channel, details);
                Error.Text = "";
                if (sender is not FrameworkElement { Tag: "apply" })
                    DialogResult = true;
            }
            catch (ArgumentException ex) { Error.Text = ex.Message; }
            return;
        }
        var endpoints = new List<Endpoint>();
        var octaves = new Dictionary<string, int>();
        foreach (var d in Drafts)
        {
            if (!int.TryParse(d.ChannelText, out var ch) || ch < 1 || ch > 16)
            {
                Error.Text = "Kanał musi być liczbą od 1 do 16.";
                return;
            }
            var endpoint = d.Original.Copy();
            endpoint.Name = d.Name.Trim();
            endpoint.Port = (d.Port ?? "").Trim();
            endpoint.Interface = d.Interface;
            endpoint.Channel = ch;
            endpoint.Available = d.Available;
            endpoint.Hidden = d.Hidden;
            endpoint.ImageKey = d.ImageKey;
            endpoint.Tags = d.TagsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            endpoints.Add(endpoint);
        }
        try
        {
            model.CommitSettings(model.State.Endpoints.Where(original => !endpoints.Any(edited => edited.Id == original.Id)).Concat(endpoints), octaves);
            Error.Text = "";
            if (sender is not FrameworkElement { Tag: "apply" })
                DialogResult = true;
        }
        catch (ArgumentException ex) { Error.Text = ex.Message; }
    }
}
