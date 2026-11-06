---
id: TASK-4
title: 'Metadaten: Songanfrage per MusicBrainz zu Album auflösen'
status: Done
assignee:
  - '@claude'
created_date: '2026-11-06T12:52:00Z'
updated_date: '2026-11-06T15:40:00Z'
labels: []
dependencies:
  - TASK-2
ordinal: 4000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Der Nutzer tippt z.B. 'Artist - Titel' in den Chat. Daraus muss ein eindeutiges Album mit Track-Liste werden, damit die Suche auf Usenet gezielt nach dem Album suchen kann (Usenet-Releases sind meist Alben, Albumgranularität ist akzeptiert). MusicBrainz wird direkt abgefragt, ohne Zwischenserver wie bei Lidarr.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Freitext 'Artist - Titel' liefert Kandidaten (Recording mit Album, Jahr, Release-Typ), sortiert nach Relevanz
- [x] #2 Bei mehreren plausiblen Treffern wird eine Auswahlliste zurückgegeben statt still geraten zu werden
- [x] #3 Album-Details enthalten Track-Liste und Track-Anzahl für die spätere Release-Prüfung
- [x] #4 MusicBrainz-Rate-Limit (1 Request/s), User-Agent-Pflicht und Fehler (503) werden eingehalten bzw. mit Retry behandelt
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
1. Core: AlbumCandidate um Release-Typ, getroffenen Track und Score erweitern, AlbumDetails um TrackCount.
2. Infrastructure/MusicBrainz: MusicBrainzOptions (BaseUrl, UserAgent, Intervall, Retries), SongQuery-Parser für 'Artist - Titel' (mit Lucene-Escaping), MusicBrainzClient als IMetadataProvider.
3. Suche: recording-Suche, Releases pro Release-Group zusammenfassen (Bootleg und Pseudo-Release raus), Ranking nach MB-Score, dann Albumtyp (Studioalbum vor Single vor Sampler/Live/DJ-Mix), dann Verbreitung, dann frühestes Jahr. Mehrere Treffer werden als Liste geliefert, nie still geraten.
4. Album-Details: Releases der Release-Group browsen, Release mit der häufigsten Gesamt-Tracklänge und frühestem Datum wählen, Tracks per release-Lookup holen.
5. HTTP-Handler: 1 Request pro Sekunde (TimeProvider), User-Agent, Retry bei 503/429 mit Retry-After.
6. Host: Optionen binden und validieren, Client registrieren.
7. Tests mit gekürzten echten MusicBrainz-Antworten als Fixtures, Fake-Handler und FakeTimeProvider (Ranking, Dedup, Track-Liste, Rate-Limit, Retry, 404). Danach Live-Stichprobe gegen die echte API.
<!-- SECTION:PLAN:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Verifiziert: 63 Tests grün (Fixtures aus echten MusicBrainz-Antworten, Fake-Server, FakeTimeProvider) plus Live-Stichprobe gegen die echte API mit Daft Punk, Radiohead, Billie Eilish, Kendrick Lamar, Nirvana, Imagine Dragons, Queen, Beyoncé, Taylor Swift: das Studioalbum steht jeweils zuoberst. Erkenntnisse aus der Live-Prüfung: (1) Eine einzelne Suchseite füllt sich bei bekannten Songs mit Live-Bootlegs, deshalb zuerst Suche auf offizielle Studioalben, dann Fallback. (2) MB-Score allein reicht nicht, Alben des gewünschten Künstlers werden vorgezogen (Get Lucky). (3) Der Server antwortet bei Last mit 503 und Retry-After 0, deshalb Backoff als Untergrenze. (4) Alle 23 Editionen von Discovery haben 14 Tracks, die Wahl der Standardedition wurde zusätzlich mit synthetischen Fällen getestet. Bekannte Grenzen: Jahr in der Trefferliste ist nur ein Hinweis (zuverlässig erst in den Album-Details), nur Titel ohne Künstler findet zuerst Coverversionen, Alben-Suche per Albumnamen gibt es nicht, nach 3 Retries wird der 503-Fehler an den Aufrufer gereicht.

Qodo-Review PR #2 (4 mittlere Befunde): behoben wurden 1) nur erste 100 Editionen (jetzt bis zu 5 Seiten à 100, Test mit 130 Editionen und Seitenlimit), 3) Gesamt-Timeout von 30 s für die ganze Pipeline (Client-Timeout aus, stattdessen Timeout pro Versuch, Retry-After gedeckelt, Timeout zählt als Versuch und endet als HttpRequestException) und 4) BaseUrl ohne Slash (wird ergänzt, http/https-Pflicht). Befund 2 (Suche über Seite eins hinaus) ist als TASK-16 ausgelagert, zusammen mit TASK-17 (Albumnamen-Suche und Jahr). Tests jetzt 82 grün, 1 Live-Test übersprungen.

Qodo-Review, zweite Runde: Der Timeout pro Versuch lief nur bis zum Eintreffen der Header. Ein Server, der Header sendet und beim Body stockt, hätte den Aufruf nach dem Entfernen des Client-Timeouts unbegrenzt blockiert. Behoben: Der Body wird noch innerhalb des Versuchs-Timeouts gelesen (LoadIntoBufferAsync), ein Timeout zählt als fehlgeschlagener Versuch. Zwei neue Tests (Stocken mit Retry, Stocken ohne Erfolg) schlagen ohne den Fix nach 15 s fehl und laufen mit dem Fix durch.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
MusicBrainz-Songsuche und Album-Details hinter IMetadataProvider: Studioalben zuerst, Rangfolge nach Künstler, Score und Albumtyp, Track-Liste der Standardedition, höfliche HTTP-Pipeline (1 Anfrage pro 1,1 s, Retries, Timeout pro Versuch inklusive Body). Verifiziert mit 85 Tests, Live-Stichprobe an 9 Songs und zwei Qodo-Runden. Ausgelagert: TASK-16 (Suche über Seite 1 hinaus) und TASK-17 (Albumnamen-Suche, genaueres Jahr). PR #2 gemergt.
<!-- SECTION:FINAL_SUMMARY:END -->
