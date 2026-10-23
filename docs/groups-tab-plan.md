# Groups tab: groups board (design A)

## Context
The Groups tab shows one group at a time (group buttons), its table beside a long list of `MatchCard`s. Replaced by design **A** from the canvas https://claude.ai/artifact/EaNZdKZC6vfVCcgQak4bWu (boards "A - Groups board", "A edge - 2 groups of 16", "A edge - 10 groups of 3 and 2"). Reference: Liquipedia and Challonge (all groups on one page, table + matches per group). Frontend only; the group DTOs already carry everything (teams with stats, matches with round, teams, winner, wins).

## Limits it must handle
- Teams 4-32 (start with at least 2 registered), groups 1-10, at least 3 teams per group at creation, so a group has 3-32 teams; `AdvancingPerGroup` below the group size (GroupsAndBracket).
- Round robin: n teams = n(n-1)/2 matches over n-1 rounds (n odd: n rounds, one team rests each round). 4 teams: 6 matches; 6 teams: 15; 16 teams: 120; 32 teams: 496.
- **Designed for regular use**: up to 4 groups of 4 fit one screen. Bigger configurations (more or bigger groups) scroll the page normally; no snap blocks, no special layouts for them.

## Decisions
- **All groups on one page**, no group picker. A summary line on top: "16 of 24 matches played · Top 2 of each group advance · Best of 3 · Ranked by match wins, then game difference" (advance part only for GroupsAndBracket).
- **Cards per row**: plain CSS grid, `repeat(auto-fit, minmax(340px, 1fr))` (no count rule in code, few groups stretch); phones: 1.
- **Group card**: name, progress ("4 / 6 played") or a "Finished" chip; table; matches.
  - Table: `#`, Team, Form (last 5 results as W/L pills, oldest first), W-L, +/- (game difference). Cut line under the last advancing place; rows tinted advanced/eliminated once the group is finished (as now). Order from the server (`GroupRanking`), the UI only displays.
  - Matches: all of the group's matches in round order as lines (as many per row as fit at 220px), each with its round tag (R1, R2, ...). Big groups simply make a long card (scroll).
  - Match line = a button: `Team A  2–1  Team B`, winner bold and loser muted once decided; "vs" when open; a confirmed match has a green line and score, a draft in progress shows its score in white (from `store.drafts`, live via `MatchDraftUpdated`); "TBD" never happens in groups (teams known at the draw).
- **Scoring**: clicking a match line pops up the match panel centered on the click (under the line, above it near the bottom of the screen; a docked panel made the cards jump to another row count); it closes on ×, Esc, a click outside, or once the result is saved: group + round + best of, `MatchCard` (draft `−`/`+`, Confirm); the clicked line is outlined green. Read-only tournaments (demo, not scoring state) open the same panel without the controls (`MatchCard` already handles `canScore`).
- Unchanged: `GroupTable` stays the one table component (Overview uses its compact mode); `Scoring`, `TournamentStore`, drafts, `confirmMatch`, hub events.

## Phase 1: pure logic
- `tabs/groups-board.ts` (pure, unit tested):
  - `teamForm(group, teamId, last = 5)`: W/L of the team's decided matches in round order
  - `matchLines(group, teamNames, drafts)`: matches in round order as `{ id, round, a, b, score, state: 'done' | 'draft' | 'open', winner }`
- Specs for each, incl. an odd group (a team rests each round) and a finished group.
- Checkpoint: tests green, commit. **CONFIRM 1**: rules read right.

## Phase 2: Groups tab UI
- `GroupTable`: optional `form` column (input `showForm`, form from `teamForm`); Overview unchanged.
- `GroupsTab` rewritten: summary line, card grid, match lines, docked match panel with `MatchCard`; picker, `selected`, `rule()` and the old match list removed; empty state while not drawn stays.
- Browser check (short, own client): 4 groups of 4 live (one screen), 2 groups of 16 and 10 groups of 3 (scroll, nothing breaks), GroupsOnly, finished group tints, `/demo` read-only, light theme, phone width.
- Checkpoint: build + tests green, commit. **CONFIRM 2**: looks right in the app.

## Phase 3: docs
- CLAUDE.md frontend section (Groups tab line, layout rule exception: Groups tab scrolls for big configurations).
- Checkpoint: CLAUDE.md is gitignored, no commit.

## Out of scope
- Backend changes; match schedules/dates; crosstable view; round stepper or other big-group layouts; form column on the Overview's compact tables; animations; the Bracket tab's panel (backlog item 4, may later share the panel with this tab).

## Stop and ask
- The group DTO lacks something the design needs (would need a backend change).
- The regular case (4 groups of 4) does not fit one screen at 1440x900 without shrinking text below 12px.
- `MatchCard` needs changes that would alter the Bracket tab.
