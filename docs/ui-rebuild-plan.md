# UI rebuild plan

New Angular frontend built from scratch in `src/UI`, structure from `docs/ui-structure-diff.md`. Phase 0 removes the old UI; old files stay readable from git history (`git show dev:src/UI/...`) for porting.

Scope: junior reference project. Keep each piece small and explainable; polish goes to phase 6.

Working rules:
- Each phase is split into checkpoints. A checkpoint is done when it builds and its tests pass; then it gets one commit (short lowercase message).
- Git: only `diff` and `commit`; anything else needs approval. Branches are not touched.
- Each phase ends with a CONFIRM point before the next one starts.

## Decisions

From the diff (section 0) plus these defaults for its open questions:
- No `/organization` page; invite code stays a dialog in the user menu.
- Scoring: tap a team = it wins the next game; played games as chips, tap one to change its winner until the match has a winner. No dialog.
- `CanStartGroups`/`CanStartBracket` also require >= 2 registered teams (no team-count rule in the client).
- No tournament edit page (`PUT tournaments/{id}` stays unused).
- Team remove keeps its current route.
- Team badge ported as is.
- Bracket keeps `brackets-viewer`, behind one clean wrapper component (third-party integration as a lesson).

## Phase 0: cleanup

- **CP 0.1** Delete `src/UI`. Remove `docs/angular-upgrade-plan.md` (superseded).
- **CP 0.2** Backend cleanup, plus their services/DTOs/tests if nothing else uses them; `dotnet test` green.
  - Endpoints:
    - `PUT tournaments/{id}/start`
    - `GET standings/{id}`
    - `GET matches`, `GET matches/{id}/games`
    - `GET teams`, `GET teams/{id}`
    - `POST teams`, `POST teams/{tid}/teams/{teamId}`
    - `GET auth/me`
  - Never-called `BroadcastMatchUpdated`/`BroadcastStandingUpdated`.
  - Bulk add teams (phase 1) replaces the removed add-team endpoints.
- **CP 0.3** CLAUDE.md:
  - Frontend section shortened to "being rebuilt, see `docs/ui-rebuild-plan.md`".
  - Removed endpoints dropped from the docs.
  - Known Issues 1, 2, 3, 4, 7, 8 closed.

**CONFIRM 0**

## Phase 1: backend changes

