using System.Runtime.InteropServices;
namespace MidiRouter;

public static class MidiPorts
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Caps
    {
        public ushort Manufacturer, Product;
        public uint Driver;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Name;
        public ushort Technology, Voices, Notes, Channels;
        public uint Support;
    }
    [DllImport("winmm.dll")] private static extern uint midiOutGetNumDevs();
    [DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "midiOutGetDevCapsW")] private static extern uint GetCaps(UIntPtr id, out Caps caps, uint size);
    [DllImport("winmm.dll")] private static extern uint midiInGetNumDevs();
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct InCaps
    {
        public ushort Manufacturer, Product; public uint Driver; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Name; public uint Support;
    }
    [DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "midiInGetDevCapsW")] private static extern uint GetInCaps(UIntPtr id, out InCaps caps, uint size);
    public static List<MidiPortOption> Inputs(string? saved = null)
    {
        var list = new List<MidiPortOption> { new() { Label = "Brak przypisania" } };
        for (uint i = 0; i < midiInGetNumDevs(); i++)
            if (GetInCaps((UIntPtr)i, out var caps, (uint)Marshal.SizeOf<InCaps>()) == 0)
                list.Add(new()
                {
                    Value = caps.Name,
                    Label = caps.Name,
                    Present = true
                });
        if (!string.IsNullOrWhiteSpace(saved) && !list.Any(p => p.Value == saved))
            list.Add(new()
            {
                Value = saved,
                Label = saved + " · niedostępny"
            });
        return list;
    }
    public static List<MidiPortOption> Outputs(string? saved = null)
    {
        var list = new List<MidiPortOption> { new() { Label = "Brak przypisania" } };
        for (uint i = 0; i < midiOutGetNumDevs(); i++)
        {
            if (GetCaps((UIntPtr)i, out var caps, (uint)Marshal.SizeOf<Caps>()) == 0)
                list.Add(new()
                {
                    Value = caps.Name,
                    Label = caps.Name,
                    Present = true
                });
        }
        if (!string.IsNullOrWhiteSpace(saved) && !list.Any(p => p.Value == saved))
            list.Add(new()
            {
                Value = saved,
                Label = saved + " · niedostępny"
            });
        return list;
    }
}
