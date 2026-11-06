---
id: TASK-6
title: Status, Benachrichtigungen und Fehleransicht
status: To Do
assignee: []
created_date: '2026-11-06T12:52:00Z'
updated_date: '2026-11-06T13:04:00Z'
labels: []
dependencies:
  - TASK-13
ordinal: 6000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Nicht jede Anfrage findet sich auf Usenet (Singles, Nischenmusik). Der Nutzer muss sehen können, was läuft, was fertig ist und was nicht gefunden wurde, sonst bleiben Lücken unbemerkt.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Status pro Auftrag: angefragt, aufgelöst, gesucht, nicht_gefunden, wird_geladen, importiert, fehler
- [ ] #2 Statusabfrage über REST-API liefert Aufträge gruppiert nach Status
- [ ] #3 Statusänderungen lösen ein Ereignis aus, das der Chat-Client als Nachricht ausgeben kann
<!-- AC:END -->
