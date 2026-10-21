# Tournament list (main page after login)

## Context
`/tournaments` shows every tournament as a card in Ongoing / Registration / Finished / Cancelled sections. With real use (asd1: 77 tournaments) it is 3.5 screens long, Finished (53) dominates, cards repeat the section name as a status chip, "4 / 7 teams" on started tournaments reads as missing teams, cards have no winner or date and uneven heights. Research (Liquipedia, HLTV, Challonge, start.gg, Battlefy, Toornament): active work first, history behind a tab or filter, finished = winner + date, live = stage progress. Design mockups: canvas https://claude.ai/artifact/9EE5MFEWHnM9R5yUP3Q9n7, chosen layout **E1** (D3 layout), colors from the **Green ramp** board.

Work phase by phase.

## Decisions
- **Layout**: page header (title + New tournament), then **Live now** (all ongoing tournaments as rows), then **tabs**: Registration / Finished / Cancelled with counts, one list visible at a time.
- **Live row**: format icon tile (tinted by stage) + name | stage track | stage chip | format | team count ("8", no max).
- **Stage track**: segments per format, from `Format` + `Status` only (no backend): BracketOnly `Registration > Playoffs > Finished`, GroupsOnly `Registration > Groups > Finished`, GroupsAndBracket `Registration > Groups > Playoffs > Finished`. Done = dim green, current = stage color, next = grey; `GroupsCompleted` = Groups done + Playoffs outlined. Pure function, unit tested.
- **Registration row**: icon + name | fill bar | format | "2 / 8" or "Full".
- **Finished row**: name | trophy + winner | format | teams | finished date, newest first.
- **One screen**: Live now shows all rows by default ("Show less" = half of the rows that fit); the open tab gets the rows left on the screen (min 3, recomputed on resize); "Show all N" / "Show less" in the section heading row (client side, the list is already loaded).
- **Cancelled row**: name | reason | teams | cancelled date.
- **No** descriptions in the list (overview tab has them), no search, no pagination, no archive, no scheduling dates.
- **Colors (green ramp)**: one green, darker stage by stage: registration `#c8e6c9`, groups `#81c784`, playoffs TO2 green `#43a047` (light theme: `#4f7f53`, `#388e3c`, `#1b5e20`). Reached stages keep their shade on the track; the Registration segment fills to registered / max teams like the Slots bar (same shade); ready for playoffs = playoffs green as outline; the Finished segment stays grey (a finished tournament leaves Live now). Gold (`--status-finished`) only for winners/trophies, never for a state.
- **Colors are app-wide**: the ramp goes into the `--status-*` tokens, so status chips look the same on every page. Seeding states (`SeedingGroups`, `SeedingBracket`) move from orange to neutral grey (the spinner shows progress). Light theme gets darker shades for contrast.
- **Labels**: `GroupsCompleted` = "Ready for playoffs".
- **Default tab**: Registration; Finished when Registration is empty. Not remembered between visits.
- **Commits**: one per phase (phase 1 includes this plan). CLAUDE.md is gitignored: phase 3 edits stay local.
- **Stuck seeding tournaments** (3 in asd1): Known Issue, no data fix.
- **Backend** (list DTO only, no new endpoint):
  - `WinnerName` (string?): the team with `TournamentTeam.FinalPlacement == 1` when exactly one team has it, else null. GroupsOnly with several groups has one placement-1 team per group (placement = group position): no single winner, the UI shows "Several group winners" for a finished GroupsOnly tournament without a winner.
  - `FinishedAt` (DateTime?): `LastModifiedDate` when `Status == Finished`, else null (nothing changes a finished tournament afterwards).
  - `CancelledAt`, `CancellationReason`: already on `Tournament`, mapped.
  - One query: `GetAllWithTeamCountAsync` projects the winner name next to the team count.
- **Realtime**: the list reloads (debounced) on the tenant group events. Start groups/bracket already send `TournamentUpdated` to the tenant group; a game result that changes the status (`GroupsCompleted`, `Finished`) only sends `GameUpdated` to the tournament group, which the list never joins. Fix: `GameResultPipeline` also sends `TournamentUpdated` (tenant group) when `NewTournamentStatus` is set, the same pattern as the start pipelines.
- **Data check per element**: live row = name, format, status, current teams (all in the DTO today); registration = current/max teams (today); finished = winner, finished date (new); cancelled = reason, date (new, already on `Tournament`). `FinishedAt` from `LastModifiedDate` holds because nothing in the app edits a finished tournament (`PUT api/tournaments/{id}` has no UI).

## Phase 1: backend
- `GetTournamentListResponseDTO`: `WinnerName`, `FinishedAt`, `CancelledAt`, `CancellationReason`.
- `TournamentRepository.GetAllWithTeamCountAsync` → also selects the winner name (subquery on `TournamentTeams` with `FinalPlacement == 1` → `Team.Name`); return type becomes a small record instead of a tuple.
- `TournamentMappings.ToGetTournamentListResponseDTO(tournament, currentTeams, winnerName, finishedAt)`; `TournamentService.GetTournamentListAsync` computes `finishedAt`.
- `GameResultPipeline.BroadcastUpdatesAsync`: also `BroadcastTournamentUpdated(tournamentId, user)` when `context.NewTournamentStatus` is not null (inside the existing try, after commit).
- Tests (`TournamentServiceTests`, SQLite integration): finished tournament → winner name + `FinishedAt`; ongoing → both null; cancelled → `CancelledAt` + reason; team count unchanged. `GameResultPipeline`: status change → `TournamentUpdated` sent; plain game → not sent.

CONFIRM: tests green; `curl GET api/tournaments` on the real API shows winners for asd1's finished tournaments. Commit.

## Phase 2: UI
- `core/models.ts`: new list item fields.
- `styles.css`: green ramp in `--status-setup`, `--status-groups`, `--status-groups-done`, `--status-bracket`, grey `--status-seeding` (dark + light theme); `shared/enums.ts`: `GroupsCompleted` label "Ready for playoffs", `outline` flag; `StatusChip` renders the outline variant.
- `features/tournaments/stage-track.ts` (+ spec): `stageSteps(format, status)` → `{ label, state }[]`; small `StageTrack` component.
- `tournament-list-page`: Live now rows, tabs (signal), Registration / Finished (latest 10 + show all, sorted by `finishedAt` desc) / Cancelled rows; rows are links; tables scroll horizontally on phones; empty states per list; realtime reload unchanged.
- Remove the old card styles.

CONFIRM: build + tests green; short browser check with asd1 (live rows, tabs, show all, phone width, light theme; score the deciding game in a second tab → list moves the tournament to Finished with its winner without reload). Commit.

## Phase 3: docs
- CLAUDE.md: list DTO fields, list page structure, status colors rule (gold = winners only).
- Known Issues: 3 asd1 tournaments stuck in `SeedingBracket` (old data, shown under Live now as "Seeding bracket").

## Out of scope
Search, filters, pagination, archive, scheduled dates, runner-up, match progress (played/total), logos.

## Stop and ask
- Winner query needs more than one extra subquery, or changes the list endpoint's shape beyond the 4 fields.
- Status color change breaks readability elsewhere (match card, group table, landing page use `--status-groups`).