- **CP 1.1** `TournamentStateDTO`: `CanStartGroups`, `CanStartBracket` (state machine `IsTransitionValid` to the format's seeding state + >= 2 teams). Tests per status/format/team count.
- **CP 1.2** `GetTournamentResponseDTO`: `CurrentTeams`, `NumberOfGroups` (count of group standings), `AdvancingPerGroup`. Tests.
- **CP 1.3** Bulk add teams: `POST api/tournaments/{id}/teams { names }` → `[{ id, name }]`.
  - Validator: 1-32 names; each trimmed, not empty, at most 24 chars; no duplicates ignoring case.
  - Service, all-or-nothing:
    - 404 if the tournament is missing.
    - 409 if `CanModifyTeams` is false.
    - 409 "Only N slots left" when there is no room.
    - 409 naming any name already registered (ignoring case).
    - Always creates new teams.
    - One save, with a tournament `Version` bump (concurrent batches get a 409).
  - `BroadcastTeamAdded` takes a list of team ids (one event per batch).
  - Remove team: also deletes the team row when it is in no other tournament; same `Version` bump.
  - `Team` name trimmed in the domain.
  - Tests: happy path, 404/409 cases, nothing saved on failure, trim/case duplicates, remove deletes orphan team.
- **CP 1.4** `TeamPlacementDTO`: nullable `GroupName`, set when the placement comes from a group position (GroupsOnly, group-stage knockouts). Tests.
- **CP 1.5** CLAUDE.md updated for 1.1-1.4.

**CONFIRM 1**

## Phase 2: app skeleton and auth

- **CP 2.1** New Angular project in `src/UI` (current major, standalone, signals, `@if/@for`).
  - PrimeNG current major with an Aura-based preset.
  - Theme tokens as CSS variables for light and dark (follows system; toggle UI in phase 6).
  - `npm start` against the local API; CORS origins checked.
- **CP 2.2** Layout shell: top bar (logo, Tournaments, user menu), toast region, not-found page, routes with lazy feature pages.
- **CP 2.3** Core auth:
  - Session store: user/tenant from `AuthResponse`, access token in memory.
  - Refresh token in localStorage.
  - Single-flight refresh, re-reading localStorage first; refresh on app start.
  - Auth interceptor: bearer token on API calls; on 401, refresh once and retry once.
  - `authGuard`, `guestGuard`.
- **CP 2.4** Error helper: ProblemDetails → field errors / `detail` toast / 429 inline / 500 with `traceId` / offline. Unit tests.
- **CP 2.5** Pages and logout:
  - Login, with `returnUrl` (relative paths only).
  - Register: org check on blur, invite field when the org exists, 409/429 handling.
  - Invite code dialog in the user menu.
  - Logout revokes and clears the session.

Done when: register (new org and join with code), login, reload keeps the session, logout revokes, expired access token refreshes silently.

**CONFIRM 2**

## Phase 3: tournament list, create, realtime

- **CP 3.1** Enum label maps (status, format, team status), `StatusChip`, `FormatBadge`, `EmptyState`.
- **CP 3.2** Realtime service:
  - Connects after login/session restore, stops on logout, reconnects automatically.
  - Typed event streams, only for events the backend sends.
- **CP 3.3** Tournament list: sections (upcoming, ongoing, finished, cancelled), reloads on tenant events.
- **CP 3.4** Create tournament:
  - Form + live preview (port `TournamentConfigService` + its unit tests).
  - Server 400 field errors mapped to the form.
  - On success, go to the new tournament's Teams tab.

Done when: create each format, see it in the list; a second browser session sees new/cancelled/deleted tournaments appear live.

**CONFIRM 3**

## Phase 4: tournament page, teams, groups

- **CP 4.1** Shell + store:
  - Route `/tournaments/:id` with child routes (overview, teams, groups, bracket, results); tab visibility from format.
  - `TournamentStore` on the shell route: tournament, state, teams (+ id → name map), groups, bracket, results; parallel load, in-place reloads.
- **CP 4.2** Live updates:
  - Store joins the hub on enter, leaves on leave; rejoins + reloads on reconnect.
  - Events filtered by `tournamentId`; `GameUpdated` applied without refetch.
  - Unit tests for event handling.
- **CP 4.3** Header and Overview:
  - Header: name, status, format; primary action from `canStartGroups`/`canStartBracket`.
  - Kebab menu: delete (`canDelete`), cancel with reason (`canCancel`); `ConfirmDialog` for all of these.
  - Overview: facts from the details DTO, next-step card, cancelled banner.
- **CP 4.4** Teams tab:
  - Comma input + one bulk call; client checks for instant feedback only.
  - Team list with remove (confirm).
  - Read-only when `canModifyTeams` is false.
- **CP 4.5** Groups tab:
  - `GroupTable` (rank, W-L, game diff, points, team status).
  - `MatchCard`:
    - Tap a team = it wins the next game; game chips to correct a winner.
    - Sends the game `version`; on 409: toast + reload the standing.
    - Enabled only when `canScoreMatches`.

Done when: a GroupsOnly tournament runs from Setup to Finished in the new UI, with two sessions staying in sync and the tab kept on live updates.

**CONFIRM 4**

## Phase 5: bracket and results

- **CP 5.1** `BracketView`: wrapper around `brackets-viewer` (first check it installs and renders with the current Angular).
  - The only place that touches the library.
  - Input: bracket from the store, team name map, `canScoreMatches`. Output: match clicked (matchId).
  - Adapter = pure function, bracket DTO → library data, with unit tests.
  - Re-renders on input change (no timers).
  - Clicks: the library's match-click callback, else one container listener.
  - Theme through the library's CSS variables.
  - Match click opens the `MatchCard` in a popover (same scoring as groups).
  - Start bracket from GroupsCompleted; BracketOnly starts straight into the bracket.
- **CP 5.2** Results tab and champion card:
  - Results tab: podium + `PlacementTable` (shared ranks as ranges, `GroupName` label where set).
  - Champion card on Overview.
  - Uses `finalStandings` from the game result / `GameUpdated`, else `GET final-standings`.
- **CP 5.3** End-to-end check of all three formats (`docs/tools/simulate_tournament.py` for data); fixes found go in this checkpoint.

Done when: BracketOnly and GroupsAndBracket run to Finished with correct results in two synced sessions.

**CONFIRM 5**

## Phase 6: extras

- **CP 6.1** Landing page ported from history (drop the "pick best-of series" claim), link to `/demo`.
- **CP 6.2** `/demo`: read-only tournament page from a JSON file (overview, groups, bracket, results), no login, no API.
- **CP 6.3** Theme toggle (system / light / dark, saved in localStorage); every page checked in both modes.
- **CP 6.4** Team badge; phone width pass (16 px gutters, scrollable tab strip, bracket scrolls inside itself).
- **CP 6.5** CLAUDE.md Frontend section written for the new UI.

**CONFIRM 6**

## Frontend tests

Unit tests with the test runner the Angular CLI sets up; every test passes (no generated boilerplate specs: generate with `--skip-tests`).
- Pure logic: `TournamentConfigService` (group split, bracket size, BYEs, matchups).
- Error helper: ProblemDetails → field errors / toast / 429 / 500 with `traceId`.
- Auth: two 401s at once trigger one refresh; failed refresh signs out; interceptor adds the token only to API calls.
- Store: tenant events of another tournament ignored; `GameUpdated` replaces the right standing; reconnect reloads.
- Bracket adapter: rounds, BYE/TBD slots, winners, series score.
- One component test: `MatchCard` (tap scores the next game for that team; disabled without `canScoreMatches`; game chip changes a winner).

Not tested: page layouts, PrimeNG rendering, e2e.

## Out of scope

Teams page, team rename, BestOf choice, public spectator link, httpOnly cookie refresh, tournament edit page, roles, NgRx/Tailwind.
