---
id: TASK-4
title: 'Metadaten: Songanfrage per MusicBrainz zu Album auflösen'
status: To Do
assignee: []
created_date: '2026-11-06T12:52:00Z'
updated_date: '2026-11-06T13:03:00Z'
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
- [ ] #1 Freitext 'Artist - Titel' liefert Kandidaten (Recording mit Album, Jahr, Release-Typ), sortiert nach Relevanz
- [ ] #2 Bei mehreren plausiblen Treffern wird eine Auswahlliste zurückgegeben statt still geraten zu werden
- [ ] #3 Album-Details enthalten Track-Liste und Track-Anzahl für die spätere Release-Prüfung
- [ ] #4 MusicBrainz-Rate-Limit (1 Request/s), User-Agent-Pflicht und Fehler (503) werden eingehalten bzw. mit Retry behandelt
<!-- AC:END -->
