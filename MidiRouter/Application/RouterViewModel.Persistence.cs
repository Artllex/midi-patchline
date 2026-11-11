using System.IO;
using System.Text.Json;

namespace MidiRouter;

public sealed partial class RouterViewModel
{
    private static StudioState Defaults() => MockupData.CreateStudio();
    private StudioState Load()
    {
        var studio = store.Load(ReadStudio, out var recovered);
        if (recovered)
            Status = "Nie można odczytać konfiguracji. Użyto makiety; oryginalny plik pozostaje zachowany.";
        return studio ?? Defaults();
    }

    public static StudioState ReadStudio(string path)
    {
        var state = JsonSerializer.Deserialize<StudioState>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Brak konfiguracji studia.");
        ValidateStudio(state);
        return state;
    }

    private static void ValidateStudio(StudioState state) =>
        StudioStateValidator.Validate(state, BuiltInSources().Select(source => source.Id).ToHashSet());

    public bool Save()
    {
        try
        {
            store.Save(State);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Status = "Nie udało się zapisać konfiguracji: " + ex.Message;
            return false;
        }
    }
}
