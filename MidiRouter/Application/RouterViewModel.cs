namespace MidiRouter;

public sealed partial class RouterViewModel : Observable
{
    private static Source[] BuiltInSources() => MockupData.CreateSources();
    public Source[] Sources { get; private set; } = Array.Empty<Source>();
    public StudioState State { get; private set; }
    public GraphicsPreferences Graphics => State.Graphics;

    public bool UpdateGeneralSettings(string midiInterface, GraphicsPreferences graphics, string? language = null)
    {
        var previousLanguage = State.Language;
        State.Language = language ?? State.Language;
        var previousInterface = State.MainInterface;
        var previousGraphics = State.Graphics;
        State.MainInterface = midiInterface;
        State.Graphics = graphics;
        if (!Save())
        {
            State.Language = previousLanguage;
            State.MainInterface = previousInterface;
            State.Graphics = previousGraphics;
            return false;
        }

        UiPresentation.Configure(State.Language, State.Graphics.Theme);
        Changed(nameof(Graphics));
        return true;
    }
    public IMidiBackend Backend { get; }
    private readonly StudioStore store;
    private string status = "";
    public string Status
    {
        get => status; private set
        {
            status = value;
            Changed();
        }
    }
    public void RestoreClickRoutes(List<Route> routes)
    {
        if (EditingGroup != null)
            EditingGroup.Routes = routes;
        else
            ActiveCard.Routes = routes;
        Refresh();
        ApplyCurrentRoutes();
        Save();
    }
    public string? SoloReceiverId { get; private set; }
    public void ToggleSolo(Receiver receiver)
    {
        if (!Receivers.Contains(receiver))
            return;
        SoloReceiverId = SoloReceiverId == receiver.Endpoint.Id ? null : receiver.Endpoint.Id;
        Refresh();
        ApplyCurrentRoutes();
    }
    public IEnumerable<Route> PlaybackRoutes => SoloReceiverId == null ? ActiveCard.Routes :
        new[] { new Route { SourceId = State.SelectedSourceId, ReceiverId = SoloReceiverId, Octaves = ActiveRoute(SoloReceiverId)?.Octaves ?? 0 } };
    private void ApplyCurrentRoutes() => Backend.ApplyRoutes(PlaybackRoutes);
    public Command SelectSource { get; }
    public Command ToggleReceiver { get; }
    public Command Panic { get; }
    private bool extendedSelection;
    private bool controlSelection;
    public bool ExtendedSelection => extendedSelection || controlSelection;
    public void SetControlSelection(bool value)
    {
        if (controlSelection == value)
            return;
        controlSelection = value;
        Changed(nameof(ExtendedSelection));
    }
    public Command OctaveDown { get; }
    public Command OctaveUp { get; }
    public void ChooseSource(Source source, bool extended)
    {
        if (!Sources.Contains(source))
            return;
        SoloReceiverId = null;
        State.SelectedSourceId = source.Id;
        extendedSelection = extended;
        foreach (var s in Sources)
            s.IsSelected = s == source;
        Changed(nameof(ExtendedSelection));
        Status = extended ? "Wybór wielu odbiorców · klikaj, aby dodawać lub usuwać." : "Wybór jednego odbiorcy · kliknięcie odbiorcy zastępuje poprzednie przypisania.";
        Refresh();
        ApplyCurrentRoutes();
        Save();
    }
    private void ChangeOctave(object? parameter, int delta)
    {
        if (parameter is Source source)
        {
            source.Octaves = Math.Clamp(source.Octaves + delta, -4, 4);
            ActiveCard.SourceOctaves[source.Id] = source.Octaves;
            Save();
            return;
        }
        if (parameter is not Receiver receiver)
            return;
        if (!receiver.IsRouted)
        {
            Toggle(receiver);
            return;
        }
        var route = ActiveRoute(receiver.Endpoint.Id)!;
        SetOctaves(receiver.Endpoint.Id, Math.Clamp(route.Octaves + delta, -4, 4));
        ApplyCurrentRoutes();
        Save();
    }
    public RouterViewModel(string? path = null, IMidiBackend? backend = null)
    {
        store = new StudioStore(path ?? StudioStore.DefaultPath);
        Backend = backend ?? new MockMidiBackend();
        State = Load();
        LoadWorkspace();
        SelectSource = new(p => { if (p is Source s) ChooseSource(s, false); });
        OctaveDown = new(p => ChangeOctave(p, -1));
        OctaveUp = new(p => ChangeOctave(p, 1));
        ToggleReceiver = new(p => { if (p is Receiver r) Toggle(r); });
        Panic = new(_ => { Backend.Panic(); Status = "PANIC · symulacja zatrzymania wszystkich nut. Przypisania zachowane."; });
        if (string.IsNullOrEmpty(Status))
            Status = Backend.Description;
    }
    public Route? ActiveRoute(string id) => EditingRoutes.FirstOrDefault(r => r.SourceId == State.SelectedSourceId && r.ReceiverId == id);
    private void Toggle(Receiver receiver)
    {

        var route = ActiveRoute(receiver.Endpoint.Id);
        if (!ExtendedSelection)
            EditingRoutes.RemoveAll(r => r.SourceId == State.SelectedSourceId && r != route);
        if (route == null)
            EditingRoutes.Add(new()
            {
                SourceId = State.SelectedSourceId,
                ReceiverId = receiver.Endpoint.Id
            });
        else
            EditingRoutes.Remove(route);
        ApplyCurrentRoutes();
        Refresh();
        Save();
    }

}
