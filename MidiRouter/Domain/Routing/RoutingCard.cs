namespace MidiRouter;

public sealed class RoutingCard : Observable
{
    private bool pinned = true;
    public bool IsPinned
    {
        get => pinned; set
        {
            pinned = value;
            Changed();
        }
    }
    public string Id { get; set; } = "studio";
    private string name = "Studio";
    public string Name
    {
        get => name; set
        {
            name = value;
            Changed();
        }
    }
    private bool active;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsActive
    {
        get => active; set
        {
            active = value;
            Changed();
        }
    }
    public Dictionary<string, string> SplitIds { get; set; } = new();
    public Dictionary<string, int> SourceOctaves { get; set; } = new();
    public List<Route> Routes { get; set; } = new();
}
