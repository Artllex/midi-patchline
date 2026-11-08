# MIDI Patchline

Natywna aplikacja Windows w C# / WPF do zarządzania routingiem MIDI i presetami studia.

## Aktualny stan

Wersja 0.1.68 jest działającym prototypem interfejsu. Obsługuje wybór źródeł i odbiorców, pojedyncze i wielokrotne przypisania, transpozycję oktawową, tagi, ukrywanie instrumentów, grafiki oraz zapis i przywoływanie presetów. Dostępna jest lista portów MIDI systemu Windows.

Przesyłanie komunikatów MIDI oraz PANIC są obecnie symulowane.

## Uruchomienie

Wymagany Windows oraz .NET SDK z obsługą WPF. Projekt jest ukierunkowany na `net6.0-windows`.

```powershell
dotnet build .\MidiRouter\MidiRouter.csproj -c Release
dotnet run --project .\MidiRouter\MidiRouter.csproj -c Release
```

Publikacja:

```powershell
dotnet publish .\MidiRouter\MidiRouter.csproj -c Release --self-contained false -o .\publish
```

Kontrola modelu i zasobów:

```powershell
.\publish\MidiPatchline.exe --self-test
```

Ustawienia użytkownika są zapisywane lokalnie w `%LOCALAPPDATA%\MidiRouter\studio.json` i nie należą do repozytorium.

## Zawartość

- `MidiRouter/` — kod WPF/C#.
- `MidiRouter/Assets/` — ikona aplikacji i grafiki instrumentów.
- `MidiRouter/tools/` — narzędzie budowania ikony.
