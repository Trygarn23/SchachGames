# Schach

C# WPF-Schachspiel mit eigener GUI, lokaler Partie und optionalem KI-Gegner.

![Schach Screenshot](docs/screenshot.svg)

## Funktionen

- Vollstaendiges 8x8-Schachbrett mit Klicksteuerung.
- Legale Zuege inklusive Schachschutz, Schachmatt und Patt.
- Rochade, En passant und automatische Bauernumwandlung zur Dame.
- KI-Gegner in drei Stufen: Leicht, Mittel, Schwer.
- KI-Persoenlichkeiten: Ausgeglichen, Aggressiv, Defensiv, Taktisch und Chaotisch.
- Rechtsklick-Markierungen auf Feldern mit mehreren Farben.
- Zugliste, letzter Zug und geschlagene Figuren.
- Schachuhr mit 10 Minuten pro Seite.
- Spielstand speichern/laden und PGN exportieren/importieren.
- Varianten/Fun Modes: Chess960, Bauernkrieg, Springerduell, King of the Hill, Drei-Schach und Ohne Damen.
- Brettdarstellung mit mehreren Themes inklusive Hell, Dunkel, Holz, Marmor, Neon, Retro, Minimal und High Contrast.
- Drag-and-drop fuer Figuren, Materialbilanz, einfache Bewertungsanzeige und Beste-Zug-Hinweis.

## Build

```powershell
dotnet build .\Schach.slnx
```

## Tests

```powershell
dotnet test .\Schach.slnx
```

## Release

Siehe [docs/release.md](docs/release.md).

## Fun Modes

Die Regeln der aktuellen Varianten stehen in [docs/fun-modes.md](docs/fun-modes.md).
Groessere Ideen und Planungsnotizen stehen in [docs/feature-roadmap.md](docs/feature-roadmap.md).
Die manuelle Testmatrix steht in [docs/qa-matrix.md](docs/qa-matrix.md).

## Projektstruktur

- `AI/`: KI-Schwierigkeitsgrade und Bewertungslogik.
- `Logic/`: Spielregeln, Figuren, Zuege, Spielende und Historie.
- `UI/`: Schachbrett-Komponente und Interaktion.
- `Visuals/`: Farben, Themes und Markierungsfarben.
- `Persistence/`: Spielstand, PGN und Einstellungen.
- `Tests/`: Unit-Tests fuer Kernregeln.

## Hinweis zur Nutzung

Dieses Projekt ist öffentlich einsehbar und dient Lern- und Übungszwecken. Der Quellcode darf ohne ausdrückliche Zustimmung nicht kopiert, weiterverwendet oder als eigenes Projekt ausgegeben werden.
