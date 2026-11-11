namespace MidiRouter;

public sealed class Endpoint
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Group { get; set; } = "";
    public string Interface { get; set; } = "Windows MIDI";
    public string Port { get; set; } = "";
    public int Channel { get; set; } = 1;
    public bool Available { get; set; } = true;
    public bool Hidden { get; set; }
    public List<string>? Tags
    { get; set; }
    public string? ImageKey { get; set; }
    public Endpoint Copy()
    {
        var copy = (Endpoint)MemberwiseClone();
        copy.Tags = Tags?.ToList();
        return copy;
    }
}
