# Schach Projekt

## UI
- [x] Hauptfenster mit Schachbrett und Seitenbereich aufbauen.
- [x] Schachbrett als eigene UI-Klasse anlegen.
- [x] Figuren per Klick auswaehlen.
- [x] Moegliche Zuege optisch markieren.
- [x] Gueltige Zuege per Klick ausfuehren.
- [x] Ungueltige Zuege mit Statusmeldung abfangen.
- [x] Button fuer neues Spiel einbauen.
- [x] Statusanzeige fuer aktuellen Spieler anzeigen.
- [x] Letzten Zug in der UI anzeigen.
- [x] Geschlagene Figuren anzeigen.
- [x] Zugliste / Verlauf anzeigen.
- [x] Brett optional drehen.
- [x] Koordinaten am Brettrand anzeigen.
- [x] Fenster bei kleinen Groessen pruefen.
- [x] Tastaturbedienung planen und spaeter umsetzen.
- [x] Spielende in der UI anzeigen.
- [x] Rechtsklick-Markierungen wie bei chess.com planen.
- [x] Rechtsklick-Markierungen auf Feldern setzen und entfernen.
- [x] Mehrere Markierungsfarben per Rechtsklick optional planen.
- [ ] Bauernumwandlung per Auswahlfenster anbieten: Dame, Turm, Laeufer, Springer.
- [ ] Spielende-Dialog mit Gewinner, Grund und Neustart-Button anzeigen.
- [ ] Zugliste per Klick auf einen Zug fokussieren oder hervorheben.
- [ ] Letzten Zug auf dem Brett markieren.
- [ ] Koenig im Schach visuell hervorheben.
- [ ] Captured-Pieces-Anzeige nach Materialwert sortieren.
- [ ] Seitenbereich bei kleinen Fenstern kompakter machen.
- [ ] Tastaturbedienung erweitern: Pfeiltasten fuer Feldauswahl.
- [ ] Tastaturbedienung erweitern: Enter zum Auswaehlen/Ziehen.
- [ ] Hinweis anzeigen, wenn keine legalen Zuege fuer eine Figur existieren.
- [ ] KI-Farbe auswaehlbar machen: Schwarz, Weiss oder beide aus.
- [ ] Modusauswahl anbieten: Mensch gegen Mensch, Mensch gegen KI.

---

## Logic
- [x] Grundmodell fuer Figurenfarben anlegen.
- [x] Grundmodell fuer Figurtypen anlegen.
- [x] Brettposition als eigenes Modell anlegen.
- [x] Schachfigur als eigenes Modell anlegen.
- [x] Spielzustand als eigene Klasse anlegen.
- [x] Startstellung korrekt aufbauen.
- [x] Aktuellen Spieler verwalten.
- [x] Bauernzuege grundlegend implementieren.
- [x] Springerzuege implementieren.
- [x] Laeuferzuege implementieren.
- [x] Turmzuege implementieren.
- [x] Damenzuege implementieren.
- [x] Koenigszuege grundlegend implementieren.
- [x] Eigene Figuren nicht schlagen lassen.
- [x] Figurenschlagen grundlegend erlauben.
- [x] Zugausfuehrung pruefen und zentral kapseln.
- [x] Schach-Erkennung planen.
- [x] Schach-Erkennung implementieren.
- [x] Matt-Erkennung planen.
- [x] Matt-Erkennung implementieren.
- [x] Patt-Erkennung implementieren.
- [x] Rochade planen.
- [x] Rochade implementieren.
- [x] En passant planen.
- [x] En passant implementieren.
- [x] Bauernumwandlung planen.
- [x] Bauernumwandlung implementieren.
- [x] Zugnotation planen.
- [x] Zughistorie als Modell anlegen.
- [ ] Bauernumwandlung auf frei waehlbare Figur umstellen.
- [ ] 50-Zuege-Regel planen.
- [ ] 50-Zuege-Regel implementieren.
- [ ] Dreifache Stellungswiederholung planen.
- [ ] Dreifache Stellungswiederholung implementieren.
- [ ] Remis durch unzureichendes Material erkennen.
- [ ] Aufgabe eines Spielers modellieren.
- [ ] Remisangebot modellieren.
- [ ] Zugvalidierung fuer importierte Partien robuster melden.
- [ ] FEN-Export planen.
- [ ] FEN-Export implementieren.
- [ ] FEN-Import planen.
- [ ] FEN-Import implementieren.
- [ ] PGN-Notation langfristig auf SAN umstellen.
- [ ] Halbzugzaehler und Vollzugnummer fuer FEN/PGN sauber fuehren.
- [ ] Spielzustand fuer Analysemodus kopierbar machen.

