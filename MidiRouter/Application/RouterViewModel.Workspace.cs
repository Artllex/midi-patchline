namespace MidiRouter;

public sealed partial class RouterViewModel
{
    private void LoadWorkspace()
    {
        Sources = BuiltInSources().Where(source => !State.DeletedSourceIds.Contains(source.Id)).ToArray();
        EditingSplit = null;
        EditingGroup = null;
        SoloReceiverId = null;
        State.Graphics ??= new GraphicsPreferences();
        UiPresentation.Configure(State.Language, State.Graphics.Theme);
        foreach (var id in State.VisibleSourceIds)
            if (!Sources.Any(s => s.Id == id) && State.SourceSettings.TryGetValue(id, out var config))
                Sources = Sources.Append(new Source { Id = id, Name = config.Name, ImagePath = config.ImagePath }).ToArray();
        foreach (var c in State.Cards)
            c.IsActive = c.Id == State.ActiveCardId;
        if (!State.VisibleSourceIds.Contains(State.SelectedSourceId))
            State.SelectedSourceId = State.VisibleSourceIds.FirstOrDefault() ?? "";
        foreach (var s in Sources)
            if (State.SourceSettings.TryGetValue(s.Id, out var saved))
            {
                s.Name = saved.Name;
                s.Details = saved.Details?.Copy() ?? SourceDetails.Detect(saved.Name);
                s.ImagePath = saved.ImagePath;
                s.Interface = saved.Interface;
                s.Port = saved.Port;
                s.Channel = saved.Channel;
                if (saved.Tags != null)
                    s.Tags = saved.Tags.ToList();
            }
        MockupData.AddLegacyEndpoints(State);
        foreach (var endpoint in State.Endpoints)
            endpoint.Tags ??= new() { endpoint.Group == "KEYBOARDS" ? "KB" : "Rack" };
        RefreshSourceSections();
        BuildReceivers();
        foreach (var s in Sources)
            s.IsSelected = s.Id == State.SelectedSourceId;
        foreach (var source in Sources)
            source.Octaves = ActiveCard.SourceOctaves.GetValueOrDefault(source.Id);
    }

    public bool ReplaceStudio(StudioState studio)
    {
        ValidateStudio(studio);
        var previous = State;
        State = studio;
        if (!Save())
        {
            State = previous;
            return false;
        }

        LoadWorkspace();
        Changed(nameof(Graphics));
        Changed(nameof(PrimarySources));
        Changed(nameof(Cards));
        Changed(nameof(PinnedCards));
        Changed(nameof(ActiveCard));
        ApplyCurrentRoutes();
        return true;
    }

    public static StudioState NewStudio()
    {
        var studio = Defaults();
        studio.Cards[0].Routes.Clear();
        return studio;
    }

}
