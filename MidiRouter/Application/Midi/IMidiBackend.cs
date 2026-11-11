namespace MidiRouter;

public interface IMidiBackend
{
    string Description { get; }
    void ApplyRoutes(IEnumerable<Route> routes);
    void Panic();
}
