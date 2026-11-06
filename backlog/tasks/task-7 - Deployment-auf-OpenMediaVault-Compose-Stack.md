---
id: TASK-7
title: 'Deployment auf OpenMediaVault: Compose-Stack (ohne Lidarr)'
status: To Do
assignee: []
created_date: '2026-11-06T12:52:00Z'
updated_date: '2026-11-06T13:04:00Z'
labels: []
dependencies:
  - TASK-2
  - TASK-15
ordinal: 7000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Der NUC läuft mit OpenMediaVault, ein OS-Wechsel ist nicht nötig, wenn Docker und das Compose-Plugin (omv-extras) installiert sind. SABnzbd, Navidrome und der Core laufen als ein Stack und teilen sich Download- und Musikordner.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 docker-compose.yml mit Core, SABnzbd und Navidrome, gemeinsame Pfade für Downloads und Musikbibliothek auf demselben Dateisystem
- [ ] #2 Usenet-Provider in SABnzbd eingerichtet, Zugangsdaten und Tokens nur als Secrets, nicht im Repo
- [ ] #3 Stack startet nach Neustart des NUC von selbst
- [ ] #4 Kurze Anleitung im Repo (README) zur Inbetriebnahme
<!-- AC:END -->
