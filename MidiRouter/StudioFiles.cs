using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace MidiRouter;

public sealed class StudioExchange
{
    public string Format { get; init; } = StudioFiles.ExchangeFormat;
    public int Version { get; init; } = 1;
    public RoutingCard? Scene { get; init; }
    public Dictionary<string, SourceConfig> Sources { get; init; } = new();
    public List<Endpoint> Receivers { get; init; } = new();
    public Dictionary<string, List<SplitProfile>> Profiles { get; init; } = new();
}

public static class StudioFiles
{
    public const string ExchangeFormat = "MIDI Patchline Exchange";
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static string Snapshot(StudioState studio) => JsonSerializer.Serialize(studio, Json);

    public static void Write<T>(string path, T data)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(data, Json));
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static StudioExchange ExportScene(RouterViewModel model, RoutingCard scene)
    {
        var profiles = scene.SplitIds.ToDictionary(pair => pair.Key, pair =>
            model.State.SplitProfiles[pair.Key].Where(profile => profile.Id == pair.Value).ToList());
        return CreateExchange(model, scene, profiles);
    }

    public static StudioExchange ExportSplit(RouterViewModel model, Source source, SplitProfile profile) =>
        CreateExchange(model, null, new() { [source.Id] = new() { profile } });

    private static StudioExchange CreateExchange(RouterViewModel model, RoutingCard? scene, Dictionary<string, List<SplitProfile>> profiles)
    {
        var routes = profiles.Values.SelectMany(list => list).SelectMany(profile => profile.Groups).SelectMany(group => group.Routes)
            .Concat(scene?.Routes ?? Enumerable.Empty<Route>()).ToList();
        var sourceIds = routes.Select(route => route.SourceId).Concat(profiles.Keys)
            .Concat(scene?.SourceOctaves.Keys.AsEnumerable() ?? Enumerable.Empty<string>()).ToHashSet();
        var receiverIds = routes.Select(route => route.ReceiverId).ToHashSet();
        return new StudioExchange
        {
            Scene = scene,
            Sources = model.Sources.Where(source => sourceIds.Contains(source.Id)).ToDictionary(source => source.Id, SourceSettings),
            Receivers = model.State.Endpoints.Where(receiver => receiverIds.Contains(receiver.Id)).ToList(),
            Profiles = profiles
        };
    }

    private static SourceConfig SourceSettings(Source source) => new()
    {
        Name = source.Name, ImagePath = source.ImagePath, Interface = source.Interface, Port = source.Port,
        Channel = source.Channel, Tags = source.Tags, Details = source.Details
    };

    public static StudioExchange ReadExchange(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("Format", out var format) || format.ValueKind != JsonValueKind.String ||
            format.GetString() != ExchangeFormat)
            throw new InvalidDataException("Wybierz plik wyeksportowanej sceny lub Splitu.");
        var exchange = root.Deserialize<StudioExchange>() ?? throw new InvalidDataException("Plik jest pusty.");
        ValidateExchange(exchange);
        return exchange;
    }

    private static void ValidateExchange(StudioExchange data)
    {
        if (data.Version != 1 || data.Sources == null || data.Receivers == null || data.Profiles == null ||
            (data.Scene == null && data.Profiles.Count == 0) ||
            data.Sources.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null ||
                string.IsNullOrWhiteSpace(pair.Value.Name) || pair.Value.Channel is < 1 or > 16) ||
            data.Receivers.Any(receiver => receiver == null || string.IsNullOrWhiteSpace(receiver.Id) ||
                string.IsNullOrWhiteSpace(receiver.Name) || receiver.Channel is < 1 or > 16 ||
                receiver.Group is not ("RACK" or "KEYBOARDS" or "ON TOP")) ||
            data.Receivers.Select(receiver => receiver.Id).Distinct().Count() != data.Receivers.Count)
            throw new InvalidDataException("Nieprawidłowy plik sceny lub Splitu.");

        var receiverIds = data.Receivers.Select(receiver => receiver.Id).ToHashSet();
        ValidateProfiles(data.Profiles, receiverIds, data.Sources.ContainsKey);
        if (data.Scene is not { } scene) return;
        if (string.IsNullOrWhiteSpace(scene.Name) || scene.SplitIds == null || scene.SourceOctaves == null ||
            scene.SourceOctaves.Any(pair => !data.Sources.ContainsKey(pair.Key) || pair.Value is < -4 or > 4) ||
            scene.SplitIds.Any(pair => !data.Profiles.TryGetValue(pair.Key, out var profiles) ||
                !profiles.Any(profile => profile.Id == pair.Value)))
            throw new InvalidDataException("Scena zawiera nieprawidłowe przypisania.");
        ValidateRoutes(scene.Routes, receiverIds, data.Sources.ContainsKey);
    }

    internal static void ValidateProfiles(Dictionary<string, List<SplitProfile>> profiles, HashSet<string> receivers, Func<string, bool> hasSource)
    {
        foreach (var (sourceId, list) in profiles)
        {
            if (!hasSource(sourceId) || list == null ||
                list.Any(profile => profile == null || string.IsNullOrWhiteSpace(profile.Id) || profile.Groups == null ||
                    profile.Groups.Count == 0) ||
                list.Select(profile => profile.Id).Distinct().Count() != list.Count)
                throw new InvalidDataException("Nieprawidłowe profile Split.");

            foreach (var group in list.SelectMany(profile => profile.Groups))
            {
                if (group == null || group.ParameterRanges == null ||
                    group.Low < 0 || group.High > 127 || group.Low > group.High ||
                    group.VelocityLow < 1 || group.VelocityHigh > 127 || group.VelocityLow > group.VelocityHigh ||
                    group.ModLow < 0 || group.ModHigh > 127 || group.ModLow > group.ModHigh ||
                    group.ParameterRanges.Any(pair => pair.Value == null || pair.Value.Length != 2 || pair.Value[0] > pair.Value[1]))
                    throw new InvalidDataException("Nieprawidłowy zakres grupy Split.");
                ValidateRoutes(group.Routes, receivers, id => id == sourceId);
            }
        }
    }

    private static void ValidateRoutes(List<Route> routes, HashSet<string> receivers, Func<string, bool> hasSource)
    {
        if (routes == null || routes.Any(route => route == null || string.IsNullOrEmpty(route.SourceId) ||
                !hasSource(route.SourceId) || !receivers.Contains(route.ReceiverId) || route.Octaves is < -4 or > 4) ||
            routes.Select(route => (route.SourceId, route.ReceiverId)).Distinct().Count() != routes.Count)
            throw new InvalidDataException("Plik zawiera nieprawidłowe trasy MIDI.");
    }

    public static StudioState Merge(RouterViewModel model, StudioExchange exchange)
    {
        ValidateExchange(exchange);
        var studio = Clone(model.State);
        var sourceIds = new Dictionary<string, string>();
        foreach (var (id, settings) in exchange.Sources)
        {
            var existing = model.Sources.FirstOrDefault(source => source.Id == id);
            var targetId = existing != null && !Same(SourceSettings(existing), settings) ? NewId() : id;
            sourceIds[id] = targetId;
            if (targetId != id || existing == null)
                studio.SourceSettings[targetId] = Clone(settings);
            if (!studio.VisibleSourceIds.Contains(targetId))
                studio.VisibleSourceIds.Add(targetId);
        }

        var receiverIds = new Dictionary<string, string>();
        foreach (var receiver in exchange.Receivers)
        {
            var existing = studio.Endpoints.FirstOrDefault(endpoint => endpoint.Id == receiver.Id);
            if (existing != null && Same(existing, receiver))
            {
                receiverIds[receiver.Id] = existing.Id;
                continue;
            }
            var copy = receiver.Copy();
            if (existing != null)
            {
                copy.ImageKey ??= copy.Id;
                copy.Id = NewId();
            }
            studio.Endpoints.Add(copy);
            receiverIds[receiver.Id] = copy.Id;
        }

        Route Remap(Route route) => new()
        {
            SourceId = sourceIds[route.SourceId], ReceiverId = receiverIds[route.ReceiverId], Octaves = route.Octaves
        };
        var profileIds = new Dictionary<(string Source, string Profile), string>();
        foreach (var (sourceId, profiles) in exchange.Profiles)
        {
            var targetSource = sourceIds[sourceId];
            if (!studio.SplitProfiles.TryGetValue(targetSource, out var targetProfiles))
                studio.SplitProfiles[targetSource] = targetProfiles = new();
            foreach (var profile in profiles)
            {
                var imported = Clone(profile);
                imported.Id = NewId();
                imported.Name = UniqueName(string.IsNullOrWhiteSpace(imported.Name) ? "Split" : imported.Name, targetProfiles.Select(existing => existing.Name));
                foreach (var group in imported.Groups) group.Routes = group.Routes.Select(Remap).ToList();
                profileIds[(sourceId, profile.Id)] = imported.Id;
                targetProfiles.Add(imported);
            }
        }

        if (exchange.Scene is { } scene)
        {
            studio.Cards.Add(new RoutingCard
            {
                Id = NewId(),
                Name = UniqueName(scene.Name, studio.Cards.Select(card => card.Name)),
                IsPinned = scene.IsPinned,
                Routes = scene.Routes.Select(Remap).ToList(),
                SourceOctaves = scene.SourceOctaves.ToDictionary(pair => sourceIds[pair.Key], pair => pair.Value),
                SplitIds = scene.SplitIds.ToDictionary(pair => sourceIds[pair.Key], pair => profileIds[(pair.Key, pair.Value)])
            });
        }
        return studio;
    }

    private static string NewId() => Guid.NewGuid().ToString("N");
    private static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    private static bool Same<T>(T a, T b) => JsonSerializer.Serialize(a) == JsonSerializer.Serialize(b);

    private static string UniqueName(string name, IEnumerable<string> existing)
    {
        var names = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!names.Contains(name)) return name;
        int number = 2;
        while (names.Contains($"{name} ({number})")) number++;
        return $"{name} ({number})";
    }
}

public static class StudioFileDialogs
{
    public const string StudioFilter = "Studio MIDI Patchline (*.patchline.json)|*.patchline.json|Pliki JSON (*.json)|*.json";
    public const string ExchangeFilter = "Scena lub Split MIDI Patchline (*.patchline-export.json)|*.patchline-export.json|Pliki JSON (*.json)|*.json";

    public static void Export(Window? owner, string name, Func<StudioExchange> create)
    {
        var safeName = string.Concat(name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch)).Trim().TrimEnd('.');
        var dialog = new SaveFileDialog { Title = "Eksportuj", Filter = ExchangeFilter, FileName = safeName, DefaultExt = ".patchline-export.json" };
        if (dialog.ShowDialog(owner) == true)
            Run(owner, "Eksportuj", () => StudioFiles.Write(dialog.FileName, create()));
    }

    public static void Run(Window? owner, string operation, Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            MessageBox.Show(owner, ex.Message, operation, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
