using System.IO;
using System.Text.Json;

namespace MidiRouter;

/// <summary>Owns the autosave location and preserves unreadable user files before replacement.</summary>
internal sealed class StudioStore
{
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MidiRouter", "studio.json");

    private readonly string path;
    private bool preserveInvalidFile;

    public StudioStore(string path) => this.path = path;

    public StudioState? Load(Func<string, StudioState> reader, out bool recovered)
    {
        recovered = false;
        if (!File.Exists(path))
            return null;
        try
        {
            return reader(path);
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
        {
            preserveInvalidFile = true;
            recovered = true;
            return null;
        }
    }

    public void Save(StudioState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        if (preserveInvalidFile && File.Exists(path))
        {
            File.Copy(path, path + ".invalid-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
            preserveInvalidFile = false;
        }
        StudioFiles.Write(path, state);
    }
}
