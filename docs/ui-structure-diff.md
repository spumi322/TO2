# UI structure diff: old UI vs blind design

Compares the old Angular 17 UI (`src/UI`, about 5900 lines of ts/html) with the blind redesign (`docs/ui-redesign/design.md`) by structure: pages, components, state, workflows, API use, security. Visual design is out of scope. Result: what the new UI keeps, takes from the blind design, or drops, plus the backend changes it needs.

## 0. Decisions already made

| # | Topic | Decision |
|---|---|---|
| 1 | Base | New project on current Angular + PrimeNG (standalone, signals). Old UI stays as reference until replaced |
| 2 | Tokens | Access token in memory, refresh token in localStorage |
| 3a | Start buttons | `CanStartGroups`/`CanStartBracket` in `TournamentStateDTO` |
| 3b | Details DTO | `NumberOfGroups`, `AdvancingPerGroup`, `CurrentTeams` in `GetTournamentResponseDTO` |
| 3c | List `CreatedDate` | No |
| 3d | Add teams | Bulk endpoint `POST api/tournaments/{id}/teams { names }`, all-or-nothing, always new teams, one broadcast, remove deletes an orphan team, tournament `Version` bump against the capacity race |
| 3e | Team rename | No |
| 4 | Theme | Light + dark, follows system, toggle |
| 5 | GroupsOnly results | Keep several groups; group added to `TeamPlacementDTO` |
| 6 | Scoring | Winner only |
| 7 | BestOf | Bo3 only |
| 8 | Teams page | None, no name suggestions (teams are disposable) |
| 9 | Logged out | Landing + `/demo` (read-only demo tournament from JSON, frontend only) |

## 1. Pages and routes

| Old route | Old page | Blind | New UI |
|---|---|---|---|
| `/` | Landing (marketing, static preview) | none (login) | Keep landing; link to `/demo`; drop the false "pick best-of series" claim |
| `/login`, `/register` | Login, register | same + guestGuard | Same pages + guestGuard |
| `/tournaments` | List, 4 sections | same | Same |
| `/create-tournament` | Create form + live preview | `/tournaments/new` | `/tournaments/new` |
| `/tournament/:id` | One page, PrimeNG tabView (tab = index number) | `/tournaments/:id/{overview,teams,groups,bracket,results}` child routes | Child routes (fixes Known Issue 2: tab reset on reload) |
| `/tournament/:id/team/:teamId` | "Coming soon" stub | none | Drop |
| none | | `/teams` library | Drop (Q8) |
| none | | `/organization` (org name + invite code) | Drop: invite code stays a dialog in the user menu (no other org data exists) |
| none | | `**` not found | Add |
| none | | none | `/demo` (Q9) |

## 2. Tournament page composition

**Old:** `TournamentDetailsComponent` (737 lines ts) loads everything, handles every SignalR event, runs every action, owns tab indices. Start/cancel/delete buttons live in the global navbar, which talks to the details page through trigger Subjects in `TournamentContextService`.

**New:** tournament shell route = header (name, status, format, primary action, kebab menu) + tab strip + `<router-outlet>`. Each tab is a small page reading from a `TournamentStore` provided on the shell route. Actions sit in the tournament header, so the navbar only does global navigation and the trigger Subjects go away.

| Area | Old | New |
|---|---|---|
| Overview | Info card (group size read from `standings[0].maxTeams`), team add card, top-4 card, team list all on one tab | Facts from the details DTO (3b), "next step" card, champion card when finished |
| Teams | On Overview; add card shown by `isRegistrationOpen` | Own tab: comma input + one bulk call (3d), list with remove; edit gated by `canModifyTeams` |
| Groups | Tab shown by `!isRegistrationOpen` + format | Tab shown by format; empty state before start |
| Bracket | `brackets-viewer` lib + adapter (131 lines) + DOM click handlers + `setTimeout` render | Keep `brackets-viewer` behind one `BracketView` wrapper: pure tested adapter, re-render on input change, match click opens the shared `MatchCard` |
| Results | Results tab + top-4 card; tiers Top 8/16/32 | Results tab: podium + table; shared ranks as ranges; group label for GroupsOnly (Q5) |
| Start actions | Navbar buttons from format + status + `isRegistrationOpen` + team count | Header primary button from `canStartGroups`/`canStartBracket` (3a) |
| Cancel/delete | Navbar kebab from `canCancel`/`canDelete` | Same rule, moved into the tournament header |

## 3. Components

