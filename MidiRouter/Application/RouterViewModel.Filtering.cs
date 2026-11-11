
namespace MidiRouter;

public sealed partial class RouterViewModel
{
    public IEnumerable<Source> PrimarySources => Sources.Where(s => State.VisibleSourceIds.Contains(s.Id));
    private string searchText = "";
    public string SearchText
    {
        get => searchText;
        set
        {
            if (searchText == value)
                return;
            searchText = value ?? "";
            Changed();
            RefreshSourceSections();
            RefreshFilter();
        }
    }
    private bool MatchesSearch(string name, IEnumerable<string>? tags) =>
        string.IsNullOrWhiteSpace(SearchText) ||
        SearchText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).All(word =>
            name.Contains(word, StringComparison.CurrentCultureIgnoreCase) ||
            (tags?.Any(tag => tag.Contains(word, StringComparison.CurrentCultureIgnoreCase)) ?? false));
    private readonly Dictionary<string, bool> sourceExpansion = new(StringComparer.OrdinalIgnoreCase);
    public List<SourceSection> SourceSections { get; private set; } = new();
    private void RefreshSourceSections()
    {
        foreach (var section in SourceSections)
            sourceExpansion[section.Name] = section.IsExpanded;
        var expanded = sourceExpansion;
        var matchingSources = PrimarySources.Where(s => MatchesSearch(s.Name, s.Tags)).ToList();
        var tags = PrimarySources.SelectMany(s => s.Tags.Count == 0 ? new[] { "Bez tagów" } : s.Tags.AsEnumerable()).Distinct(StringComparer.OrdinalIgnoreCase);
        SourceSections = tags.Select(tag => new SourceSection { Name = tag, Sources = matchingSources.Where(s => tag == "Bez tagów" ? s.Tags.Count == 0 : s.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList(), IsExpanded = !expanded.TryGetValue(tag, out var value) || value })
            .Where(s => s.Sources.Count > 0).OrderBy(s => { int index = State.SourceSectionOrder.FindIndex(t => t.Equals(s.Name, StringComparison.OrdinalIgnoreCase)); return index < 0 ? int.MaxValue : index; }).ToList();
        Changed(nameof(SourceSections));
    }
    public void ReorderSourceSection(string name, int index)
    {
        var ordered = SourceSections.Select(s => s.Name).ToList();
        if (!ordered.Remove(name))
            return;
        ordered.Insert(Math.Clamp(index, 0, ordered.Count), name);
        State.SourceSectionOrder = ordered;
        RefreshSourceSections();
        Save();
    }
    public IEnumerable<Source> OtherSources => Sources.Skip(2);
    public List<Receiver> Receivers { get; private set; } = new();
    public IEnumerable<Receiver> HiddenReceivers => Receivers.Where(r => r.Endpoint.Hidden && MatchesSearch(r.Name, r.Endpoint.Tags));
    public IEnumerable<Receiver> Rack => Receivers.Where(r => Matches(r.Endpoint) && r.Endpoint.Group == "RACK");
    public IEnumerable<Receiver> Keyboards => Receivers.Where(r => Matches(r.Endpoint) && r.Endpoint.Group == "KEYBOARDS");
    public IEnumerable<Receiver> OnTop => Receivers.Where(r => Matches(r.Endpoint) && r.Endpoint.Group == "ON TOP");
    public HashSet<string> SelectedTags { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool ShowHidden { get; set; }
    private bool Matches(Endpoint e) => MatchesSearch(e.Name, e.Tags) && (ShowHidden || !e.Hidden) && (SelectedTags.Count == 0 || (e.Tags ?? new()).Any(t => SelectedTags.Contains(t)));
    public void RefreshFilter()
    {
        Changed(nameof(Rack));
        Changed(nameof(Keyboards));
        Changed(nameof(OnTop));
        Changed(nameof(HiddenReceivers));
        Changed(nameof(HasRack));
        Changed(nameof(HasKeyboards));
        Changed(nameof(HasOnTop));
        Changed(nameof(HasHiddenReceivers));
    }
    public bool HasRack => Rack.Any();
    public bool HasKeyboards => Keyboards.Any();
    public bool HasOnTop => OnTop.Any();
    public bool HasHiddenReceivers => HiddenReceivers.Any();

}
