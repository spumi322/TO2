# TO2 UI redesign

New Angular frontend for TO2. Designed against the backend contract only (controllers, DTOs, state machine, auth, SignalR hub).
Demo pages: `docs/ui-redesign/demo/` (open `index.html` from disk).

Principles:
- Organizer tool first: one tournament screen shows status, next action, live data.
- Backend decides rules: action buttons come from `GET api/tournaments/{id}/state` flags; client never maps status -> allowed action.
- Live by default: SignalR keeps open screens current; no manual refresh needed.
- Small and explainable: signals + thin API services, no state library, one component lib.

## 1. Stack

- Angular current major (standalone components, signals, built-in control flow `@if/@for`, functional guards/interceptors). Rebuild is a new app, so start on the current major instead of 17 (out of support).
- PrimeNG current major, styled mode, own preset built on Aura via design tokens (palette below). Used for: dialog, drawer, menu, toast, autocomplete, input number, select button, tooltip, skeleton.
- No Tailwind, no NgRx. Layout + custom components (bracket, group table, match card) in plain CSS with the shared CSS variables.
- `@microsoft/signalr` for the hub. No `jwt-decode`: `AuthResponse` already carries user + tenant + `expiresAt`.
- Fonts: Inter (UI, tabular numbers for scores), Space Grotesk (headings, big numbers). Self-hosted via `@fontsource`.

## 2. Frontend structure

### Routes

| Route | Page | Guard |
|---|---|---|
| `/login` | Sign in | guestGuard |
| `/register` | Create account: create org or join org with invite code | guestGuard |
| `/tournaments` | Tournament list (dashboard) | authGuard |
| `/tournaments/new` | Create tournament form with live preview | authGuard |
| `/tournaments/:id` | Tournament shell (header, stage stepper, tabs); children below | authGuard |
| `/tournaments/:id/overview` | Default tab: next action, key facts, recent activity | |
| `/tournaments/:id/teams` | Registered teams; add/remove while `canModifyTeams` | |
| `/tournaments/:id/groups` | Group tables + group matches (formats with groups) | |
| `/tournaments/:id/bracket` | Bracket view (formats with bracket) | |
| `/tournaments/:id/results` | Final standings (Finished only) | |
| `/teams` | Organization team library (list, create) | authGuard |
| `/organization` | Org name, members-join info, generate invite code | authGuard |
| `**` | Not found | |

- Tabs are child routes: reload and deep links keep the tab; a live reload never resets it.
- Tab visibility from format: Groups tab for `GroupsOnly`/`GroupsAndBracket`, Bracket tab for `BracketOnly`/`GroupsAndBracket`, Results only when `Finished`. Tabs whose stage has not started show an empty state ("Groups are drawn when you start the tournament").
- Match scoring is a dialog (desktop) / bottom sheet (phone) opened from any match card; URL query `?match=<id>` so it survives reload and can be linked.

### Layout shell

- Top bar: logo, org name, nav (Tournaments, Teams, Organization), right side: live connection dot, theme toggle, user menu (username, email, Invite code, Sign out).
- Phone (< 768 px): nav collapses into a menu drawer; tournament tabs become a horizontally scrollable tab strip.
- Content max width 1200 px (bracket tab may use full width with horizontal scroll inside the bracket only).
- Global: toast region (top right / bottom on phone), offline/reconnecting banner under the top bar.

### Feature areas (folders)

- `core/auth`: session store, auth API, token refresh, guards, auth interceptor.
- `core/http`: error interceptor, ProblemDetails parsing, API base URL.
- `core/realtime`: hub connection service, typed event streams.
- `core/ui`: theme service (light/dark/system), toast service, page title.
- `features/auth`: login page, register page.
- `features/tournaments`: list page, create page.
- `features/tournament`: shell + tabs (overview, teams, groups, bracket, results), match dialog, tournament store.
- `features/teams`: team library page.
- `features/organization`: org page, invite code dialog.
- `shared`: presentational components and pipes (below), enum label maps.

