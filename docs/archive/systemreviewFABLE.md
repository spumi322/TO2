# TO2 review, 2026-11

Branch `newui` @ `1b7fe94`. Static read, nothing built or run.

Filter applied: solo, junior+, non-commercial reference project, a few admins per organization. An item stays only if (a) someone using or reading the project would notice it, or (b) it is cheap and makes the code easier to read. Hardening for scale, abuse or strangers is out. Excluded as before: CI/CD, Docker, deploy, already-decided trade-offs.

Coverage: backend non-test code fully read (no migrations). UI: all services, stores, helpers, component classes; presentational templates and CSS not read. Tests: names only.

## 1. Do (7 items)

### D1 Delete dead code
No production caller, no test reference. Biggest readability gain for the least risk.
- Services: `IAuthService.ValidateRefreshTokenAsync`.
- Repositories: `GetActiveAsync`, `GetByStatusAsync`, `GetWithStandingsAsync`, `GetByStandingIdWithGamesAsync`, `IRepository.GetAllAsync`, `CountAsync()`, `ExistsAsync(long)`.
- DTOs: `GenerateMatchDTO`, `SeedGroupsResponseDTO`, `SeedBracketResponseDTO`, `MatchResultDTO`, `GenerateGamesDTO` (returned, never read).
- Domain: `Tournament.TournamentParticipants`, `BracketSeedingUtility.IsPowerOfTwo`.
- Flags that are always the same: `StartGroupsResponseDTO.Success` / `StartBracketResponseDTO.Success`, context `Success`, `ShouldContinue`, `Standings`, the `entriesUpdated` counter, `MatchResultContext.Message` / response `Message` (UI ignores it; the finish text carries a raw team id and names the wrong champion in GroupsOnly).
- Unused constructor parameters in pipeline steps and strategies (`UpdateGroupStatsStep`, `TransitionTournamentStateStep`, `BracketProgressStrategy`, `TransitionToGroupsInProgressStep`, the Mark/Validate seeded steps), `AuthController` logger.
- UI: `TournamentStore.loading` is set and never read: use it for a first-load indicator or remove it.
- Accept: build without new warnings, all tests green, CLAUDE.md no longer names removed things.

### D2 Wrong annotations on `Tournament`
- `Format` has `Range(0,2)` (enum is 1–3), `MaxTeams` `Range(2,32)` (rule is 4–32), `Description` max 250 (validator 120), `DefaultValue` attributes. Nothing enforces them; a reader takes them for rules.
- Intent: delete them; limits live in the validators and EF config.

### D3 Hygiene a reader sees first
- Tracked by mistake: `webAPI.EntityFrameworkCore.targets` (repo root). `<None Remove="nul" />` in `TO2.csproj`.
- README: TODO screenshot placeholder.
- CLAUDE.md says the match result body is `{ teamAWins, teamBWins, version }`; the DTO also needs `matchId`, `standingId`, `tournamentId`. Fix the doc.
- Commented-out `UpdateAsync` in `Repository`, stale route comments in `TournamentsController`, broken characters in a `FormatMetadata` comment, `CreateTournamentValidator` in the global namespace.

### D4 Bracket preview differs from the real bracket (8+ slots)
- Where: UI `tournament-config.generateMatchups` vs backend `BracketSeedingUtility.GenerateSeedingOrder`.
- What: the create page shows seeds 1/8 next to 4/5 (standard). The backend builds 1/8 next to 3/6. Visible to anyone who creates an 8-team bracket and compares.
- Intent: backend uses the standard order the UI already shows.
- Accept: unit test pins the 8- and 16-slot round 1 order; property tests green. No existing test pins the current order.

### D5 "Tournament not found" on a hub hiccup
- Where: `TournamentStore.connect`: any `JoinTournament` rejection sets `notFound`, also a dropped connection.
- Intent: the REST load decides not-found; a hub failure never hides a loaded tournament.
- Accept: store spec for load ok + join rejected.

### D6 Concurrent match results can leave a tournament stuck
- Where: `MatchResultPipeline`, `GroupProgressStrategy`, `StandingService.IsGroupFinished` / `CheckAllGroupsAreFinished`.
- What: two results confirmed at the same instant for the last two matches of a group (no shared team), or the last matches of two groups. Each transaction sees the other as unfinished, neither writes a shared row, no version conflict. Group or status never advances; only exit is cancel.
- Honest sizing: needs two admins within milliseconds; will almost never happen here. It stays in "Do" because optimistic concurrency is this project's headline feature, the tool already exists (`BumpVersion`, as in add/remove team), and "found a write skew, closed it with the aggregate lock" is a strong interview story.
- Accept: test with two contexts: second save is a conflict, retry finishes the group and moves the status. Property tests green.

### D7 `NextPowerOfTwo` twice
- `StandingService` has a private copy of `BracketSeedingUtility.NextPowerOfTwo`. Use the one.

## 2. Know it, do not build it

Good answers to "what would you do next?". Building them is out of scale.