---

## KI
- [x] KI-Schwierigkeitsgrade als Modell anlegen.
- [x] Alle legalen Zuege fuer den aktuellen Spieler sammeln.
- [x] Leichte KI mit zufaelligem Zug implementieren.
- [x] Mittlere KI mit Materialbewertung implementieren.
- [x] Schwere KI mit einfacher Antwort-Bewertung implementieren.
- [x] KI in der UI auswaehlbar machen.
- [x] KI-Zug nach Spielerzug automatisch ausfuehren.
- [x] KI bei neuem Spiel sauber zuruecksetzen.
- [x] KI-Zuege in letzter Zug, geschlagene Figuren und Zugliste anzeigen.
- [x] KI nach Schach-Erkennung auf echte legale Schachzuege begrenzen.
- [x] KI-Spielstaerke nach vollstaendigen Sonderregeln neu feinjustieren.
- [ ] KI flackern beheben beim Zug.
- [ ] KI-Farbe konfigurierbar machen.
- [ ] KI-Zug abbrechen, wenn waehrend des Denkens ein neues Spiel gestartet wird.
- [ ] KI-Denkzeit pro Schwierigkeit konfigurierbar machen.
- [ ] Mittel-KI um einfache Koenigssicherheit erweitern.
- [ ] Schwer-KI mit tieferer Suche planen.
- [ ] Schwer-KI mit Minimax und Alpha-Beta-Pruning implementieren.
- [ ] Bewertungsfunktion um Mobilitaet erweitern.
- [ ] Bewertungsfunktion um Bauernstruktur erweitern.
- [ ] Bewertungsfunktion um Koenigssicherheit erweitern.
- [ ] Eroeffnungszuege optional als kleine Bibliothek planen.
- [ ] KI-Zug im Verlauf als KI-Zug kennzeichnen.
- [ ] Analyse der besten KI-Kandidaten optional anzeigen.

---

## Visuals
- [x] Brettfarben zentral auslagern.
- [x] Markierung fuer ausgewaehltes Feld definieren.
- [x] Markierung fuer moegliche Zuege definieren.
- [x] Figurenfarben zentral festlegen.
- [x] Seitenbereich optisch strukturieren.
- [x] Hover-Zustaende fuer Felder verbessern.
- [x] Fokusrahmen fuer Tastaturbedienung gestalten.
- [x] Light Theme planen.
- [x] Dark Theme planen.
- [x] Figurendarstellung spaeter durch eigene Assets ersetzen oder verbessern.
- [x] Animation fuer Zuege optional planen.
- [x] Animation fuer geschlagene Figuren optional planen.
- [x] Rechtsklick-Markierungen visuell definieren.
- [x] Rechtsklick-Markierungen mit moeglichen Zugmarkierungen abstimmen.
- [ ] Nach Markieren von Feldern mit Linksklick alle Markierungen entfernen.
- [ ] Figuren-Assets als echte Grafiken pruefen.
- [ ] Unicode-Figuren gegen SVG/PNG-Figurenset abwaegen.
- [ ] Theme-Palette zentraler strukturieren.
- [ ] Markierungsfarben im Light Theme pruefen.
- [ ] Markierungsfarben im Dark Theme pruefen.
- [ ] Letzter-Zug-Markierung visuell definieren.
- [ ] Schach-Markierung fuer Koenig visuell definieren.
- [ ] Matt/Patt-Endzustand optisch deutlicher gestalten.
- [ ] Zuganimation bei deaktivierter Animation abschaltbar machen.
- [ ] UI-Skalierung bei 125%, 150% und 200% Windows-Skalierung pruefen.
- [ ] Farben auf ausreichenden Kontrast pruefen.

