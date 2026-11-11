using System.Windows;
using Microsoft.Win32;

namespace MidiRouter;

public sealed class StudioExchange
{
    public string Format { get; init; } = StudioFiles.ExchangeFormat;
    public int Version { get; init; } = 1;
    public RoutingCard? Scene
    {
        get; init;
    }
    public Dictionary<string, SourceConfig> Sources { get; init; } = new();
    public List<Endpoint> Receivers { get; init; } = new();
    public Dictionary<string, List<SplitProfile>> Profiles { get; init; } = new();
}
