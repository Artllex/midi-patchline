using System.Windows;
namespace MidiRouter;

public sealed class EndpointDraft : Observable
{
    public bool IsSource { get; set; }
    public System.Windows.Visibility SourceVisibility => IsSource ? Visibility.Visible : Visibility.Collapsed;
    public string[] SourceTypes => new[] { "Klawiatura", "Sekwencer", "Pady / perkusja", "Kontroler", "Pedały", "Program / DAW", "Inne" };
    private string sourceType = "Inne";
    public string SourceType
    {
        get => sourceType; set
        {
            sourceType = value;
            Changed();
            Changed(nameof(KeyboardVisibility));
        }
    }
    public System.Windows.Visibility KeyboardVisibility => SourceType == "Klawiatura" ? Visibility.Visible : Visibility.Collapsed;
    private string keyCountText = "";
    public string KeyCountText
    {
        get => keyCountText; set
        {
            keyCountText = value;
            Changed();
            Changed(nameof(RangeLabel));
        }
    }
    public int[] KeyCounts => new[] { 25, 32, 37, 49, 61, 73, 76, 88 };
    public IEnumerable<MidiPortOption> NoteOptions => new[] { new MidiPortOption { Value = "", Label = "Nie ustawiono" } }.Concat(Enumerable.Range(0, 128).Select(n => new MidiPortOption { Value = n.ToString(), Label = NoteLabel(n) }));
    private string firstNoteText = "";
    public string FirstNoteText
    {
        get => firstNoteText; set
        {
            firstNoteText = value ?? "";
            Changed();
            Changed(nameof(RangeLabel));
        }
    }
    public string RangeLabel => int.TryParse(KeyCountText, out var count) && int.TryParse(FirstNoteText, out var first) && count > 0 && first + count <= 128
        ? NoteLabel(first) + " — " + NoteLabel(first + count - 1) : "Wskaż liczbę klawiszy i najniższą nutę.";
    private static string NoteLabel(int n) => new[] { "C", "C♯", "D", "D♯", "E", "F", "F♯", "G", "G♯", "A", "A♯", "B" }[n % 12] + (n / 12 - 1) + " · MIDI " + n;
    public string DetectionMessage { get; private set; } = "";
    public void DetectKeyboard()
    {
        var detected = SourceDetails.Detect(Name);
        if (detected.KeyCount == null)
            detected = SourceDetails.Detect(Port);
        if (detected.KeyCount is int count)
        {
            SourceType = "Klawiatura";
            KeyCountText = count.ToString();
            DetectionMessage = "Profil modelu: " + count + " klawiszy.";
        }
        else
            DetectionMessage = "Brak profilu modelu. Ustaw dane ręcznie.";
        Changed(nameof(DetectionMessage));
    }
    public Endpoint Original { get; }
    private string name = "";
    public string Name
    {
        get => name; set
        {
            if (name == value)
                return;
            name = value ?? "";
            Changed();
            if (IsSource)
                DetectKeyboard();
        }
    }
    public string Interface { get; set; }
    public List<string> Interfaces { get; } = new() { "Windows MIDI" };
    public IEnumerable<string> Channels => Enumerable.Range(1, 16).Select(c => c.ToString());
    private string port = "";
    public string Port
    {
        get => port; set
        {
            if (port == value)
                return;
            port = value ?? "";
            Changed();
            if (IsSource)
                DetectKeyboard();
        }
    }
    public List<MidiPortOption> PortOptions { get; }
    public string TagsText { get; set; }
    public bool Hidden { get; set; }
    private string imageKey = ""; public string ImageKey
    {
        get => imageKey; set
        {
            imageKey = value;
            Changed();
            Changed(nameof(Preview));
        }
    }
    public System.Windows.Media.Imaging.BitmapSource? Preview => ArtworkPicker.Load(ImageKey);
    public string ChannelText { get; set; }
    public bool Available { get; set; }
    public bool Routed { get; }
    public string OctaveText { get; set; }
    public EndpointDraft(Endpoint endpoint, Route? route, bool input = false)
    {
        TagsText = string.Join(", ", endpoint.Tags ?? new());
        Hidden = endpoint.Hidden;
        ImageKey = endpoint.ImageKey ?? endpoint.Id;
        Interface = endpoint.Interface;
        PortOptions = input ? MidiPorts.Inputs(endpoint.Port) : MidiPorts.Outputs(endpoint.Port);
        Original = endpoint.Copy();
        Name = endpoint.Name;
        Port = endpoint.Port;
        ChannelText = endpoint.Channel.ToString();
        Available = endpoint.Available;
        Routed = route != null;
        OctaveText = (route?.Octaves ?? 0).ToString();
    }
}
