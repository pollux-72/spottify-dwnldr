---
id: TASK-17
title: 'Metadaten: Anfrage nach Albumnamen (Artist - Album)'
status: To Do
assignee: []
created_date: '2026-11-06T14:57:00Z'
labels: []
dependencies:
  - TASK-4
priority: low
ordinal: 17000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Aktuell wird nur nach Aufnahmen (Songs) gesucht. Wer ein ganzes Album anfragt, etwa 'Daft Punk - Random Access Memories', bekommt keine sinnvolle Antwort. Da Usenet-Releases ohnehin Alben sind, ist das eine natürliche Eingabe im Chat. Ausserdem ist das Jahr in der Trefferliste der Songsuche nur ein Hinweis, weil die Suche nicht alle Editionen auflistet.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Findet die Songsuche nichts Passendes oder ist der Text ein Albumname, wird per Release-Group-Suche (releasegroup und artist) nach dem Album gesucht und als Kandidat zurückgegeben
- [ ] #2 Kandidaten aus der Albumsuche haben das Erscheinungsjahr der Release Group (first-release-date), nicht das späteste gelistete Jahr
- [ ] #3 Tests mit gekürzten echten Antworten, Live-Stichprobe mit mindestens zwei Alben
<!-- AC:END -->
