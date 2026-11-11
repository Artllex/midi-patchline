namespace MidiRouter;

public sealed class StudioState
{
    public Dictionary<string, List<SplitProfile>> SplitProfiles { get; set; } = new();
    public GraphicsPreferences Graphics { get; set; } = new();
    public string Language { get; set; } = "PL";
    public List<string> SourceSectionOrder { get; set; } = new();
    public List<string> VisibleSourceIds { get; set; } = new() { "a", "b" };
    public Dictionary<string, SourceConfig> SourceSettings { get; set; } = new();
    public List<string> SectionOrder { get; set; } = new() { "RACK", "KEYBOARDS", "ON TOP" };
    public List<string> DeletedSourceIds { get; set; } = new();
    public List<string> DeletedReceiverIds { get; set; } = new();
    public int Version { get; set; } = 1;
    public string SelectedSourceId { get; set; } = "a";
    public string MainInterface { get; set; } = "Windows MIDI";
    public string ActiveCardId { get; set; } = "studio";
    public List<Endpoint> Endpoints { get; set; } = new();
    public List<RoutingCard> Cards { get; set; } = new();
}
