---
id: TASK-9
title: Indexer-Client (Newznab-API)
status: To Do
assignee: []
created_date: '2026-11-06T13:03:00Z'
updated_date: '2026-11-06T13:04:00Z'
labels: []
dependencies:
  - TASK-2
ordinal: 9000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Der Core muss auf dem Indexer des Nutzers nach Album-Releases suchen. Fast alle Usenet-Indexer sprechen die Newznab-API (Kategorie Audio, 3000er). Die Suche liefert Kandidaten mit Titel, Größe, Alter, Kategorie und NZB-Link.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Suche nach Artist und Album liefert Kandidaten mit Titel, Größe, Alter, Kategorie, NZB-URL
- [ ] #2 Indexer-URL und API-Key kommen aus der Konfiguration, Fehler und Rate-Limits des Indexers werden behandelt
- [ ] #3 Gegen eine Fake-API getestet, ein manueller Test gegen den echten Indexer ist dokumentiert
<!-- AC:END -->
