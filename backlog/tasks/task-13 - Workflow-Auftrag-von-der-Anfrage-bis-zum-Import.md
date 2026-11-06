---
id: TASK-13
title: 'Workflow: Auftrag von der Anfrage bis zum Import'
status: To Do
assignee: []
created_date: '2026-11-06T13:03:00Z'
updated_date: '2026-11-06T13:04:00Z'
labels: []
dependencies:
  - TASK-4
  - TASK-10
  - TASK-11
  - TASK-12
ordinal: 13000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Die Bausteine müssen zu einem robusten Ablauf verbunden werden: auflösen, suchen, auswählen, herunterladen, importieren. Der Ablauf muss Neustarts überstehen, ohne Duplikate oder verlorene Aufträge.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Ablauf als Zustandsautomat mit gespeichertem Status, nach Neustart wird dort weitergemacht, wo er stand
- [ ] #2 Doppelte Anfragen für dasselbe Album werden erkannt und nicht erneut geladen
- [ ] #3 REST-API: Anfrage anlegen, Kandidat bestätigen, Status abfragen, Auftrag abbrechen
- [ ] #4 Ende-zu-Ende-Test mit gefakten Indexer- und SABnzbd-Clients
<!-- AC:END -->