### Shared components (presentational, no HTTP)

- `StatusChip`: status label + color (+ pulsing dot for active states, spinner for seeding).
- `StageStepper`: Registration -> Groups -> Bracket -> Finished, steps per format, current step highlighted; Cancelled shows a red end marker. Display only.
- `FormatBadge`: Groups / Bracket / Groups + Bracket.
- `TeamName`: team id -> name via the tournament team map; `TBD` for unknown (null or 0), `BYE` for an empty bracket slot.
- `MatchCard`: two team rows, series score (`teamAWins`/`teamBWins`), winner bold, loser dimmed, Bo label, "Live" when some games scored and no winner; clickable when scoring is allowed.
- `GroupTable`: rank, team, W-L, game diff, points; rows tinted by `GroupTeamDTO.status` (Advanced green edge, Eliminated dimmed); advancing cut line after `advancingPerGroup`.
- `BracketView`: columns per round (round labels Final / Semifinals / Quarterfinals / Round of N), match cards with connector lines, champion slot at the end. Pure CSS grid, no bracket library.
- `PlacementTable` + `Podium`: final standings, shared ranks shown as ranges ("3rd-4th").
- `ConfirmDialog`: title, message, confirm label, danger style, optional reason textarea (max 250, counter).
- `CopyField`: read-only value + copy button (invite code).
- `EmptyState`, `ErrorState` (with traceId for 500), skeleton loaders, `LiveIndicator`.

### Services and state

- API services, one per controller, return typed DTOs only: `AuthApi`, `TournamentsApi`, `TeamsApi`, `MatchesApi` (game result only), `StandingsApi` (`/groups`, `/bracket` only; raw `GET api/standings/{id}` and `GET api/matches?standingId=` return entities and are not used), `TenantApi`.
- `SessionStore` (root, signals): user, tenant, access token + expiry; `isLoggedIn` computed.
- `TournamentListStore` (list page): list signal, grouped by section; applies tenant hub events.
- `TournamentStore` (provided in the tournament shell route, one per open tournament): `tournament`, `state` (flags), `teams` (+ id -> name map), `groups`, `bracket`, `results`, `loading`/`error`. Methods: `load()`, `reloadState()`, `reloadStandings()`, `applyGameUpdated(event)`; actions (start groups/bracket, cancel, delete, add/remove team, score game) call the API then reload what changed.
- Enums arrive as numbers (no string enum converter): one `enums.ts` with `TournamentStatus` 1-8, `Format` 1-3, `TeamStatus` 0-4 and their labels.
- Team names: match/game DTOs carry only team ids; the store keeps a `Map<id, name>` from `GET api/tournaments/{id}/teams`.

## 3. Theme

### Palette

Tokens as CSS variables on `:root`, dark values under `[data-theme=dark]` and `prefers-color-scheme` (theme toggle: system / light / dark, saved in localStorage).

| Token | Light | Dark | Use |
|---|---|---|---|
| `--bg` | `#F6F7FB` | `#0E1117` | page background |
| `--surface` | `#FFFFFF` | `#161B24` | cards, dialogs |
| `--surface-2` | `#EEF0F6` | `#1E2531` | table header, inputs, hover |
| `--border` | `#DDE1EA` | `#2A3242` | dividers, card borders |
| `--text` | `#141824` | `#E7EAF1` | primary text |
| `--text-muted` | `#5B6475` | `#97A0B3` | secondary text |
| `--primary` | `#4F46E5` | `#8B85FF` | brand, primary buttons, focus ring |
| `--primary-contrast` | `#FFFFFF` | `#0E1117` | text on primary |
| `--success` | `#15803D` | `#4ADE80` | winner, saved, advanced |
| `--warning` | `#B45309` | `#FBBF24` | seeding, warnings |
| `--danger` | `#B91C1C` | `#F87171` | cancel/delete, errors, eliminated |

