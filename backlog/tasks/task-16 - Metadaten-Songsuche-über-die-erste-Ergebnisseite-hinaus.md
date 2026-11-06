---
id: TASK-16
title: 'Metadaten: Songsuche über die erste Ergebnisseite hinaus'
status: To Do
assignee: []
created_date: '2026-11-06T14:57:00Z'
labels: []
dependencies:
  - TASK-4
priority: low
ordinal: 16000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Die Songsuche bewertet nur die ersten 25 Aufnahmen. Passt das gesuchte Album erst auf einer späteren Seite, taucht es nicht in der Kandidatenliste auf. Aufgefallen im Qodo-Review von PR #2. Der Aufwand ist hoch (eine Seite ist bis zu 350 KB gross, MusicBrainz antwortet bei Last öfter mit 503), und bei den 9 live geprüften Songs stand das richtige Album bereits auf Seite eins, deshalb wurde es nicht in TASK-4 gelöst.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Weitere Ergebnisseiten werden anhand von count und offset innerhalb eines festen Request-Budgets geholt und gemeinsam bewertet
- [ ] #2 Ein Test mit einem Album, das erst auf Seite zwei erscheint, belegt den Fall
- [ ] #3 Das Budget ist konfigurierbar, ein 503 auf einer Folgeseite verwirft die bereits gefundenen Kandidaten nicht
- [ ] #4 Eine Live-Stichprobe mit mindestens einem Song, bei dem Seite eins nicht reicht, ist in den Notizen festgehalten
<!-- AC:END -->
