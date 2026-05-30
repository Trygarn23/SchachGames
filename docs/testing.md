# Teststrategie

## Vor groesseren Features

```powershell
dotnet build .\Schach.slnx
dotnet test .\Schach.slnx
```

## UI-Smoke-Test

- Anwendung startet ohne XAML-Fehler.
- Neues Spiel erzeugt die Startstellung.
- Ein Zug per Maus aktualisiert Brett, Status und Zugliste.
- KI-Zug laeuft ohne Flackern und ohne blockierende Fehlermeldung.
- Speichern/Laden und PGN/FEN-Import zeigen Statusmeldungen.
