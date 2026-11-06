---
id: TASK-1
title: 'Spotify: Dev-App registrieren und OAuth-Login (PKCE)'
status: To Do
assignee: []
created_date: '2026-11-06T12:52:00Z'
labels: []
dependencies: []
references:
  - >-
    https://developer.spotify.com/documentation/web-api/tutorials/february-2026-migration-guide
ordinal: 1000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Spotify-Dev-Mode verlangt seit Feb 2026 einen Premium-Owner (max. 5 User). Der Nutzer hat nur einen Free-Account und will Premium holen. Ohne Dev-App (Client ID) kann die Bridge die Liked Songs nicht lesen. Eigene Premium-Subscription muss durchgehend aktiv bleiben, sonst stoppt die App.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Dev-App im Spotify Dashboard mit Premium-Account angelegt, Client ID im Konfig-Secret hinterlegt (nie im Repo)
- [ ] #2 Einmaliger Login-Flow (Authorization Code mit PKCE, Scope user-library-read) liefert Refresh-Token, wird persistent gespeichert
- [ ] #3 Token-Refresh funktioniert ohne Benutzerinteraktion (headless, auf dem Server)
<!-- AC:END -->
