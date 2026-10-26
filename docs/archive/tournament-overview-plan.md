# Tournament page: Overview (design D)

## Context
The tournament page Overview repeated itself (format sentence + facts card, next-step button + header button) and the Teams tab was a dead list after the start. Design canvas https://claude.ai/artifact/DKgrzFdLncx7HvzYoGpC12, chosen layout **D** (B's status card + C's teams panel and facts sidebar). Scope: Overview and team management; playing groups/bracket is out of scope. Junior+ project: every part explainable in two sentences.

## Decisions
- **Tabs**: Overview, Groups, Bracket, Results (when finished). No Teams tab: teams are managed on Overview during registration; after the start the Groups/Bracket tabs show them.
- **Header**: name, status chip, format badge, More menu (cancel, delete). No action button.
- **Status card** (full width): status line, headline with the count, one sentence, at most one main action, `StageTrack` with counts ("11 / 16", "2 / 4").
  - Registration: "N of M teams registered", action Start groups / Start bracket (from `canStartGroups`/`canStartBracket`).
  - Seeding: "Drawing the groups" / "Seeding the bracket", no action.
  - Groups live: "X of Y groups finished", no action (the tab bar leads to Groups).
  - Ready for playoffs: "All Y groups finished", action Start bracket.
  - Playoffs live: "X of Y rounds finished", no action.
  - Finished: champion ("Bravo won"), or group winners for GroupsOnly with several groups.
  - Cancelled: who and when, reason.
- **Left panel** (by state): registration = team management (add box + list with remove); after the start = one card per group ("Finished" or "4 / 6 matches") and per bracket round ("Finished" or "1 / 2 matches"), only stages that exist.
- **Sidebar**: format sentence; facts (Max teams in registration, Teams after; Groups; Advance per group; Created by; Created); About (description, when set).
- **Backend**: `CreatedBy`, `CreatedDate` added to `GetTournamentResponseDTO` (audit fields already stored).
- Counts and the status card text come from pure functions (unit tested); components only render them.

## Phase 1: backend
- `GetTournamentResponseDTO` + `CreatedBy`, `CreatedDate`; service test.

## Phase 2: pure logic
- `stage-steps.ts`: `stageCount(step, counts)` label; `StageTrack` input `showCounts` (list page unchanged).
- `overview-status.ts`: `stageCounts(tournament, groups, bracket)`, `statusCard(...)`, `stageCards(groups, bracket)`; specs.

## Phase 3: Overview UI
- `OverviewTab`: status card, left panel (`TeamManager` moved from the Teams tab, stage cards), sidebar.
- Shell + demo: no Teams tab, no header action; route `teams` removed; Teams tab files deleted.
- Docs: CLAUDE.md frontend section.
