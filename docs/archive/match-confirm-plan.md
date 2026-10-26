# Match scoring: draft in the hub, saved on Confirm

## Context
Scoring saved every game at once, and the game that decided a match ran everything (winner, group stats, advancing, finishing). A misclick on that game could not be undone. Design canvas: https://claude.ai/artifact/6KQ4RD3FWjeQYFRqd5aJS6 (stepper rows, Confirm, group switcher). Junior+ project: each part explainable in two sentences.

## Decisions
- **Draft**: per match two counts (wins A, wins B), changed with a small `−` / `+` per team. Not saved; shared live with the tournament's hub group (`ShareMatchDraft` → `MatchDraftUpdated`, relay only, nothing stored). Shown as "draft" / "draft · anna".
- **Confirm**: shown when one team has the wins needed (`+` off, `−` still fixes). Sends the counts in one request; the database only holds confirmed matches. The final looks the same; the existing "Tournament finished" toast follows.
- **Confirmed rows**: score only, winner bold, loser faded, no controls (reopen = later admin feature, out of scope).
- **Best of N**: wins needed from the match's `BestOf` (`GetGamesToWin`), nothing is fixed to Bo3.
- **Backend is additive**: the old per-game endpoint, `GameResultPipeline` and its steps stay untouched until the new flow is accepted. New code lives in new files; existing files only get registrations and one hub method.
- **Groups tab**: one group at a time (group buttons with Finished / "2 / 6"), table beside the matches; no snap scrolling.
- **Bracket tab**: clicking a match opens a side panel with the same scoring row.
- Known limits: someone opening the page mid-draft sees it from the next change; a draft of a closed tab stays visible until someone changes or confirms that match.

## Phase 1: match result endpoint
- `POST api/matches/{matchId}/result { matchId, standingId, tournamentId, teamAWins, teamBWins, version }` (match version).
- `MatchResultPipeline` (new folder `Pipelines/MatchResult`): checks (ids belong together, `CanScoreMatches`, match not decided, version, both teams known, valid counts for `BestOf`), writes the games in one transaction (loser's wins first, deciding game last, unplayed games empty), then runs the game result steps after `ScoreGameStep` unchanged; broadcasts `GameUpdated` (+ `TournamentUpdated` like the old pipeline) after commit.
- Tests: checks, game order, steps reached, broadcast.

## Phase 2: draft relay
- `TournamentHub.ShareMatchDraft(tournamentId, matchId, teamAWins, teamBWins)`: tenant check as in `JoinTournament`, sends `MatchDraftUpdated { tournamentId, matchId, teamAWins, teamBWins, updatedBy }` to the others in `tournament-{id}`.
- Tests: relayed to others; refused for another tenant's tournament.

## Phase 3: scoring row
- API `setMatchResult`; store drafts (map by match id; own changes + hub events; cleared by `GameUpdated` of that match); `confirmMatch`.
- `MatchCard`: stepper per team, draft label, Confirm, confirmed view. Bracket: match click opens a side panel with the row.
- Old per-game UI (tap = next game, game chips) removed from the frontend; the backend endpoint stays.

## Phase 4: Groups tab
- Group buttons, one group shown: table + matches side by side, fits one screen.

## Phase 5: docs
- CLAUDE.md (scoring, hub events, pipelines), this plan current.

## Later (after acceptance): done 2026-10-26
- Removed the old per-game endpoint, `GameResultPipeline`, `ScoreGameStep` and their tests.
- Match level only: the steps moved to `Pipelines/MatchResult` (`MatchResultContext`, `IMatchResultPipelineStep`); the pipeline sets the match winner itself, so `CheckMatchCompletionStep`, `BuildResponseStep`, `GameService.SetMatchWinner` and `SetGameResultDTO` are gone.
