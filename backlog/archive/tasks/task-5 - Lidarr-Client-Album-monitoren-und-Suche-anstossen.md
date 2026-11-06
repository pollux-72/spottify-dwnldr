---
id: TASK-5
title: 'Lidarr-Client: Album monitoren und Suche anstossen'
status: To Do
assignee: []
created_date: '2026-11-06T12:52:00Z'
labels: []
dependencies:
  - TASK-4
ordinal: 5000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Lidarr sucht über die Indexer des Nutzers, schickt NZBs an SABnzbd und sortiert die Dateien in die Bibliothek. Die Bridge muss nur das Album in Lidarr anlegen und die Suche auslösen. Der Artist soll dabei nicht komplett monitored werden, sonst lädt Lidarr die ganze Diskografie.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Album wird per Lidarr-API (v1) mit monitored=true angelegt, Artist-Monitoring nur für dieses Album
- [ ] #2 Bereits vorhandene Alben in Lidarr werden erkannt und nicht doppelt angelegt
- [ ] #3 Lidarr-URL und API-Key kommen aus der Konfiguration, Fehler setzen den Track-Status auf error mit Meldung
<!-- AC:END -->