---

## Tests / Qualitaet
- [x] Build pruefen.
- [x] Manuell testen: Spiel startet mit korrekter Grundstellung.
- [x] Manuell testen: Weiss beginnt.
- [x] Manuell testen: Bauern koennen ein oder zwei Felder ziehen.
- [x] Manuell testen: Springer koennen ueber Figuren springen.
- [x] Manuell testen: Laeufer, Turm und Dame werden blockiert.
- [x] Manuell testen: Eigene Figuren koennen nicht geschlagen werden.
- [x] Unit-Test-Projekt planen.
- [x] Unit-Tests fuer Startstellung ergaenzen.
- [x] Unit-Tests fuer Basiszuege ergaenzen.
- [ ] Unit-Tests fuer Rochade ergaenzen.
- [ ] Unit-Tests fuer verbotene Rochade durch Schach ergaenzen.
- [ ] Unit-Tests fuer En passant ergaenzen.
- [ ] Unit-Tests fuer Bauernumwandlung ergaenzen.
- [ ] Unit-Tests fuer Schach-Erkennung ergaenzen.
- [ ] Unit-Tests fuer Matt-Erkennung ergaenzen.
- [ ] Unit-Tests fuer Patt-Erkennung ergaenzen.
- [ ] Unit-Tests fuer FEN-Import/Export vorbereiten.
- [ ] Unit-Tests fuer Speichern/Laden ergaenzen.
- [ ] Unit-Tests fuer PGN-Export ergaenzen.
- [ ] UI-Smoke-Test planen.
- [ ] Regressionstest fuer KI-Zug ohne Crash ergaenzen.
- [ ] Build und Tests vor jedem groesseren Feature ausfuehren.

---

## Spaetere Features
- [x] Menue mit Neu, Speichern, Laden und Beenden planen.
- [x] Spiel speichern und laden.
- [x] PGN-Export planen.
- [x] PGN-Import planen.
- [x] Schachuhr planen.
- [ ] Zeit planen koennen anhand verschiedener Modi, z. B. Blitz, Bullet, Standard.
- [ ] Eigene Zeitkontrolle mit Minuten und Inkrement anbieten.
- [ ] Uhr pausieren und fortsetzen koennen.
- [ ] Uhr nach Spielende automatisch stoppen.
- [x] Lokales Spiel Mensch gegen Mensch verbessern.
- [x] Einfache KI planen.
- [x] Engine-Anbindung optional planen.
- [x] Einstellungen persistent speichern.
- [x] GitHub README mit Screenshot und Build-Anleitung schreiben.
- [ ] Einstellungsfenster fuer Theme, KI, Zeit und Animationen planen.
- [ ] Letzte Fensterposition und Fenstergroesse speichern.
- [ ] Zuletzt geoeffnete Spielstanddatei merken.
- [ ] Export als Bild/Screenshot planen.
- [ ] Analysemodus mit freiem Brett planen.
- [ ] Zuege vor/zurueck durch die Partie planen.
- [ ] Partie nachtraeglich analysieren koennen.
- [ ] UCI-Engine-Anbindung konkretisieren.
- [ ] Stockfish-Anbindung optional implementieren.
- [ ] Release-Build und Installer planen.
- [ ] GitHub Actions fuer Build/Test planen.
- [ ] Changelog einfuehren.
- [x] Version im UI anzeigen.

---

## Legende
- [x] fertig
- [ ] offen
- [ ] ! in Arbeit
