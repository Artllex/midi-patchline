namespace MidiRouter;

public sealed class Route
{
    public string SourceId { get; set; } = "";
    public string ReceiverId { get; set; } = "";
    public int Octaves { get; set; }
}
