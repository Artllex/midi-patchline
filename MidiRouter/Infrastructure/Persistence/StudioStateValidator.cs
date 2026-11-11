using System.IO;

namespace MidiRouter;

/// <summary>Normalizes legacy aliases and validates persisted studio relationships.</summary>
internal static class StudioStateValidator
{
    public static void Validate(StudioState state, IReadOnlySet<string> builtInSourceIds)
    {
        NormalizeLegacyRoutes(state);
        ValidateCollections(state);
        bool KnownSource(string id) => builtInSourceIds.Contains(id) || state.SourceSettings.ContainsKey(id);
        var receiverIds = state.Endpoints.Select(endpoint => endpoint.Id).ToHashSet();
        if (!state.Cards.Any(card => card.Id == state.ActiveCardId) || !(state.SelectedSourceId == "" && state.VisibleSourceIds.Count == 0) && !KnownSource(state.SelectedSourceId))
            throw new InvalidDataException("Nieprawidłowa aktywna scena lub źródło.");

        foreach (var card in state.Cards)
        {
            if (card.Routes.Any(route => !KnownSource(route.SourceId) ||
                !receiverIds.Contains(route.ReceiverId) || route.Octaves < -4 || route.Octaves > 4))
                throw new InvalidDataException("Nieprawidłowa trasa w scenie.");
        }

        StudioFiles.ValidateProfiles(state.SplitProfiles, receiverIds, KnownSource);
        if (state.Cards.Any(card => card.SplitIds == null || card.SourceOctaves == null || card.SplitIds.Any(pair =>
            !state.SplitProfiles.TryGetValue(pair.Key, out var profiles) ||
            !profiles.Any(profile => profile.Id == pair.Value))))
            throw new InvalidDataException("Nieprawidłowe przypisanie Splitu do sceny.");
    }

    private static void NormalizeLegacyRoutes(StudioState state)
    {
        if (state.SelectedSourceId == "roli")
            state.SelectedSourceId = "a";
        if (state.Cards == null)
            return;
        foreach (var card in state.Cards.Where(card => card?.Routes != null))
        {
            if (card.Routes.Any(route => route == null))
                throw new InvalidDataException("Scena zawiera pustą trasę.");
            foreach (var route in card.Routes.Where(route => route.SourceId == "roli"))
                route.SourceId = "a";
            card.Routes = card.Routes.GroupBy(route => (route.SourceId, route.ReceiverId))
                .Select(group => group.First()).ToList();
        }
    }

    private static void ValidateCollections(StudioState state)
    {
        if (state.Version != 1 || state.Endpoints == null || state.Cards == null ||
            state.SourceSettings == null || state.VisibleSourceIds == null || state.SplitProfiles == null ||
            state.SectionOrder == null || state.SourceSectionOrder == null)
            throw new InvalidDataException("Nieprawidłowa struktura konfiguracji.");

        if (state.Endpoints.Any(endpoint => endpoint == null || string.IsNullOrWhiteSpace(endpoint.Id) ||
            string.IsNullOrWhiteSpace(endpoint.Name) || endpoint.Channel < 1 || endpoint.Channel > 16 ||
            !new[] { "RACK", "KEYBOARDS", "ON TOP" }.Contains(endpoint.Group)) ||
            state.Endpoints.Select(endpoint => endpoint.Id).Distinct().Count() != state.Endpoints.Count)
            throw new InvalidDataException("Nieprawidłowa lista odbiorców.");

        if (state.Cards.Count == 0 || state.Cards.Any(card =>
            card == null || string.IsNullOrWhiteSpace(card.Id) || card.Routes == null) ||
            state.Cards.Select(card => card.Id).Distinct().Count() != state.Cards.Count)
            throw new InvalidDataException("Nieprawidłowa lista scen.");
    }
}
