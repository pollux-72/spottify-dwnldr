---
id: TASK-2
title: Projektgerüst (.NET), Konfiguration, SQLite-State, Dockerfile
status: In Progress
assignee:
  - '@claude'
created_date: '2026-11-06T12:52:00Z'
updated_date: '2026-11-06T13:57:00Z'
labels: []
dependencies: []
ordinal: 2000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Der Core-Dienst läuft als kleiner Container auf dem NUC (OpenMediaVault + Docker). Entscheidung vom Nutzer: Stack .NET, Datenbank SQLite. Spotify und Lidarr sind aus dem Scope (Lidarr wegen instabilem Metadaten-Server und reinem Albumfokus, Spotify wegen Premium-Pflicht im Dev Mode). Stattdessen gibt der Nutzer Songs per Telegram-Bot ein. Der Core braucht persistenten State für Aufträge und deren Status. Die Teile Metadaten, Indexer, Downloader, Import liegen hinter schmalen Schnittstellen, damit sie austauschbar bleiben. .NET-Version (Vorschlag: aktuelle LTS) beim Start prüfen.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Solution mit Core-Bibliothek, Host (Worker-Service mit REST-API) und Testprojekt, Build und Tests per einem Befehl
- [x] #2 Konfiguration über Umgebungsvariablen bzw. appsettings, Secrets nie im Repo
- [x] #3 SQLite-Schema für Aufträge (Anfrage-Text, MusicBrainz-IDs, gewähltes Release, SAB-Job-ID, Status, Fehler, Zeitstempel) mit Migrationen
- [x] #4 Schnittstellen für MetadataProvider, Indexer, Downloader, Importer definiert
- [x] #5 Dockerfile baut ein Image, Datenverzeichnis ist als Volume mountbar
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
1. Solution (.slnx) mit Spottify.Core (Domain + Schnittstellen), Spottify.Infrastructure (EF Core SQLite + Migrationen), Spottify.Host (Web-Host mit Worker-Grundlage, /healthz) und Spottify.Tests (xUnit); Directory.Build.props für Nullable/Warnungen als Fehler.
2. Konfiguration per Options-Klasse (appsettings + Umgebungsvariablen mit Prefix SPOTTIFY_), Datenverzeichnis konfigurierbar.
3. Entität DownloadRequest + RequestStatus (Requested, Resolved, Searching, NotFound, Downloading, Imported, Failed), EF-Migration InitialCreate, Migration beim Start.
4. Schnittstellen IMetadataProvider, IIndexer, IDownloader, ILibraryImporter mit minimalen DTOs (Details folgen in TASK-4/9/10/12).
5. Multi-stage Dockerfile, /data als Volume, .dockerignore, .gitignore, README.
6. Tests: Konfiguration, Migration, Roundtrip der Entität, Healthcheck; dotnet test und docker build verifizieren.
<!-- SECTION:PLAN:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Gebaut mit .NET 10 (SDK 10.0.300), EF Core 10 SQLite, xUnit 2. Verifiziert: dotnet build ohne Warnungen (Warnungen sind Fehler), dotnet test 9/9 grün (Migration, Roundtrip, Status als Text, Zeitsortierung, Options-Bindung, echter Host-Start mit SPOTTIFY_-Umgebungsvariable und /healthz), manueller Start mit dotnet run ergibt Healthy und legt die Datenbank an. AC 5 (Dockerfile) ist NICHT lokal verifiziert, da auf diesem Rechner kein Docker installiert ist. Die CI (.github/workflows/ci.yml) baut das Image und prüft /healthz im Container; AC 5 wird erst nach grüner CI abgehakt.

CI auf PR #1 grün (beide Jobs): build-and-test und docker-image. Der Docker-Job baut das Image, startet den Container und prüft /healthz, damit ist AC 5 belegt. Status bleibt In Progress bis zum Merge des PRs.
<!-- SECTION:NOTES:END -->