Primary is indigo: neutral enough for a dashboard, distinct from all status hues except Setup-adjacent neutrals.

### Tournament status colors

Chip = tinted background (12% alpha) + solid text/dot color. Text label always shown (color is never the only signal).

| Status | Label | Light | Dark | Extra |
|---|---|---|---|---|
| Setup | Registration | `#475569` | `#A3B0C2` | |
| SeedingGroups | Drawing groups | `#B45309` | `#FBBF24` | spinner |
| GroupsInProgress | Groups live | `#0369A1` | `#38BDF8` | pulsing dot |
| GroupsCompleted | Groups done | `#7C3AED` | `#C4B5FD` | "Start bracket" hint |
| SeedingBracket | Seeding bracket | `#B45309` | `#FBBF24` | spinner |
| BracketInProgress | Playoffs live | `#BE185D` | `#F472B6` | pulsing dot |
| Finished | Finished | `#15803D` | `#4ADE80` | trophy icon |
| Cancelled | Cancelled | `#B91C1C` | `#F87171` | struck card, banner with reason |

List sections by status: Live (Seeding*, GroupsInProgress, GroupsCompleted, BracketInProgress), Registration (Setup), Finished, Cancelled.

### Typography

- Inter 400/500/600; Space Grotesk 600/700 for page titles, tournament name, podium and big series scores.
- Scale: 12 / 14 (body) / 16 / 20 / 24 / 32. Line height 1.5 body, 1.2 headings.
- Scores and tables: `font-variant-numeric: tabular-nums`.

### Spacing, shape, elevation

- 4 px base: 4, 8, 12, 16, 24, 32, 48.
- Radius: 6 (inputs, chips), 10 (cards), 14 (dialogs). Pills for chips.
- Cards: 1 px border, no shadow in dark; light uses a soft shadow `0 1px 2px rgb(16 24 40 / .06)`. Dialogs get a larger shadow.
- Focus: 2 px `--primary` outline + 2 px offset on every interactive element.
- Touch targets >= 40 px; phone gutters 16 px.

### Component notes

- Buttons: primary (filled indigo) only for the one next action per screen; secondary outlined; danger only inside menus/dialogs.
- Destructive actions (Delete, Cancel tournament) live in the tournament header kebab menu, never next to the primary action.
- Match card: winner row bold + success left bar; loser text muted; scores right-aligned in a small box.
- Group table: top `advancingPerGroup` rows get a green left edge; dashed line under the last advancing row.
- Bracket: 220 px columns, 24 px gutters, 1 px connector lines in `--border`, highlighted path for the champion after Finished.
- Motion: 150 ms ease for hover/expand; live-updated card flashes `--primary` tint for 1 s; respects `prefers-reduced-motion`.

## 4. Security

All verified against the backend code (Program.cs, AuthService, JwtTokenService, GlobalExceptionHandler, TournamentHub). Client checks are UX; the backend enforces.

### Tokens

- Login/register/refresh return `AuthResponse { userId, userName, email, tenantId, tenantName, accessToken, refreshToken, expiresAt }`. Access token 60 min, refresh token 7 days (config).
- Access token: memory only (SessionStore signal). Refresh token: localStorage (needed to survive reload; trade-off: readable by XSS, mitigated by Angular escaping + no `innerHTML`/`bypassSecurityTrust*` for user text).
- App start: refresh token present -> `POST api/auth/refresh` -> session restored (user + tenant from the response); fails -> clear and go to `/login`.
- Refresh rotates: the backend revokes the used token on every refresh. So:
  - one in-flight refresh per tab (shared promise);
  - across tabs: refresh inside `navigator.locks.request('to2-refresh')`, re-read localStorage inside the lock; if another tab already rotated, use the stored newer token;
  - `storage` event: refresh token removed in another tab -> this tab signs out.
