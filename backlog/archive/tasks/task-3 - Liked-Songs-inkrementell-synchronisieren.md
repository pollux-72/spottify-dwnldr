---
id: TASK-3
title: Liked Songs inkrementell synchronisieren
status: To Do
assignee: []
created_date: '2026-11-06T12:52:00Z'
labels: []
dependencies:
  - TASK-1
  - TASK-2
ordinal: 3000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Die Bridge soll bei jedem Lauf nur neue Likes verarbeiten, damit Spotify-Rate-Limits geschont werden und nichts doppelt angestossen wird. Grundlage ist GET /me/tracks, das im Dev Mode weiter verfügbar ist.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Neue Likes seit dem letzten Lauf werden in die DB übernommen (inkl. ISRC aus external_ids, falls vorhanden)
- [ ] #2 Wiederholter Lauf ohne neue Likes ändert nichts (idempotent)
- [ ] #3 Spotify-Fehler und Rate-Limits (429, Retry-After) werden behandelt, ohne den State zu beschädigen
<!-- AC:END -->
