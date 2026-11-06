---
id: TASK-12
title: Import und Tagging in die Bibliothek
status: To Do
assignee: []
created_date: '2026-11-06T13:03:00Z'
updated_date: '2026-11-06T13:04:00Z'
labels: []
dependencies:
  - TASK-10
ordinal: 12000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
SABnzbd liefert Rohdateien mit zufälligen Ordnernamen. Navidrome braucht eine saubere Struktur (Artist/Album) und korrekte Tags. Zu entscheiden ist, ob dafür beets (Python, ausgereift, läuft als Container neben dem Core) oder eigene .NET-Logik (z.B. TagLibSharp) genutzt wird; die Entscheidung gehört in dieses Ticket.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Entscheidung beets oder TagLibSharp mit kurzer Begründung festgehalten
- [ ] #2 Fertige Downloads landen als Artist/Album/Track in der Bibliothek, Tags und Cover stimmen mit den MusicBrainz-Daten überein
- [ ] #3 Ein anschliessender Navidrome-Scan wird angestossen (API) oder dokumentiert automatisch
- [ ] #4 Fehlerhafte Importe (unvollständig, falsche Dateien) setzen den Auftrag auf fehler, ohne die Bibliothek zu verändern
<!-- AC:END -->
