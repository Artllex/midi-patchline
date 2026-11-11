
namespace MidiRouter;

public sealed partial class RouterViewModel
{
    public SplitProfile? EditingSplit { get; private set; }
    public SplitGroup? EditingGroup { get; private set; }
    public List<Route> EditingRoutes => EditingGroup?.Routes ?? ActiveCard.Routes;
    public static string NextSplitName(IEnumerable<SplitProfile> profiles)
    {
        var names = profiles.Select(profile => profile.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        int number = 1;
        while (names.Contains($"Split {number}"))
            number++;
        return $"Split {number}";
    }

    public void EditSplit(Source source, SplitProfile? profile = null)
    {
        ChooseSource(source, true);
        if (!State.SplitProfiles.TryGetValue(source.Id, out var profiles))
            State.SplitProfiles[source.Id] = profiles = new();
        if (profile == null)
        {
            profile = profiles.FirstOrDefault(p => p.Id == ActiveCard.SplitIds.GetValueOrDefault(source.Id)) ?? profiles.FirstOrDefault();
            if (profile == null)
            {
                profile = new SplitProfile { Name = NextSplitName(profiles) };
                profiles.Add(profile);
            }
        }
        EditingSplit = profile;
        EditingGroup = profile.Groups.FirstOrDefault();
        Refresh();
        Save();
    }
    public void SelectSplitGroup(SplitGroup group)
    {
        EditingGroup = group;
        Refresh();
    }
    public void CloseSplit()
    {
        EditingSplit = null;
        EditingGroup = null;
        Refresh();
        ApplyCurrentRoutes();
        Save();
    }
    public void SetSplitGroupEnabled(SplitGroup group, bool enabled)
    {
        var previous = group.IsEnabled;
        group.IsEnabled = enabled;
        if (Save())
            ApplyCurrentRoutes();
        else
            group.IsEnabled = previous;
    }

    public void ToggleSplit(Source source, SplitProfile profile)
    {
        if (!State.SplitProfiles.TryGetValue(source.Id, out var profiles) || !profiles.Contains(profile))
            return;

        if (ActiveCard.SplitIds.GetValueOrDefault(source.Id) == profile.Id)
            ActiveCard.SplitIds.Remove(source.Id);
        else
            ActiveCard.SplitIds[source.Id] = profile.Id;

        ApplyCurrentRoutes();
        Save();
    }
    public IEnumerable<Route> RoutesForNote(string sourceId, int note, int velocity, int modwheel, IReadOnlyDictionary<string, int>? parameters = null)
    {
        if (SoloReceiverId != null)
            return PlaybackRoutes.Where(r => r.SourceId == sourceId);
        if (ActiveCard.SplitIds.TryGetValue(sourceId, out var id) && State.SplitProfiles.TryGetValue(sourceId, out var profiles))
        {
            var profile = profiles.FirstOrDefault(p => p.Id == id);
            if (profile != null)
                return profile.Groups.Where(g => g.Accepts(note, velocity, modwheel, parameters)).SelectMany(g => g.Routes).ToList();
        }
        return ActiveCard.Routes.Where(r => r.SourceId == sourceId);
    }

}
