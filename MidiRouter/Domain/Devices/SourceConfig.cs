namespace MidiRouter;

public sealed class SourceConfig
{
    public SourceDetails? Details { get; set; }
    public string Interface { get; set; } = "Windows MIDI"; public string Port { get; set; } = ""; public int Channel { get; set; } = 1; public List<string>? Tags
    { get; set; }
    public string Name { get; set; } = ""; public string? ImagePath { get; set; }
}
