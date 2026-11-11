namespace MidiRouter;

public static class MidiNote
{
    public static string Name(int n) => new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" }[n % 12] + (n / 12 - 1);
    public static bool TryParse(string text, out int note)
    {
        note = 0;
        text = text.Trim().ToUpperInvariant().Replace("♯", "#").Replace("♭", "b");
        if (text.Length < 2)
            return false;
        int pitch = text[0] switch
        {
            'C' => 0,
            'D' => 2,
            'E' => 4,
            'F' => 5,
            'G' => 7,
            'A' => 9,
            'B' => 11,
            _ => -100
        };
        if (pitch < 0)
            return false;
        int index = 1;
        if (text[index] == '#')
        {
            pitch++;
            index++;
        }
        else if (text[index] == 'B' || text[index] == 'b')
        {
            pitch--;
            index++;
        }
        if (!int.TryParse(text[index..], out var octave))
            return false;
        note = (octave + 1) * 12 + pitch;
        return note >= 0 && note <= 127;
    }
}
