# Engine-Anbindung

## Ziel

Eine UCI-kompatible Engine wie Stockfish soll optional angebunden werden, ohne die interne KI zu ersetzen.

## Schritte

- Engine-Pfad in Einstellungen speichern.
- `uci`, `isready`, `position fen ...` und `go movetime ...` verwenden.
- Beste Antwort aus `bestmove` lesen und als normalen Spielzug ausfuehren.
- Engine nur im Analysemodus oder als eigener KI-Level aktivieren.

## Status

`Engine/UciEngineClient.cs` enthaelt einen ersten UCI-Client fuer eine spaetere Stockfish-Anbindung.
