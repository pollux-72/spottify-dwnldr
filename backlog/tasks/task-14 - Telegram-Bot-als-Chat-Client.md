---
id: TASK-14
title: Telegram-Bot als Chat-Client
status: To Do
assignee: []
created_date: '2026-11-06T13:03:00Z'
updated_date: '2026-11-06T13:04:00Z'
labels: []
dependencies:
  - TASK-13
ordinal: 14000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Der Nutzer will Songs in einer Art Chat angeben. Ein Telegram-Bot läuft auf Android ohne eigene App und kann pushen, wenn etwas fertig ist. Der Bot ist nur ein Client der Core-API, damit sich später z.B. eine Web-App ergänzen lässt.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Nachricht 'Artist - Titel' startet eine Anfrage, bei mehreren Treffern erscheinen Auswahl-Buttons
- [ ] #2 Bot meldet Statusänderungen (gefunden, lädt, importiert, nicht gefunden, Fehler) als Nachricht
- [ ] #3 Befehle für Status und Abbrechen
- [ ] #4 Nur die konfigurierten Telegram-User-IDs dürfen den Bot nutzen, alle anderen werden ignoriert
<!-- AC:END -->
