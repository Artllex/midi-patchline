namespace MidiRouter;

public sealed class SourceDetails
{
    public string Type { get; set; } = "Inne";
    public int? KeyCount { get; set; }
    public int? FirstNote { get; set; }
    public SourceDetails Copy() => new() { Type = Type, KeyCount = KeyCount, FirstNote = FirstNote };
    public static SourceDetails Detect(string name)
    {
        var compact = new string(name.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        if (compact.Contains("seaboard2") || compact.Contains("seaboardrise2"))
            return new()
            {
                Type = "Klawiatura",
                KeyCount = 49
            };
        foreach (int count in new[] { 49, 61, 88 })
            if (compact.Contains("impactgxp" + count))
                return new()
                {
                    Type = "Klawiatura",
                    KeyCount = count
                };
        return new();
    }
}
