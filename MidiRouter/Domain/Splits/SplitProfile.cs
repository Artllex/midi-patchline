namespace MidiRouter;

public sealed class SplitProfile
{
    public bool IsPinned { get; set; } = true;
    public string Id { get; set; } = Guid.NewGuid().ToString("N"); public string Name { get; set; } = "Nowy split";
    public List<SplitGroup> Groups { get; set; } = new() { new() { Name = "Warstwa 1", High = 59 }, new() { Name = "Warstwa 2", Low = 60 } };
}
