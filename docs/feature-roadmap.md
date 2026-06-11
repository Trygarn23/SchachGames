# Feature Roadmap

Diese Roadmap haelt groessere Ideen fest, damit die Todo nicht nur Namen sammelt, sondern pro Thema eine Richtung hat.

## Weitere Fun Modes

- Atomic Chess: eigene Schlaglogik mit Explosionsregeln, danach separate Matt-/Koenigssicherheitspruefung.
- Horde Chess: asymmetrische Startstellung, eigene Gewinnbedingung fuer die Seite ohne normalen Materialaufbau.
- Fog of War: UI- und Logikschicht muessen zwischen echter Stellung und sichtbarer Stellung unterscheiden.
- Koenigjagd: Zielfelder und Siegbedingung als Variante von King of the Hill modellieren.
- Chaos Board: Startstellung muss trotz Zufall beide Koenige, legale Zuege und keine Sofortprobleme garantieren.

## Analyse und Training

- Variantenbaum nutzt `AnalysisVariation` mit Hauptvariante und Nebenvarianten.
- Puzzle nutzt `PuzzleDefinition` mit Start-FEN, Loesungszugfolge, Thema und Rating.
- Beste-Zug-Anzeige kann spaeter Engine- und interne KI-Auswertung nebeneinander zeigen.
- Coach-Kommentare sollen langfristig taktische Motive erkennen: Haenger, Mattdrohung, Gabel, Fesselung.

## Profile und Statistiken

- Partie-Metadaten liegen in `GameMetadata`.
- Lokale Ratings und Fortschritt koennen auf `PlayerRating` aufbauen.
- Ein Statistik-Dashboard sollte zuerst lokale Profile, Varianten und Zeitkontrollen auswerten.

## Visuals

- Der Theme-Ausbau bleibt zweigleisig: feste Presets fuer schnelle Auswahl und spaeter ein Editor fuer eigene Farben.
- Figuren-Sets sollten als austauschbare Asset-Pakete kommen, damit Unicode, SVG und Bildsets nicht vermischt werden.
- 3D-Brett bleibt Langzeitoption, weil es eine andere Rendering-Schicht erfordert.

## Online und Releases

- Online-Modus braucht spaeter eine klare Trennung zwischen lokaler Spielregel-Engine und Netzwerktransport.
- Portable ZIP und Installer sollten erst nach stabiler Settings-/Speicherstruktur gebaut werden.
- Auto-Update bleibt optional und sollte erst nach GitHub Releases kommen.
