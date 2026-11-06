---
id: TASK-10
title: SABnzbd-Client
status: To Do
assignee: []
created_date: '2026-11-06T13:03:00Z'
updated_date: '2026-11-06T13:04:00Z'
labels: []
dependencies:
  - TASK-2
ordinal: 10000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Der Core übergibt den gewählten NZB an SABnzbd und muss den Fortschritt und das Ergebnis erfahren, um den Auftrag weiterzuführen.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 NZB per API an SABnzbd übergeben (Kategorie 'music', eindeutiger Name), liefert die Job-ID
- [ ] #2 Status eines Jobs abfragbar (queued, downloading, extracting, completed, failed) inkl. Zielordner
- [ ] #3 SABnzbd-URL und API-Key kommen aus der Konfiguration, Fehler werden mit Meldung im Auftrag gespeichert
<!-- AC:END -->