- Auth interceptor: attaches `Authorization: Bearer` to API calls only (never to other origins). Before a request, if `expiresAt` is within 30 s, refresh first. On 401 from a non-auth endpoint: refresh once, retry once; second 401 -> sign out, `/login?returnUrl=`.
- 401 from `login` = wrong credentials (show `detail`), no refresh attempt.
- Sign out: `POST api/auth/revoke { refreshToken }` (needs a valid access token: refresh first if expired), then clear session, stop hub, go to `/login`. Revoke failure still clears locally.

### Organizations and invite codes

- Register form: username, email, password, organization name, invite code (shown only when needed).
- Org name check `GET api/auth/organizations/exists?name=` on blur (not on every keystroke: it shares the 10/min auth rate limit with login/register/refresh). Result: "New organization will be created" or "Organization exists: enter an invite code from a member".
- Submit outcomes mapped from backend: 409 "already exists" -> reveal invite field, focus it; 400 "Invalid or expired invite code." -> field error; 400 "Organization not found" with a code -> suggest clearing the code; 409 "just taken" (race) -> reveal invite field.
- Password hint mirrors Identity config: min 6, digit, upper, lower. Server message ("User creation failed: ...") shown under the form.
- Invite code (`POST api/tenant/invite-code`, any member): shown once in a dialog with copy button and expiry ("valid for 24 h, until 21 Oct 14:05"). Warning before generating: "Generating a new code makes the previous one stop working." The code cannot be shown again (backend stores only a hash). "Copy invite message" copies org name + code + register URL (code never placed in a URL).
- Code input accepts any formatting (backend strips dashes/spaces, uppercases); UI auto-formats `XXXX-XXXX-...` while typing.

### Error handling (error interceptor + per-feature handling)

Backend errors are RFC 7807 ProblemDetails (`title`, `detail`, `traceId`).

| Status | Source | UI |
|---|---|---|
| 400 validation | FluentValidation / DataAnnotations: `errors { Field: [msg] }` | map to form controls (case-insensitive keys, e.g. `NumberOfGroups`); unmapped -> form-level message |
| 400 domain | `ValidationException`/`DomainException`: `detail` | form-level or toast with `detail` |
| 401 | expired/invalid token, missing tenant claim | refresh + retry once, then sign out |
| 403 | inactive organization (login, invite) | full-page message "Organization inactive", sign out |
| 404 | missing or other tenant's resource (tenant filter) | not-found state on the page ("Tournament not found or not in your organization") |
| 409 | stale `version` or action not allowed in current state | toast with `detail`, then reload the affected data (state + standings / tournament) |
| 429 | auth endpoints, 10/min per IP | inline "Too many attempts. Try again in a minute."; disable submit 60 s; a 429 on refresh does not sign out |
| 500 | anything else | toast "Something went wrong" + copyable `traceId` |
| network/0 | offline | top banner, retry button |

### Guards and action gating

- `authGuard` (canMatch) on app routes: session or successful silent refresh, else `/login?returnUrl=` (only relative app paths accepted as returnUrl).
- `guestGuard` on `/login`, `/register`: logged in -> `/tournaments`.
- No role guards (backend has no roles: every org member can do everything in the org).
- Tournament actions come from `TournamentStateDTO`: `canModifyTeams` (add/remove team, edit teams tab), `canScoreMatches` (match cards clickable), `canCancel`, `canDelete` (kebab menu items), `isTransitionState` (show "Seeding..." progress, disable actions). State is reloaded after every action, on 409, and on hub events that change status.
- Start buttons: shown from proposed `canStartGroups` / `canStartBracket` flags (section 6). The backend still returns 409 if the state changed meanwhile.

### Versioned writes

- Tournament edit sends the loaded `version`; game result sends the game's `version` from the loaded group/bracket data.
- 409 on a versioned write: toast with server `detail` ("Game was modified by another user. Reload and try again."), automatic reload of that standing, dialog stays open with fresh data so the user can re-check and save again.

### SignalR