| Old component | Lines (ts+html) | Fate |
|---|---|---|
| `TournamentDetailsComponent` | 854 | Split: shell + store + tab pages |
| `NavbarComponent` | 282 | Rewrite: global nav + user menu only (no tournament actions) |
| `BracketComponent` + `BracketAdapterService` | 454 | Rewrite as `BracketView` wrapper + pure adapter; scoring through `MatchCard` popover |
| `MatchesComponent` | 173 | Replace with `MatchCard` (shared by groups and bracket) |
| `GroupComponent` | 83 | `GroupTable` + match list of `MatchCard`s |
| `CreateTournamentComponent` | 638 | Port the form + preview; keep `TournamentConfigService` logic |
| `TeamManagementCardComponent` + `RegisteredTeamsListComponent` | 248 | Merge into the Teams tab |
| `TournamentInfoCardComponent`, `TopResultsCardComponent` | 108 | Overview facts + champion card |
| `FinalStandingsDisplayComponent` | 87 | `PlacementTable` + podium |
| `TournamentListComponent` | 190 | Port; list store applies hub events |
| `Login`/`Register` | 366 | Port; shared error helper instead of 40 duplicated lines each; org check on blur |
| `ConfirmDialogComponent`, `InviteCodeDialogComponent` | 372 | Keep concept; replace remaining native `confirm()` (start groups/bracket, remove team) |
| `TeamBadgeComponent` | 50 | Optional (cosmetic) |
| `LandingComponent` | 260 | Keep, plus `/demo` link |
| `TournamentHeaderComponent`, `TournamentStateBannerComponent`, `TeamManagementComponent` | 152 | Drop (unused / commented out / stub) |
| `TournamentStatusLabel` + `StatusConfig` | 78 | One enum label map (two label sources today) |
| `FormatConfig` | 45 | Labels only; `requiresGroups/Bracket` used only for tab visibility |
| New from blind | | `StatusChip`, `StageStepper`, `MatchCard`, `GroupTable`, `BracketView`, `PlacementTable`, `EmptyState`/`ErrorState`, connection dot, not-found page |

## 4. State and data flow

| Topic | Old | New |
|---|---|---|
| Tournament state | Root singleton `TournamentContextService` (BehaviorSubjects) + copies in the details component + navbar | `TournamentStore` (signals) per open tournament, provided on the shell route; destroyed on leave |
| Load | `tournament` then `teams` chained; standings loaded after; state loaded separately; `reloadTournamentData` reassigns `tournament$` (rebuilds the whole view) | `load()` fetches tournament, state, teams, groups, bracket in parallel; reloads update signals in place |
| Team names | `tournament.teams` passed down as inputs | Store keeps `Map<id, name>` from `GET {id}/teams` |
| Hub connection | Started lazily by `setTournament`; join/leave inside the context service | Realtime service: connect after login, stop on logout; shell joins/leaves; on reconnect rejoin + reload |
| Events | 11 handlers in the details page; tenant events not filtered (Known Issue 1); listens to `MatchUpdated`/`StandingUpdated`, which the backend never sends | Store applies events, filters tenant events by `tournamentId`; only events the backend sends |
| Own events | Skipped by comparing `updatedBy` with username | Not needed: applying `GameUpdated` is idempotent; toasts only for other users |
| Timing hacks | `setTimeout` for tab switch (500 ms), bracket render (50/100 ms), navigation after create (1500 ms) | None needed (routes, own bracket component) |
| Dead code | `loadBracketMatches`/`bracketMatches` (fetches raw matches, never shown), `getAllTournaments` (`/all` does not exist), `getAllTournamentsWithTeams`, `getStandingsByTournamentId`, `getAllGamesByMatch`, `matchFinished` outputs nobody binds, `teamsPerGroup/teamsPerBracket` model fields | Not ported |
| Final standings | Loads from API after `tournamentFinished` (comment says the event payload is empty) | Use the event/response `finalStandings`: the backend now reads tracked entities, so the payload is filled (stale comment) |

## 5. Workflows

**Register / join org**
- Old: org check on every keystroke (400 ms debounce): shares the 10/min auth rate limit with login/register. Invite field shown on exists or on a 409 mentioning "invite code".
- New: check on blur; same invite-field logic; 429 handled inline.

**Create tournament**
- Old: form + live preview (good); after submit waits 1.5 s, then opens the details page.
- New: same form/preview (port `TournamentConfigService`); navigate straight to the Teams tab.

**Add / remove teams**
- Old: comma input; per name: reuse an org team by name or create it, then register (2 calls); client capacity/duplicate checks (inconsistent case handling); partial batches; orphan teams.
- New: same comma input; one call (3d); client checks only for instant feedback; 409 `detail` as toast. Remove: confirm dialog, then `DELETE`.

**Start groups / bracket**
- Old: navbar decides the button from format/status/team count; native `confirm()`; the GroupsAndBracket `PUT {id}/start` path is unreachable (no button calls it).
- New: header button from `canStartGroups`/`canStartBracket`; `ConfirmDialog` with summary (team count, groups, advancing from 3b); on success go to the Groups/Bracket tab.

**Score games (winner only)**
- Old groups: `+` button per team scores the next unscored game, with an optimistic update. Old bracket: click a team inside the library's DOM. No way to fix a mis-tap.
- Blind: dialog per match, one row per game.
- New: `MatchCard` used in groups and bracket: tap a team = it wins the next game (keeps the old one-tap speed); played games shown as small chips, tapping one lets you change its winner until the match has a winner (backend allows re-scoring with the current version). No dialog.

