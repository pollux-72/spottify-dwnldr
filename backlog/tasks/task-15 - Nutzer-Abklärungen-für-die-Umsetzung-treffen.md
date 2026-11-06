---
id: TASK-15
title: 'Nutzer: Abklärungen für die Umsetzung treffen'
status: To Do
assignee: []
created_date: '2026-11-06T13:03:00Z'
labels:
  - user-action
dependencies: []
priority: high
ordinal: 15000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Für diese Punkte hat der Agent nicht genug Informationen. Sie müssen vom Nutzer kommen, bevor die jeweils abhängigen Tasks umgesetzt werden können. Zugangsdaten und API-Keys NICHT im Chat oder im Repo posten, sondern später in die Secret-Konfiguration des Stacks eintragen.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Indexer: Name und Bestätigung, dass er die Newznab-API unterstützt (Kategorie Audio), API-Limits pro Tag, Zahl der Downloads pro Tag
- [ ] #2 Usenet-Provider: Name und Verbindungslimit (für die SABnzbd-Einstellung), Server-Adresse
- [ ] #3 Telegram: Bot bei @BotFather anlegen, Token sicher ablegen; eigene Telegram-User-ID ermitteln
- [ ] #4 NUC: OpenMediaVault-Version, Docker und Compose-Plugin installiert (omv-extras)? Welcher Datenträger und Pfad für Downloads und Musik, wie viel freier Platz?
- [ ] #5 Zugriff vom Handy: Tailscale oder anderes VPN gewünscht? Bereits vorhanden?
- [ ] #6 Qualität: FLAC oder MP3 bevorzugt? Ab welcher Bitrate akzeptabel? Wie soll bei nicht gefundenen Titeln verfahren werden?
- [ ] #7 .NET-Version bestätigen (Vorschlag: aktuelle LTS) und Hosting-Wunsch für den Quellcode (GitHub-Repo, CI)
<!-- AC:END -->