- Hub `/hubs/tournament`, `accessTokenFactory` returns the current access token (refreshing first if near expiry). Token goes in `access_token` query string: required for WebSockets; backend reads it only for `/hubs` paths.
- Connect after login/session restore, stop on sign out. `withAutomaticReconnect`; on reconnect: re-`JoinTournament` for the open tournament and reload its data (events missed while offline).
- Tournament page: `JoinTournament(id)` on enter, `LeaveTournament(id)` on leave. A `HubException` (unknown or other tenant's tournament) -> not-found state.
- Tenant events (`TournamentCreated/Updated/Deleted/Cancelled`, `TeamAdded/Removed`) are filtered by `tournamentId` on the tournament page.

### Other

- No user content rendered as HTML; descriptions and cancel reasons as text with `white-space: pre-line`.
- No secrets or personal data in URLs (except the hub token as required by SignalR).
- Hosting headers (frontend host config): CSP `default-src 'self'; connect-src 'self' <api> wss://<api>; frame-ancestors 'none'`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: same-origin`.

## 5. Key UX flows

### Register / join organization

1. `/register`: username, email, password, organization.
2. Org blur -> exists check -> helper text + invite code field when it exists.
3. Submit -> session -> `/tournaments` with a welcome empty state ("Create your first tournament" / "Invite your team").
4. Invite: user menu or `/organization` -> "Generate invite code" -> confirm (replaces old) -> code + copy.

### Create tournament (`/tournaments/new`)

- One page, two columns (form left, live preview right; preview below on phone).
- Fields: name (4-60), description (max 250, counter), format (3 selectable cards with mini diagrams), max teams (stepper 4-32), groups count + advancing per group (only for formats with groups).
- Live preview: group sizes (teams split evenly, extra teams to the first groups), matches per group (round robin, Bo3), bracket size (next power of two of groups x advancing, or of max teams), BYEs, rounds.
- Inline validation mirrors the create validator (min group size 3, advancing < min group size, single group needs >= 2 advancing); server 400 field errors mapped back.
- Submit -> `/tournaments/:id/teams` (next step is adding teams).

### Add teams (Teams tab, Setup)

- Capacity bar "6 / 8 teams".
- One autocomplete input: suggestions from the org team library (`GET api/teams`, excluding teams already registered); typing a new name offers "Create team 'X'" (max 24 chars) -> `POST api/teams` then `POST api/teams/{tournamentId}/teams/{teamId}`.
- Enter to add, input stays focused for fast entry. Team rows with a remove button (`DELETE api/teams/{teamId}/{tournamentId}`).
- 409s inline under the input: duplicate name, already registered, full, registration closed.
- Hidden when `canModifyTeams` is false: list becomes read-only with team status.

### Start groups / bracket

- Overview shows a "Next step" card: Setup with >= 2 teams -> "Start groups" (or "Start bracket" for BracketOnly); fewer than 2 teams -> disabled with hint "Add at least 2 teams".
- Confirm dialog summarises: "8 teams into 2 groups, 12 matches (Bo3). Registration closes. Teams can not be changed afterwards."
- GroupsCompleted -> "Start bracket" card: "Top 2 of each group advance; winners meet runners-up; same-group teams are kept apart in round 1."
- During request: button spinner; `isTransitionState` shows a "Drawing groups..." state. Success -> navigate to Groups/Bracket tab; other open clients get `GroupsStarted`/`BracketStarted` and reload.

### Score games (versioned)

- Click a match card (only when `canScoreMatches`, both teams known, no match winner) -> match dialog.
- Dialog: header Team A `1 - 0` Team B, Bo3. One row per game: two large team buttons (tap = winner), optional score inputs (e.g. 16 - 12; when filled, the higher score preselects the winner), "Save game".
- Save -> `PUT api/matches/{matchId}/games/{gameId}/result { gameId, winnerId, teamAScore, teamBScore, matchId, standingId, tournamentId, version }`.
- A scored game can be corrected until the match has a winner; after that the dialog is read-only.
- Games not needed after the match is decided show "Not played".
- Response flags drive feedback: `matchFinished` -> "Alpha wins 2-1"; `standingFinished` -> "Group A finished"; `allGroupsFinished` -> "All groups done: start the bracket"; `tournamentFinished` -> confetti-free success banner + link to Results.
- After save: reload state if `newTournamentStatus` set; reload the standing (or apply the hub event, whichever arrives first).

### Live updates

- `GameUpdated` (tournament group): carries the whole updated group or bracket -> replace that standing in the store; changed match card flashes; toast "alice scored Alpha vs Bravo" when `updatedBy` is someone else; `newTournamentStatus` -> reload state.
- `GroupsStarted` / `BracketStarted` -> reload state + standings.
- `TeamAdded` / `TeamRemoved` (filtered by id) -> reload teams.
- `TournamentUpdated` (filtered) -> reload tournament + state.
- `TournamentCancelled` (filtered) -> reload; red banner "Cancelled by bob: reason".
- `TournamentDeleted` (filtered) -> dialog "This tournament was deleted by bob" -> back to list.
- List page applies all tenant events by reloading the list (debounced 500 ms).
- Live dot in top bar: green connected, amber reconnecting, gray offline.

### Cancel / delete

- Kebab menu in the tournament header: "Delete tournament" when `canDelete` (Setup), "Cancel tournament" when `canCancel` (started, not finished).
- Delete confirm: "Deletes the tournament and its teams that are in no other tournament. This cannot be undone." -> `DELETE` -> list + toast.
- Cancel confirm with optional reason (max 250) -> `POST .../cancel { reason }` -> page stays, read-only, red banner with who/when/reason.

### Final standings

- Results tab (Finished): podium (1st, 2nd, shared 3rd), then full table: placement (ranges for shared ranks: "5th-8th"), team, how far ("Champion", "Final", "Semifinals", "Group stage").
- Overview of a finished tournament shows champion card + link to Results.
- Data from `GET api/tournaments/{id}/final-standings` (empty list unless Finished) or the `finalStandings` in the last game result / hub event.

## 6. Proposed backend additions

No schema changes; all data already exists.

- `TournamentStateDTO` + `CanStartGroups`, `CanStartBracket`: state machine `IsTransitionValid(status, Seeding*, format)` -> start buttons without client status rules.
- `GetTournamentResponseDTO` + `CurrentTeams`, `NumberOfGroups`, `AdvancingPerGroup`: overview facts, start-confirm summary, advancing cut line in group tables.
- `GetTournamentListResponseDTO` + `CreatedDate`: sort the list newest first.
- `POST api/tournaments/{id}/teams` body `{ name }` -> `{ teamId, tournamentId }`: create team + register in one transaction (no orphan team when registration fails). Optional: two existing calls work.
- `PUT api/teams/{id}` body `{ name, version }` -> `{ id, name, version }`: rename from the team library (service method exists; team DTOs need `Version`). Optional.

## 7. Unresolved questions

1. Rebuild on current Angular + PrimeNG major (not 17)? Default yes.
2. Refresh token in localStorage OK? Alternative httpOnly cookie = backend change in AuthController + CSRF care, ~1 day.
3. Which section 6 additions to accept? Default: state flags + tournament DTO fields.
4. Default theme: system (default), dark or light?
5. GroupsOnly with several groups: every group winner gets placement 1 / Champion. Show per-group results? Needs group id in `TeamPlacementDTO` (small DTO change).
6. Per-game score inputs wanted, or winner-only? Default optional scores.
7. BestOf choice at create (Bo1/Bo3/Bo5)? Needs a tournament column = schema change; default stays Bo3.
8. Teams library page in scope? Default yes, list + create only.
9. Public spectator link (no login)? Needs anonymous endpoints + tenant filter bypass, large; default no.