- No account lockout: only the 10/min per IP limit guards login. Identity has lockout built in.
- Refresh tokens are never deleted; fine for years at this size.
- `ShareMatchDraft` trusts match id and numbers from members of the own organization.
- Refresh does not check `Tenant.IsActive` (login does). The flag is only ever set by hand in the DB.
- Nothing is tested over HTTP: auth wiring, validation response shape, rate limit. The property test covers the business flow below that.
- UI repeats the creation rules (`validateConfig`, limits, group distribution) for the live preview; nothing forces both sides to agree.
- UI branches on backend message text in the register page.
- `GetTeamsForBracketByFormat` is a query that also writes statuses.
- Pipelines log expected 409s as errors before the handler logs its warning.

## 3. Dropped from the first pass

Not worth doing or discussing at this scale: 404 vs 200/204 on read endpoints, non-numeric id page, JWT settings startup check, cancellation tokens, `AddIdentityCore`, `FluentValidation.AspNetCore` (revisit with .NET 10), linter/analyzers, popup focus handling, `TENANT_EVENT_NAMES` slice, bracket re-render overlap (unverified), UI service specs, extra unit tests, start-up refresh wait, store connect after destroy.

## 4. Feature ideas (3)

- F1 Best-of per tournament or stage. Enum, `FormatService` and game generation already handle Bo1/Bo5; only creation and a few UI texts assume Bo3. Small, shows the abstractions pay off.
- F2 Tie-break shown in the group table (head-to-head / game difference / registration order). UI only.
- F3 Correct a confirmed result while nothing depends on it yet. Larger; a real organizer need and a good pipeline exercise.

## Order

D1 → D2 → D3 (one cleanup pass) → D4 → D5 → D6 → D7.

## 5. Implementation plan

Agreed after going through the review together. This section replaces the order above and reworks D1: the cleanup is driven by the rules the app has today, not by "this method has no caller".

### Decisions

- **Schema is not frozen.** The review's "keep columns, not worth migrations" is dropped. Each piece of step 1 gets its own migration, so the schema matches the product after every piece. Take a DB backup first (`G:\Code\TO2-db-backups`). The migrations also run on the deployed database at the next deploy; they only remove things nothing reads.
- **`TournamentStore.loading`** is removed, not turned into a loading indicator.
- **Bo1/Bo5 support stays**: unused today but it is feature idea F1.

### Corrections to the review

- D1 says "no test reference". Not true for two things; these tests change with the code:
  - Context `Success`: asserted in `ByePipelineStepsTests` and `DistributeTeamsIntoGroupsStepTests`.
  - `GenerateGamesDTO`: mocked in `ByePipelineStepsTests`.
- `Tournament.TournamentParticipants` is an EF navigation (named in the model snapshot), not a plain dead property. Removing it changes the snapshot, so it goes with a migration.

### Step 1: the model matches the product

A migration per piece.

**1a. Remove what has no rule behind it**
- D2: the wrong and unenforced annotations on `Tournament` (`Range(0,2)` on `Format`, `Range(2,32)` on `MaxTeams`, `MaxLength(250)` on `Description`, the `DefaultValue` attributes). Limits live in the validators and EF config; check that removing `MaxLength`/`Required` attributes does not change column types, or keep those that EF reads.
- `Tournament.TournamentParticipants`: nothing reads it. It is an EF navigation, so the model snapshot changes; it goes with a migration.

### Step 2: bugs

- **D4** Backend seeding order becomes the standard one the create preview shows (1/8 next to 4/5). Unit test pins the 8- and 16-slot round 1 order.
- **D5** A hub join failure never sets "not found"; only the REST load decides. Store spec: load ok + join rejected.
- **D6** Aggregate lock (`BumpVersion`) in the match result pipeline so two results at the same instant cannot leave a group or status stuck. Two-context test: second save is a conflict, retry finishes the group.

### Step 3: plain leftovers

Last, because the list shrinks after step 1.

- Always-the-same flags: `Success` on both start response DTOs and contexts, `ShouldContinue`, context `Standings` (written twice, never read), `entriesUpdated`, `MatchResultContext.Message` and the response `Message` (UI never reads it; the finish text carries a raw team id and names the wrong champion in GroupsOnly). UI model fields `success` / `message` go too.
- `IAuthService.ValidateRefreshTokenAsync`.
- Repositories: `GetActiveAsync`, `GetByStatusAsync`, `GetWithStandingsAsync`, `GetByStandingIdWithGamesAsync`, `IRepository.GetAllAsync`, `CountAsync()`, `ExistsAsync(long)`.
- DTOs: `GenerateMatchDTO`, `SeedGroupsResponseDTO`, `SeedBracketResponseDTO`, `MatchResultDTO`; `GenerateGames` returns `Task` instead of `GenerateGamesDTO`.
- `BracketSeedingUtility.IsPowerOfTwo`.
- Unused constructor dependencies, verified: `UpdateGroupStatsStep` (`_unitOfWork`), `TransitionTournamentStateStep` (`_tournamentRepository`, `_unitOfWork`), `BracketProgressStrategy` (`_unitOfWork`), `TransitionToGroupsInProgressStep` (`_tournamentRepository`), `AuthController` (`_logger`). Still to check: the Mark/Validate seeded steps.
- `TournamentStore.loading`.
- D3 hygiene as listed above; D7 `NextPowerOfTwo` duplicate.

### Every step

- Build without new warnings, all tests green (property tests included).
- `CLAUDE.md` and `public/demo-tournament.json` follow DTO and rule changes.
- Stop after each sub-step for a diff review; commit only when told.
