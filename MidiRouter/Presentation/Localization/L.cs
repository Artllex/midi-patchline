using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MidiRouter;

public static class L
{
    public static readonly string[] LanguageCodes = { "PL", "ENG", "FR", "ZH", "DE", "ES" };
    public static readonly string[] LanguageLabels = { "PL — Polski", "ENG — English", "FR — Français", "ZH — 简体中文", "DE — Deutsch", "ES — Español" };
    private static readonly Dictionary<string, Dictionary<string, string>> Dictionaries = new()
    {
        ["ENG"] = Load("en"),
        ["FR"] = Load("fr"),
        ["ZH"] = Load("zh"),
        ["DE"] = Load("de"),
        ["ES"] = Load("es")
    };
    public static string Language { get; internal set; } = "PL";

    private static Dictionary<string, string> Load(string code)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"MidiPatchline.Translations.{code}.json")
            ?? throw new InvalidOperationException($"Missing translations: {code}.");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }

    public static string T(string text)
    {
        if (!Dictionaries.TryGetValue(Language, out var dictionary))
            return text;
        if (dictionary.TryGetValue(text, out var translated))
            return translated;
        foreach (var prefix in new[] { "Zakres klawiszy: ", "Ustawienia odbiorcy — ", "Zapisano kartę: ",
            "Nie udało się zapisać konfiguracji: ", "Profil modelu: " })
            if (text.StartsWith(prefix, StringComparison.Ordinal))
                return dictionary[prefix] + text[prefix.Length..].Replace(" klawiszy.", dictionary[" klawiszy."]);

        foreach (var suffix in new[] { " · Włączony", " · Wyłączony", " · niedostępny" })
            if (text.EndsWith(suffix, StringComparison.Ordinal))
                return text[..^suffix.Length] + dictionary[suffix];

        var range = Regex.Match(text, @"^(.+) — wpisz liczbę od (\d+) do (\d+)\.$");
        if (range.Success)
            return string.Format(CultureInfo.CurrentCulture, dictionary["{0} — wpisz liczbę od {1} do {2}."],
                T(range.Groups[1].Value), range.Groups[2].Value, range.Groups[3].Value);

        var bounds = Regex.Match(text, @"^Od (\d+) do (\d+); dolna wartość nie może przekraczać górnej\.$");
        if (bounds.Success)
            return string.Format(CultureInfo.CurrentCulture, dictionary["Od {0} do {1}; dolna wartość nie może przekraczać górnej."],
                bounds.Groups[1].Value, bounds.Groups[2].Value);

        const string assignment = " · kliknij, aby zmienić przypisanie";
        if (text.Contains(assignment))
            return text.Replace(assignment, dictionary[assignment]);
        if (text.Contains('|'))
            return string.Join("|", text.Split('|').Select(T));
        return text;
    }

}
