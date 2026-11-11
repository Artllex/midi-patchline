namespace MidiRouter;

public sealed class Source : Observable
{
    private bool solo;
    public bool IsSolo
    {
        get => solo; set
        {
            solo = value;
            Changed();
        }
    }
    private int octaves;
    public int Octaves
    {
        get => octaves; set
        {
            octaves = value;
            Changed();
            Changed(nameof(OctaveLabel));
        }
    }
    public string OctaveLabel => $"{Octaves:+0;-0;0} oct";
    public SourceDetails Details { get; set; } = new();
    public string Id { get; init; } = "";
    private string name = ""; public string Name
    {
        get => name; set
        {
            name = value;
            Changed();
        }
    }
    private string? imagePath; public string? ImagePath
    {
        get => imagePath; set
        {
            imagePath = value;
            Changed();
        }
    }
    public string Interface { get; set; } = "Windows MIDI"; public string Port { get; set; } = ""; public int Channel { get; set; } = 1;
    public List<string> Tags { get; set; } = new();
    private bool selected;
    public bool IsSelected
    {
        get => selected; set
        {
            selected = value;
            Changed();
        }
    }
}
