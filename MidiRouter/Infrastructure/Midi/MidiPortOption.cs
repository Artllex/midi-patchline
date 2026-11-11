namespace MidiRouter;

public sealed class MidiPortOption
{
    public string Value { get; init; } = "";
    public string Label { get; init; } = "";
    public bool Present
    {
        get; init;
    }
}
