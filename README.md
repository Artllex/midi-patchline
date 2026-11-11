# MIDI Patchline

Natywna aplikacja Windows w C# / WPF do zarządzania scenami routingu MIDI i Splitami.

## Stan

Wersja 0.1.129 — ukończona makieta UI — zachowuje dotychczasowy interfejs i zapis konfiguracji. Obsługuje przypisania pojedyncze i wielokrotne (także CTRL), transpozycję, Solo, tagi, ukrywanie urządzeń, sceny, Splity, import/eksport, sześć języków oraz motywy kolorystyczne.

Przesyłanie komunikatów MIDI i PANIC nadal są **symulowane** przez MockMidiBackend. Lista portów pochodzi z Windows WinMM; samo wykrycie portu nie oznacza obsługi transmisji.

## Struktura

Repozytorium jest częścią istniejącego projektu Project Manager C:\MIDI, w product/source. Dokumentacja PM, kompilacje i kopie bezpieczeństwa pozostają poza repozytorium, odpowiednio w product/documentation, product/builds i product/operations.

| Katalog | Odpowiedzialność |
| --- | --- |
| MidiRouter/Domain | Stan studia, urządzenia, trasy, sceny, profile i grupy Split, wartości nut |
| MidiRouter/Application | Polecenia i koordynacja routingu; części RouterViewModel według funkcji |
| MidiRouter/Infrastructure | Zapis, walidacja, import/eksport, porty WinMM i motyw okna Windows |
| MidiRouter/Presentation | Widoki WPF, modele widoków, style, zachowania dotyku/myszy, motywy i tłumaczenia |
| MidiRouter/Resources/Localization | Pliki tłumaczeń EN, FR, ZH, DE i ES; PL jest językiem bazowym |
| MidiRouter/Mockup/Data | Wyłącznie dane startowe makiety i zgodności starszych wersji |
| MidiRouter/Mockup | Wczytywanie danych makiety i symulowany backend MIDI |
| MidiRouter/Bootstrap | Opcjonalny punkt wejścia diagnostyki |
| MidiRouter/Assets | Grafiki instrumentów i identyfikacja aplikacji |
| tests/MidiPatchline.Checks | Oddzielny projekt testów modelu i widoków |
| tools | Powtarzalna kompilacja i weryfikacja |

Podział jest funkcjonalny, w jednym projekcie WPF: nie wprowadza sztucznych zależności pakietowych. Części RouterViewModel współdzielą stan jednej sesji; zapis i walidacja mają własne klasy. Dotychczasowa przestrzeń nazw MidiRouter oraz format JSON wersji 1 są zachowane.

## Kompilacja i uruchomienie

Wymagany Windows oraz .NET SDK z obsługą WPF. Projekt używa net6.0-windows, bez zewnętrznych pakietów NuGet.

    dotnet build .\MidiPatchline.sln -c Release
    dotnet run --project .\MidiRouter\MidiRouter.csproj -c Release

Zwykła publikacja aplikacji:

    dotnet publish .\MidiRouter\MidiRouter.csproj -c Release --self-contained false -o .\publish

Kompletna weryfikacja po zmianach:

    .\tools\Verify.ps1 -OutputDirectory .\publish

Skrypt publikuje aplikację, buduje oddzielną bibliotekę testową i uruchamia self-test, startup-check oraz settings-check. Obejmuje routing, Split, zapis/odtwarzanie, import/eksport, odzyskiwanie uszkodzonych plików, zasoby, tłumaczenia, motywy i aktualizację kursora. Testy korzystają z własnych katalogów tymczasowych, bez otwierania konfiguracji użytkownika. Wyniki trafiają do katalogu publikacji.

Flagi diagnostyczne wymagają MidiPatchline.Checks.dll obok aplikacji — tę bibliotekę kopiuje Verify.ps1. Zwykłe uruchomienie nie ładuje biblioteki testów.

Formatowanie C#:

    dotnet format whitespace .\MidiPatchline.sln --no-restore

## Dane użytkownika i makiety

Konfiguracja użytkownika pozostaje w %LOCALAPPDATA%\MidiRouter\studio.json, a wybrane grafiki w sąsiednim Artwork. Nie należą do repozytorium. Zapisy są atomowe; nieczytelna konfiguracja otrzymuje kopię .invalid-* przed pierwszym zastąpieniem.

Dane startowe są zasobami JSON z Mockup/Data, nie listami urządzeń zaszytymi w logice routingu. Każde wczytanie zwraca niezależne obiekty. Nie zastępują zapisanych scen. Ze względu na zgodność prototypu nadal zachowano dodawanie brakujących historycznych odbiorców; opis w MidiRouter/Mockup/README.md.

Aktualizacja języka i motywu jest uruchamiana przy zmianach oraz ładowaniu elementów. Nie wolno podłączać skanowania całego drzewa do LayoutUpdated ani zmieniać kursora w odpowiedzi na QueryCursor: te pętle powodowały przeciążenie interfejsu.

## Ukończony etap UI

Plik → Makieta aktywuje dane demonstracyjne. Przed zmianą bieżące studio otrzymuje lokalną kopię .before-mockup-* obok konfiguracji użytkownika. Język i wygląd pozostają zachowane. Ukończenie UI nie oznacza implementacji rzeczywistej transmisji MIDI.
