# QA-Matrix

## Automatisierte Tests

- Kernregeln: Startstellung, Basiszuege, Sonderregeln, Schach, Matt, Patt, Remisregeln.
- Varianten: Chess960-Startstellung, King of the Hill, gespeicherte Varianten.
- KI: legaler Zug, Eroeffnungsbuch, Kandidatenliste.
- Analysemodelle: Variantenbaum und Puzzle-Zugfolge.

## Manuelle UI-Smoke-Tests

- Promotiondialog oeffnen und jede Umwandlungsfigur testen.
- Theme-Wechsel fuer Dunkel, Hell, Holz, Marmor, Neon, Retro, Minimal, High Contrast.
- Rechtsklick-Markierungen, Rechtszieh-Pfeile und Shift-Rechtsklick-Kontextmenue testen.
- Schnellstart fuer Klassisch, Blitz, Fun und Analyse testen.
- Sidebar ein- und ausklappen.

## Accessibility

- High-Contrast-Theme bei normaler und hoher DPI pruefen.
- Tastatur: `N`, `F`, `Esc`, `Strg+K`, Pfeiltasten, Enter.
- Markierungen muessen auch ohne Farbwissen erkennbar bleiben.

## Performance

- KI-Suche im schweren Modus fuer typische Mittelspielstellungen messen.
- Langlauf: 100 KI-Zuege ohne Crash.
- Theme-Wechsel darf das Brett nicht merklich blockieren.
