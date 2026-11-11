using System.IO;

namespace MidiRouter;

internal static class RefactoringChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    public static void Run(string directory)
    {
        var sources = MockupData.CreateSources();
        sources[0].Name = "Changed fixture";
        sources[0].Tags.Clear();
        Check(MockupData.CreateSources()[0].Name == "ROLI Seaboard 2" &&
            MockupData.CreateSources()[0].Tags.Count > 0, "Source fixtures must not share mutable state");

        var fixture = MockupData.CreateStudio();
        fixture.Cards[0].Routes.Clear();
        Check(MockupData.CreateStudio().Cards[0].Routes.Count == 2, "Studio fixtures must be fresh");

        var legacy = MockupData.CreateStudio();
        MockupData.AddLegacyEndpoints(legacy);
        var count = legacy.Endpoints.Count;
        MockupData.AddLegacyEndpoints(legacy);
        Check(legacy.Endpoints.Count == count, "Legacy mockup additions must be idempotent");

        var sourceIds = sources.Select(source => source.Id).ToHashSet();
        StudioStateValidator.Validate(legacy, sourceIds);
        legacy.Cards[0].Routes.Add(new Route { SourceId = "missing", ReceiverId = "jp" });
        try
        {
            StudioStateValidator.Validate(legacy, sourceIds);
            throw new InvalidOperationException("Unknown route source accepted");
        }
        catch (InvalidDataException) { }

        foreach (int note in Enumerable.Range(0, 128))
            Check(MidiNote.TryParse(MidiNote.Name(note), out var parsed) && parsed == note,
                "MIDI note round trip: " + note);
        Check(!MidiNote.TryParse("F#4d", out _) && !MidiNote.TryParse("G10", out _),
            "Invalid notes must be rejected");

        foreach (var choice in ArtworkPicker.Choices.Where(choice => choice.Key.Length > 0))
            Check(ArtworkPicker.Load(choice.Key) != null, "Packaged artwork: " + choice.Key);
        Check(ArtworkPicker.UriFor("jp")!.EndsWith("/jp8080.png") &&
            ArtworkPicker.UriFor("roland") == ArtworkPicker.UriFor("jd800"),
            "Legacy artwork aliases must resolve consistently");

        var file = Path.Combine(directory, "store-recovery.json");
        File.WriteAllText(file, "{broken");
        var original = File.ReadAllBytes(file);
        var store = new StudioStore(file);
        Check(store.Load(RouterViewModel.ReadStudio, out var recovered) == null && recovered,
            "Unreadable studio recovery");
        Check(File.ReadAllBytes(file).SequenceEqual(original), "Read must not overwrite invalid studio");
        store.Save(MockupData.CreateStudio());
        var backups = Directory.GetFiles(directory, "store-recovery.json.invalid-*");
        Check(backups.Length == 1 && File.ReadAllBytes(backups[0]).SequenceEqual(original),
            "Original unreadable file must be preserved exactly");
        Check(RouterViewModel.ReadStudio(file).Cards[0].Id == "studio", "Recovered studio can be reloaded");
        File.Delete(file);
        File.Delete(backups[0]);
    }
}
