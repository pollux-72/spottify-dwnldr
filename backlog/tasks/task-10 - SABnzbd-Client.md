---
id: TASK-10
title: SABnzbd-Client
status: In Progress
assignee:
  - '@claude'
created_date: '2026-11-06T13:03:00Z'
updated_date: '2026-11-06T15:45:00Z'
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
- [x] #1 NZB per API an SABnzbd übergeben (Kategorie 'music', eindeutiger Name), liefert die Job-ID
- [x] #2 Status eines Jobs abfragbar (queued, downloading, extracting, completed, failed) inkl. Zielordner
- [x] #3 SABnzbd-URL und API-Key kommen aus der Konfiguration, Fehler werden mit Meldung im Auftrag gespeichert
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
1. Core: DownloaderException und DownloaderAuthenticationException; IDownloader unverändert (Submit, GetStatus).
2. Gemeinsame Hilfen aus dem Newznab-Client herausziehen: Prüfung lokaler Adressen (Klartext-http) und begrenztes Lesen der Antwort, damit beide Clients dasselbe nutzen.
3. Infrastructure/Sabnzbd: SabnzbdOptions (BaseUrl, ApiKey, Kategorie music, Timeout, Antwortlimit, optionale Pfadzuordnung RemotePath/LocalPath), SabnzbdDownloader: mode=addurl mit nzbname und cat, liefert nzo_id; Status über mode=queue und mode=history mit nzo_ids, Abbildung der SAB-Zustände auf Queued, Downloading, Extracting, Completed, Failed, Ausgabepfad aus history.storage mit Pfadzuordnung.
4. Schutz des API-Keys wie beim Indexer (kein HTTP-Logging, keine URL in Meldungen). Job-IDs werden vor der Verwendung geprüft.
5. Host: optional über Spottify:Sabnzbd, dann vollständige Validierung beim Start.
6. Tests mit nach der SABnzbd-API-Doku geschriebenen JSON-Antworten (Fake-Server): Submit, Statuszustände, unbekannte Jobs, Fehler, Auth, Limit, Pfadzuordnung, Logging ohne Key; Live-Test nur mit SPOTTIFY_LIVE_TESTS=1, README beschreibt den manuellen Test.
<!-- SECTION:PLAN:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Gebaut gegen die SABnzbd-API-Dokumentation, NICHT gegen ein laufendes SABnzbd (keins zur Hand, die JSON-Antworten sind von Hand nach der Doku geschrieben, so gekennzeichnet in Harness und README). Der manuelle Test ist der Live-Test SabnzbdLiveTests: er startet keinen Download, sondern fragt nach einem nicht existierenden Job und prüft damit Adresse, Key und API-Pfad. Verifiziert: 253 Tests grün, 3 Live-Tests übersprungen. AC 3 (Fehler mit Meldung im Auftrag speichern) ist der Teil des Workflows in TASK-13: der Client liefert DownloaderException mit Meldung ohne Key, gespeichert wird sie dort. Entscheidungen: Status aus Queue und History, bei Nichtfund zwei Durchgänge wegen des Wechsels zwischen beiden Listen, unbekannter Job wird als Failed gemeldet statt geworfen, Pfadzuordnung RemotePath/LocalPath, gemeinsame Hilfen LocalAddress und LimitedBody mit dem Indexer-Client geteilt. Offen: bei TASK-7 prüfen, dass SABnzbd und Core denselben Pfad mounten.
<!-- SECTION:NOTES:END -->
