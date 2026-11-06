---
id: TASK-8
title: 'Android: Zugriff auf Navidrome und Offline-Sync einrichten'
status: To Do
assignee: []
created_date: '2026-11-06T12:52:00Z'
updated_date: '2026-11-06T13:04:00Z'
labels: []
dependencies:
  - TASK-7
  - TASK-14
ordinal: 8000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Ziel des Nutzers: neue Songs landen ohne manuelles Kopieren auf dem Handy. Navidrome stellt die Bibliothek per Subsonic-API bereit, ein Android-Client cached sie offline.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Navidrome von aussen erreichbar, z.B. per VPN oder Tailscale, ohne ungeschützten Port ins Internet
- [ ] #2 Android-Client (z.B. Symfonium) angebunden, Offline-Cache für die Bibliothek getestet
- [ ] #3 Ende-zu-Ende-Test: Song im Telegram-Bot anfragen, Album erscheint nach dem Import offline auf dem Handy
<!-- AC:END -->
