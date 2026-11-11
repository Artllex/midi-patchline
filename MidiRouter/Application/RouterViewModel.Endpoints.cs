
namespace MidiRouter;

public sealed partial class RouterViewModel
{
    public void UpdateSource(Source source, string name, string? image, IEnumerable<string>? tags = null, string? midiInterface = null, string? port = null, int? channel = null, SourceDetails? details = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Wpisz nazwę źródła.");
        if (!Sources.Contains(source))
        {
            Sources = Sources.Append(source).ToArray();
            if (!State.VisibleSourceIds.Contains(source.Id))
                State.VisibleSourceIds.Add(source.Id);
        }
        if (details != null)
            source.Details = details.Copy();
        source.Name = name.Trim();
        source.ImagePath = image;
        Changed(nameof(PrimarySources));
        if (tags != null)
            source.Tags = tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (midiInterface != null)
            source.Interface = midiInterface;
        if (port != null)
            source.Port = port;
        if (channel != null)
            source.Channel = channel.Value;
        State.SourceSettings[source.Id] = new()
        {
            Details = source.Details.Copy(),
            Name = source.Name,
            ImagePath = image,
            Tags = source.Tags.ToList(),
            Interface = source.Interface,
            Port = source.Port,
            Channel = source.Channel
        };
        RefreshSourceSections();
        Save();
    }
    public void SetOctaves(string id, int value)
    {
        if (value < -4 || value > 4)
            throw new ArgumentOutOfRangeException(nameof(value));
        var route = ActiveRoute(id);
        if (route != null)
            route.Octaves = value;
        Refresh();
    }
    public void CommitSettings(IEnumerable<Endpoint> endpoints, Dictionary<string, int> octaves)
    {
        var copies = endpoints.Select(e => e.Copy()).ToList();
        if (copies.Any(e => string.IsNullOrWhiteSpace(e.Name) || e.Channel < 1 || e.Channel > 16))
            throw new ArgumentException("Wpisz nazwę oraz kanał od 1 do 16. Port może pozostać pusty.");
        if (octaves.Values.Any(v => v < -4 || v > 4))
            throw new ArgumentException("Transpozycja: od −4 do +4 oktaw.");
        State.Endpoints = copies;
        foreach (var pair in octaves)
            SetOctaves(pair.Key, pair.Value);
        RefreshSourceSections();
        if (SoloReceiverId != null && !State.Endpoints.Any(e => e.Id == SoloReceiverId && e.Available && !string.IsNullOrWhiteSpace(e.Port)))
            SoloReceiverId = null;
        BuildReceivers();
        Refresh();
        ApplyCurrentRoutes();
        Save();
    }
    private void BuildReceivers()
    {
        Receivers = State.Endpoints.Select(e => new Receiver(this, e)).ToList();
        RefreshFilter();
    }
    private void Refresh()
    {
        foreach (var source in Sources)
        {
            source.Octaves = ActiveCard.SourceOctaves.GetValueOrDefault(source.Id);
            source.IsSolo = SoloReceiverId != null && source.Id == State.SelectedSourceId;
        }
        foreach (var r in Receivers)
            r.Refresh();
    }

}
