---
id: TASK-11
title: 'NZB-Auswahl: bestes Release für ein Album bestimmen'
status: To Do
assignee: []
created_date: '2026-11-06T13:03:00Z'
updated_date: '2026-11-06T13:04:00Z'
labels: []
dependencies:
  - TASK-4
  - TASK-9
ordinal: 11000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Das war die Stärke von Lidarr und ist nun unser eigener Teil: Aus vielen Indexer-Treffern muss das passende Album-Release gewählt werden, sonst landen falsche Alben, Sampler oder schlechte Qualität in der Bibliothek.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Treffer werden gegen MusicBrainz-Daten geprüft (Artist, Albumtitel, Jahr, grobe Größe pro Track-Anzahl)
- [ ] #2 Bewertung mit konfigurierbaren Regeln: bevorzugtes Format (FLAC oder MP3 ab Bitrate), unerwünschte Wörter, Mindest- und Höchstgröße
- [ ] #3 Ohne passenden Treffer ergibt sich der Status nicht_gefunden mit Begründung, es wird nichts Zweifelhaftes geladen
- [ ] #4 Auswahl ist mit Testfällen echter Release-Namen abgedeckt
<!-- AC:END -->
