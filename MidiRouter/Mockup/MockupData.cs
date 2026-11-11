using System.IO;
using System.Reflection;
using System.Text.Json;

namespace MidiRouter;

internal static class MockupData
{
    public static Source[] CreateSources() => Read<Source[]>("sources");
    public static StudioState CreateStudio() => Read<StudioState>("studio");

    public static void AddLegacyEndpoints(StudioState studio)
    {
        foreach (var endpoint in Read<Endpoint[]>("legacy-endpoints"))
            if (!studio.DeletedReceiverIds.Contains(endpoint.Id) && !studio.Endpoints.Any(existing => existing.Id == endpoint.Id))
                studio.Endpoints.Add(endpoint);
    }

    private static T Read<T>(string name)
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"MidiPatchline.Mockup.{name}.json")
            ?? throw new InvalidOperationException($"Missing mockup data: {name}.");
        return JsonSerializer.Deserialize<T>(stream)
            ?? throw new InvalidDataException($"Invalid mockup data: {name}.");
    }
}
