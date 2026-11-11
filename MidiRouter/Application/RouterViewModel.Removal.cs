namespace MidiRouter;

public sealed partial class RouterViewModel
{
    public void DeleteSource(Source source)
    {
        if (!Sources.Contains(source)) return;
        State.DeletedSourceIds.Add(source.Id);
        State.VisibleSourceIds.Remove(source.Id);
        State.SourceSettings.Remove(source.Id);
        State.SplitProfiles.Remove(source.Id);
        foreach (var card in Cards)
        {
            card.Routes.RemoveAll(route => route.SourceId == source.Id);
            card.SplitIds.Remove(source.Id);
            card.SourceOctaves.Remove(source.Id);
        }
        foreach (var group in State.SplitProfiles.Values.SelectMany(profiles => profiles)
            .SelectMany(profile => profile.Groups))
            group.Routes.RemoveAll(route => route.SourceId == source.Id);
        Sources = Sources.Where(item => item != source).ToArray();
        if (State.SelectedSourceId == source.Id)
            State.SelectedSourceId = State.VisibleSourceIds.FirstOrDefault() ?? "";
        CloseSplit();
        foreach (var item in Sources) item.IsSelected = item.Id == State.SelectedSourceId;
        RefreshSourceSections();
        Changed(nameof(PrimarySources));
        Save();
    }

    public void DeleteReceiver(Receiver receiver)
    {
        var id = receiver.Endpoint.Id;
        State.DeletedReceiverIds.Add(id);
        State.Endpoints.RemoveAll(endpoint => endpoint.Id == id);
        foreach (var card in Cards) card.Routes.RemoveAll(route => route.ReceiverId == id);
        foreach (var group in State.SplitProfiles.Values.SelectMany(profiles => profiles)
            .SelectMany(profile => profile.Groups))
            group.Routes.RemoveAll(route => route.ReceiverId == id);
        if (SoloReceiverId == id) SoloReceiverId = null;
        BuildReceivers();
        Refresh();
        ApplyCurrentRoutes();
        Save();
    }
}
