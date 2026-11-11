namespace MidiRouter;

public sealed class SourceSection
{
    public string Name { get; init; } = ""; public List<Source> Sources { get; init; } = new(); public bool IsExpanded { get; set; } = true;
}
