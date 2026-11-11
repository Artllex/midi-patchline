namespace MidiRouter;

public sealed class SplitGroup
{
    public bool IsExpanded { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public static string[] Types => new[] { "Zakres klawiszy", "Velocity", "Modwheel" }
     .Concat(new[] { "Pitch Bend", "Channel Aftertouch", "Poly Aftertouch", "Program Change", "Release Velocity", "Kanał MIDI" })
     .Concat(Enumerable.Range(0, 128).Where(n => n != 1).Select(n => "CC " + n.ToString("000"))).ToArray();
    public string Type { get; set; } = "Zakres klawiszy";
    public Dictionary<string, int[]> ParameterRanges { get; set; } = new();
    public int ParameterMax => Type == "Pitch Bend" ? 16383 : Type == "Kanał MIDI" ? 16 : 127;
    public int ParameterMin => Type == "Kanał MIDI" ? 1 : 0;
    public int[] ParameterRange => ParameterRanges.TryGetValue(Type, out var range) ? range : new[] { ParameterMin, ParameterMax };
    public string Name { get; set; } = "Grupa";
    public int Low { get; set; } = 0; public int High { get; set; } = 127;
    public int VelocityLow { get; set; } = 1; public int VelocityHigh { get; set; } = 127;
    public int ModLow { get; set; } = 0; public int ModHigh { get; set; } = 127;
    public List<Route> Routes { get; set; } = new();
    public bool Accepts(int note, int velocity, int mod, IReadOnlyDictionary<string, int>? parameters = null)
    {
        if (!IsEnabled)
            return false;
        if (Type == "Zakres klawiszy")
            return note >= Low && note <= High;
        if (Type == "Velocity")
            return velocity >= VelocityLow && velocity <= VelocityHigh;
        if (Type == "Modwheel")
            return mod >= ModLow && mod <= ModHigh;
        if (parameters == null || !parameters.TryGetValue(Type, out var value))
            return false;
        return value >= ParameterRange[0] && value <= ParameterRange[1];
    }
}