**Live updates, cancel/delete, results:** see sections 2 and 4.

## 6. API usage

| Endpoint | Old UI | New UI | Note |
|---|---|---|---|
| `POST auth/login`, `register`, `refresh` | yes | yes | |
| `POST auth/revoke` | no (logout only clears storage) | yes | Logout should revoke the refresh token |
| `GET auth/me` | no | no | Cleanup candidate (Known Issue 8 disappears with it) |
| `GET auth/organizations/exists` | per keystroke | on blur | |
| `POST tenant/invite-code` | yes | yes | |
| `GET tournaments`, `{id}`, `{id}/teams`, `{id}/state`, `{id}/final-standings` | yes | yes | `{id}` + 3b fields, `state` + 3a flags, final standings + group (Q5) |
| `POST tournaments`, `{id}/start-groups`, `{id}/start-bracket`, `{id}/cancel`, `DELETE {id}` | yes | yes | |
| `PUT tournaments/{id}` (edit name/description) | no | ? | Not used by any UI (see questions) |
| `PUT tournaments/{id}/start` | unreachable | no | Cleanup candidate |
| `GET standings/{id}/groups`, `/bracket` | yes | yes | |
| `GET standings/{id}` | defined, unused | no | Cleanup candidate (returns entities) |
| `GET matches?standingId=` | dead `loadBracketMatches` | no | Cleanup candidate (returns entities) |
| `GET matches/{id}/games` | defined, unused | no | Cleanup candidate |
| `PUT matches/{m}/games/{g}/result` | yes | yes | |
| `GET teams`, `GET teams/{id}`, `POST teams`, `POST teams/{tid}/teams/{teamId}` | yes (add flow) | no | Replaced by 3d; cleanup candidates |
| `DELETE teams/{teamId}/{tournamentId}` | yes | yes | Odd route; could move to `DELETE tournaments/{id}/teams/{teamId}` next to 3d |
| `POST tournaments/{id}/teams` | | yes | New (3d) |
| Hub `MatchUpdated`, `StandingUpdated` | listened | no | Never sent; cleanup candidates in `ISignalRService` |

## 7. Security and error handling

| Topic | Old | New |
|---|---|---|
| Token storage | Both tokens in localStorage | Access in memory, refresh in localStorage (Q2) |
| User info | `jwt-decode` of the access token | From `AuthResponse` (user + tenant + `expiresAt`); drop `jwt-decode` |
| Refresh | Guard and interceptor refresh independently: with rotation, two refreshes at once revoke each other and log the user out | One shared in-flight refresh per tab; before refreshing re-read localStorage (another tab may have rotated it). Blind's `navigator.locks` cross-tab lock not needed at this scale |
| Expiry | Refresh only after a 401 | Same + refresh on app start (access token is in memory) |
| Logout | Clears storage, navigates; no revoke, hub keeps running | Revoke, stop hub, clear session |
| Guards | `AuthGuard` (canActivate) | `authGuard` + `guestGuard`; login uses `returnUrl` (old ignores it) |
| Errors | Each component parses ProblemDetails itself (login/register duplicate 40 lines); generic texts hide `detail` (cancel/delete) | One error helper: 400 field errors to form controls, 409/400 `detail` as toast, 429 inline, 500 with `traceId`, 0 = offline banner |
| Action gating | Mix of state flags and `isRegistrationOpen`/status/format checks | State flags only (3a) |
| Versioned writes | Game `version` sent; 409 toast (+ reload in bracket only) | Game `version` sent; 409 toast + reload the standing in both views |

## 8. Simplification candidates

Frontend only:
1. Tabs as child routes (fixes Known Issue 2).
2. Per-tournament store instead of root context service + navbar trigger Subjects.
3. `brackets-viewer` behind one wrapper with a pure adapter; no `window` access outside it, no timers, no per-element click handlers.
4. One-tap scoring with correctable game chips, same component in groups and bracket.
5. Event filtering by `tournamentId` (fixes Known Issue 1); drop listeners for unsent events.
6. One error helper; one status label map; drop dead code (section 4).
7. Native `confirm()` replaced by `ConfirmDialog` everywhere.
8. Logout revokes; single-flight refresh.

Needs backend (accepted): 3a, 3b, 3d, Q5.

Backend cleanup once the old UI is removed: `PUT tournaments/{id}/start`, `GET standings/{id}`, `GET matches`, `GET matches/{id}/games`, `GET teams`, `GET teams/{id}`, `POST teams`, `POST teams/{tid}/teams/{teamId}`, `GET auth/me`, `BroadcastMatchUpdated`/`BroadcastStandingUpdated` (defined in `ISignalRService`, never called).

## Answered questions

Defaults taken, see `docs/ui-rebuild-plan.md` (Decisions): no `/organization` page, one-tap scoring with correctable game chips, start flags include >= 2 teams, no tournament edit page, team remove route unchanged, cleanup endpoints removed in phase 0 together with the old UI, team badge kept.
