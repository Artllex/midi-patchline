# Dane makiety

Data zawiera wyłącznie dane przykładowe. Grafiki są wspólnymi zasobami w Assets, a prawdziwe ustawienia użytkownika pozostają poza repozytorium.

- sources.json: ROLI i Nektar oraz historyczne identyfikatory dodatkowych źródeł. Widoczność jest określana przez VisibleSourceIds studia, a nie przez obecność w tym pliku.
- studio.json: początkowi odbiorcy i scena Studio z referencyjnymi przypisaniami.
- legacy-endpoints.json: historyczni odbiorcy dodawani tylko przy braku ich identyfikatorów. Zachowuje zachowanie istniejących wersji prototypu.

MockupData odczytuje osadzone zasoby, zwracając nowe obiekty. Edycja jednej sesji nie zmienia kolejnej. Dane nie są wczytywane ze ścieżek dewelopera.

MockMidiBackend implementuje kontrakt IMidiBackend; symuluje zastosowanie tras i PANIC. Prawdziwy backend MIDI nie został jeszcze zaimplementowany.

Przy przyszłym usunięciu danych demonstracyjnych z normalnego uruchomienia trzeba świadomie zmienić inicjalizację i migrację: obecnie zachowujemy identyfikatory a, b, jp, virus itd., ponieważ występują w zapisanych scenach i Splitach. Nie należy usuwać ich jako pozornie nieużywanych danych.
