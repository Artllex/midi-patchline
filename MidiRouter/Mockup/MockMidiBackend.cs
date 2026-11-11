namespace MidiRouter;

public sealed class MockMidiBackend : IMidiBackend
{
    public string Description => "Tryb makiety · MIDI symulowane";
    public int PanicCount { get; private set; }
    public void ApplyRoutes(IEnumerable<Route> routes)
    { /* No hardware I/O in mockup. */
    }
    public void Panic() => PanicCount++;
}
