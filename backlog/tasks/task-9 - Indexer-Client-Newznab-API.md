---
id: TASK-9
title: Indexer-Client (Newznab-API)
status: Done
assignee:
  - '@claude'
created_date: '2026-11-06T13:03:00Z'
updated_date: '2026-11-06T15:40:00Z'
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
- [x] #1 Suche nach Artist und Album liefert Kandidaten mit Titel, Größe, Alter, Kategorie, NZB-URL
- [x] #2 Indexer-URL und API-Key kommen aus der Konfiguration, Fehler und Rate-Limits des Indexers werden behandelt
- [x] #3 Gegen eine Fake-API getestet, ein manueller Test gegen den echten Indexer ist dokumentiert
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
1. Core: IndexerException, IndexerAuthenticationException, IndexerLimitReachedException, damit der Workflow Fehlerarten unterscheiden kann.
2. Infrastructure/Newznab: NewznabOptions (BaseUrl, ApiKey, Kategorien, Limit, Timeout), NewznabIndexer als IIndexer (t=search, q aus Artist und Album ohne Sonderzeichen, cat=3000er), NewznabResponseParser (RSS/XML mit newznab:attr, DTD verboten), Fehlerabbildung für HTTP 401/403/429 und <error code=...>.
3. Schutz des API-Keys: HttpClient-Logging für diesen Client abschalten (die URL enthält den Key), Key nie in Exception-Meldungen.
4. Host: Indexer nur registrieren, wenn Spottify:Indexer gesetzt ist, Optionen validieren.
5. Tests mit realistischen Newznab-Antworten (Fake-Server): Parsing, Größe, Kategorie, Datum, kaputte Einträge, leere Ergebnisse, Fehlercodes, Query-Aufbau, Logging ohne Key. Live-Test nur mit SPOTTIFY_LIVE_TESTS=1 und konfiguriertem Indexer, README beschreibt den manuellen Test.
<!-- SECTION:PLAN:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Gebaut gegen die Newznab-Spezifikation, NICHT gegen einen echten Indexer: Es stand kein Indexer-Zugang zur Verfügung, die XML-Fixtures sind von Hand nach der Spezifikation geschrieben und im Fixture-Kommentar und im README als solche gekennzeichnet. AC 3 verlangt einen dokumentierten manuellen Test: README-Abschnitt 'Usenet search' und der Live-Test NewznabLiveTests (nur mit SPOTTIFY_LIVE_TESTS=1 und konfiguriertem Indexer). Bitte nach TASK-15 einmal gegen den echten Indexer laufen lassen. Verifiziert: 143 Tests grün, 2 Live-Tests übersprungen. Designentscheidungen: XML statt JSON (überall unterstützt), DTD verboten, t=search mit bereinigtem Suchtext statt t=music, API-Key nie in Meldungen oder Logs (Test), kein eigener Retry (Limits des Indexers sollen nicht ausgereizt werden, der Workflow entscheidet). Offen: Indexer mit abweichendem API-Pfad oder ohne t=search.

Qodo-Review PR #3 (3 mittlere Befunde), alle behoben: 1) kaputtes XML bei 401/403/429 ergab die generische statt der statusspezifischen Exception, 2) die Antwort wurde vor jeder Größenprüfung komplett gepuffert (jetzt ResponseHeadersRead, Prüfung von Content-Length und hartes Limit beim Lesen, MaxResponseBytes, Timeout für Header und Body gemeinsam), 3) Key über Klartext-HTTP (http nur noch für lokale Adressen erlaubt, Prowlarr oder NZBHydra im Heimnetz funktionieren weiter). Tests: 171 grün, 2 Live-Tests übersprungen.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Newznab-Indexer-Client hinter IIndexer: t=search, XML-Parser (DTD verboten), Fehlerarten als Exceptions, API-Key nie in Meldungen oder Logs, Größen- und Zeitlimit, Klartext-http nur für lokale Adressen. Verifiziert mit 171 Tests und zwei Qodo-Runden. Einschränkung: nicht gegen einen echten Indexer getestet (Fixtures nach Spezifikation geschrieben), der manuelle Test ist im README beschrieben und nach TASK-15 einmal auszuführen. PR #3 gemergt.
<!-- SECTION:FINAL_SUMMARY:END -->
