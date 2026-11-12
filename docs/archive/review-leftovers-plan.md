# Review leftovers plan

Open items from the 2026-11 code review. Part 1 says what is wrong and why it matters. Part 2 says how every phase is carried out. The solution is the implementer's job: read the code, pick the simplest fix that an interviewer can be walked through, reuse patterns the project already has.

Scale: solo junior reference project, a few admins per organization, no data worth keeping. A fix that needs new infrastructure, a new abstraction or more than a few files is too big: stop and ask.

## Part 1: what is open and why

### Phase 1: `Tournament` says only what is enforced

**1.1 Annotations that are not rules.**
`Tournament` carries data annotations that nothing enforces and that contradict the real rules: `Format` has `Range(0,2)` while the enum is 1 to 3, `MaxTeams` has `Range(2,32)` while the rule is 4 to 32, `Description` has `MaxLength(250)` while the validator allows 120, plus `DefaultValue` attributes that do nothing.
Why: a reader takes them for rules. The limits live in the validators and the EF configuration; the entity should not state a second, different set.
Watch: EF reads `MaxLength` and `Required` for column types. An annotation EF relies on stays, or its effect moves to the EF configuration, so the schema does not change by accident.

**1.2 `Tournament.TournamentParticipants`.**
A navigation to group entries that nothing reads.
Why: it suggests the tournament aggregate owns its group entries; in this model they belong to a standing.
Watch: it is an EF navigation named in the model snapshot, so removing it changes the snapshot and needs a migration.

### Phase 2: the bracket preview differs from the real bracket

The create page previews round 1 in the standard order (seed 1/8 next to 4/5). The backend builds a different order (1/8 next to 3/6). From 8 slots up, anyone who creates a bracket and compares it with the preview sees two different brackets.
Why: the preview is a promise of what the tournament will look like; the standard order is also the one that keeps the top seeds apart longest.
Direction: the backend follows the order the preview shows, not the other way round.
Proof: a unit test pins the round 1 order for 8 and 16 slots (no test pins the order today); property tests stay green.

### Phase 3: a hub hiccup shows "Tournament not found"

When joining the tournament's SignalR group fails for any reason, including a dropped connection, the tournament page shows "not found" although the tournament loaded fine over REST.
Why: a live-update problem must never hide data the user already has. Whether a tournament exists is the REST load's answer only.
Proof: a store spec for "load succeeds, join is rejected": the tournament stays visible.

### Phase 4: two results at the same instant can leave a tournament stuck

Two admins confirm the last two matches of a group at the same moment (matches without a shared team), or the last matches of two groups. Each transaction checks "is the group / are all groups finished?" and sees the other match as still open. Neither writes a row the other one wrote, so no version conflict fires. Both commit, and the group or the tournament status never advances. The only way out is cancelling the tournament.
Why: it needs two admins within milliseconds, so it will almost never happen. It is fixed anyway because optimistic concurrency is this project's headline feature and the tool already exists: the aggregate lock (`BumpVersion`) that add/remove team use for the same kind of problem.
Proof: a test with two contexts: the second save is a conflict, a retry finishes the group and moves the status. Property tests stay green.

### Phase 5: leftovers that say something untrue

Each item is code that describes behavior the app does not have. One sub-step per group.

**5.1 Flags and messages that never vary.**
- `Success` on both start response DTOs and on the pipeline contexts: always true, failures are exceptions.
- `ShouldContinue`, the context `Standings` (written twice, never read), the `entriesUpdated` counter.
- `MatchResultContext.Message` and the response `Message`: the UI never reads it, and the finish text carries a raw team id and names a wrong champion in GroupsOnly.
- The UI model fields `success` / `message` that mirror them.
Why: the pipeline convention is "failure = throw". A success flag next to it tells the reader there is a second failure path to handle.
Watch: two tests assert the context `Success` (`ByePipelineStepsTests`, `DistributeTeamsIntoGroupsStepTests`); they change with the code.

**5.2 Methods and DTOs without a caller.**
- `IAuthService.ValidateRefreshTokenAsync`.
- Repository methods: `GetActiveAsync`, `GetByStatusAsync`, `GetWithStandingsAsync`, `GetByStandingIdWithGamesAsync`, `IRepository.GetAllAsync`, `CountAsync()`, `ExistsAsync(long)`.
- DTOs: `GenerateMatchDTO`, `SeedGroupsResponseDTO`, `SeedBracketResponseDTO`, `MatchResultDTO`. `GenerateGamesDTO` is returned and never read (mocked in `ByePipelineStepsTests`).
- `BracketSeedingUtility.IsPowerOfTwo`.
Why: every unused method is a question for the reader ("who calls this?") and a place where old designs survive.
Watch: confirm "no caller" for each one before deleting; tests count as callers that must change.

**5.3 Constructor dependencies nobody uses.**
Known: `UpdateGroupStatsStep` (unit of work), `TransitionTournamentStateStep` (tournament repository, unit of work), `BracketProgressStrategy` (unit of work), `TransitionToGroupsInProgressStep` (tournament repository), `AuthController` (logger). Not yet checked: the four Mark/Validate seeded steps.
Why: an injected unit of work in a step suggests the step commits, which breaks the rule that only the pipeline commits.

**5.4 `TournamentStore.loading`.**
Set in several places, read nowhere. Decided: it is removed, not turned into a loading indicator.

**5.5 Two `NextPowerOfTwo`.**
`StandingService` has a private copy of `BracketSeedingUtility.NextPowerOfTwo`.
Why: one rule, one place.

**5.6 Hygiene a reader sees first.**
- `<None Remove="nul" />` in `TO2.csproj`.
- `CLAUDE.md` gives the match result body as `{ teamAWins, teamBWins, version }`; the DTO also takes `matchId`, `standingId`, `tournamentId`.
- A commented-out `UpdateAsync` in `Repository`.
- Route comments in `TournamentsController` that do not match the routes (`api/Tournament/5`).
- Broken characters in a `FormatMetadata` comment (check; fix if present).
- `CreateTournamentValidator` sits in the global namespace.

## Part 2: how to carry it out

### Order

Phase 1, 2, 3, 4, 5, in that order; 5.1 to 5.6 one after another. The run is unattended: all steps back to back, no waiting for a confirmation in between. A step starts only after the one before is committed or skipped.

### For every phase or sub-step

1. **Read first.** Confirm the problem still exists as described and find every reader and writer of what changes (code, tests, UI, `public/demo-tournament.json`). If the problem is already gone, note it in the progress list and go on.
2. **Change.** The smallest change that removes the problem. No renames, reformatting or "while I am here" fixes outside the item.
3. **Schema changes.** Back up the local database first (`G:\Code\TO2-db-backups`), stop a running API, one migration per change, apply it. Plain schema changes are enough; no data-preserving effort.
4. **Tests.** A bug fix (phases 2, 3, 4) gets the test named under "Proof", written so that it fails without the fix. Deletions get no new tests; existing tests are adapted.
5. **Build and test.** From `src/webAPI`: `dotnet build` with no new warnings, `dotnet test` all green, property tests included. When UI files changed, from `src/UI`: `npm test` and `npm run build`.
6. **Browser.** When the change is visible in the UI or touches a DTO the UI reads: run API and UI, check the affected page in the real app (phase 2: create preview against the started bracket; phase 3: tournament page with a failing hub join; 5.1 and 5.4: start groups, start bracket, confirm a match). Close the tab afterwards.
7. **Docs.** `CLAUDE.md`, README, code comments, `public/demo-tournament.json` and the project memory (`C:\Users\spumi\.claude\projects\G--Code-TO2\memory\`, index `MEMORY.md`) describe the state after the change. The project memory counts as project documentation: a memory that names something the step changed or removed is corrected or deleted. Current state only: no "removed", "previously" or history notes. Implementation documents can be used for checklisting or for version control.
8. **Commit.** Only when steps 5 and 6 are clean. One commit per phase or sub-step, holding only that step's files.
9. **Progress.** Tick the step in the progress list at the end of this file and fill in its report lines (see "Progress").

### Guardrails

- Line endings are LF everywhere; files written by EF or scripts are converted before staging.
- Commit messages: short, lowercase, plain words.
- No branch operations, no push, no amend or history rewrite. Git is used for diff and commit only.
- Versioned writes never go through `ExecuteUpdate` or raw SQL in application code.
- The state transition table is not changed.
- No keyboard or accessibility work, no animations, no UI polish that the item does not ask for.

### When something does not go to plan

The sequence above is the main guard: every step is small, proven by build and tests, and its own commit, so a wrong call can be reverted alone. An obstacle is therefore not a reason to stop the run.

- **Ordinary obstacles** (build error, failing test, locked file, API still running, flaky browser check): find the cause and fix it. That is part of the step.
- **A judgment call** (an unlisted caller or test, two possible fixes, a description that is slightly off): take the simpler option that fits the "why" of the item, write the decision into the progress list, go on.
- **A step that cannot be finished within its limits** (it would need a new abstraction or package, change what a user sees beyond the item, or the tests cannot be made green without weakening them): undo that step's changes, mark it skipped with the reason, continue with the next step. The steps do not depend on each other, so a skipped step never stops the run.

Never done to get past an obstacle: deleting or weakening a test, committing with a red build or red tests, touching a guardrail, widening the step.

**Stop the whole run only when:**
- the working tree holds changes the run did not make (files under `docs/` aside);
- the database backup or a migration fails and the cause is not obvious;
- the build or tests are red before the step's own change (the base is broken);
- the same file write fails several times in a row.

### Out of scope

Hardening for scale or abuse, CI/CD, deployment, new features, and anything not listed in part 1.

When all phases are done, this file moves to `docs/archive/`.

## Decided defaults

- Phase 4: the second admin gets a 409 and confirms again by hand; no server retry.
- Phase 5: one commit per sub-step.

## Progress

- [x] 1.1 annotations
  - **Result**: done (bd884a3).
  - **Verified**: build 20 warnings (same as before the run), 405 tests green; `dotnet ef migrations has-pending-model-changes` reports no change, so no migration.
  - **Judgment calls**: all annotations left the entity, also the harmless ones (`Required`, `EnumDataType`), so the entity states no limits at all. The column sizes EF took from `MaxLength` moved to `TO2DbContext` next to the existing `Name` size. `Description` stays a 250 column although the validator allows 120: shrinking it is a schema change the item does not ask for, and the local database holds a 249 character description that would make the migration fail. A comment in the configuration says these are column sizes and the input limits are in the validators.
  - **Obstacles and errors**: None.
- [x] 1.2 `TournamentParticipants`
  - **Result**: done (8b2ddf5).
  - **Verified**: build 20 warnings, 405 tests green (the Postgres test applies all migrations to an empty database); migration applied to the local database.
  - **Judgment calls**: None. The migration `RemoveTournamentParticipantsNavigation` has an empty `Up`/`Down`: the foreign key `GroupEntries.TournamentId` stays through `GroupEntry.Tournament`, only the snapshot changes.
  - **Obstacles and errors**: None. Backup: `to2-20261110-before-remove-tournament-participants.dump`.
- [x] 2 bracket order
  - **Result**: done (3338a3f).
  - **Verified**: new test `GenerateSeedingOrder_IsTheStandardBracketOrder` (2, 4, 8, 16 slots) failed for 8 and 16 before the fix; 409 tests green, property tests included. Browser: create preview for 16 teams / 4 groups / 2 advancing shows #1/#8, #4/#5, #2/#7, #3/#6; a simulated tournament of that shape (`simulate_tournament.py`, tournament 381) shows the same round 1 on its Bracket tab (group winners S04, S11, S01, S03 = seeds 1 to 4: S04/S05, S03/S07, S11/S02, S01/S12).
  - **Judgment calls**: `GenerateSeedingOrder` now uses the same algorithm as the UI's `generateMatchups` (start from the final, split every slot), so the two can be compared line by line. `GroupResult_3Groups_SeparatesSameGroupMatch` pinned the positions of the old order; its expected list was reordered (same four pairs, new positions), nothing else in it changed. `public/demo-tournament.json` was left alone: its bracket is stored match data, not derived from seeds.
  - **Obstacles and errors**: the strategy test above failed after the fix (cause: pinned old positions; solved as described). The browser was signed in as an older test user; it was signed out and the simulation's test user signed in to open tournament 381.
- [x] 3 hub join failure
  - **Result**: done (82b78ca).
  - **Verified**: new store spec "keeps the loaded tournament when joining the hub group fails" failed before the fix; UI 106 tests green, `npm run build` clean. Browser: with the `JoinTournament` hub call made to fail (a script on the page closed the socket on that message), tournament 381 opened from the list and stayed fully visible; the connection dot turned orange.
  - **Judgment calls**: a failed join is written to the console as a warning (the same way the store reports a failed live update) and shown nowhere else; the header's connection dot already tells the user the live connection is down. A tournament of another organization is still "not found" through the REST load's 404.
  - **Obstacles and errors**: the first browser script clicked the list row before the list had rendered and threw; the row was clicked by hand afterwards. While the test script kept closing the socket, the page reconnected and rejoined in a fast loop (231 joins in about 3 seconds): that is the script's doing (a real refusal does not close the connection), and the rejoin-after-reconnect code is outside this item.
- [x] 4 concurrent results
  - **Result**: done (eb63a73).
  - **Verified**: new `ConcurrentMatchResultTests` (last two matches of one group; last matches of two groups): request A is paused before its save, request B runs completely, A's save is a `ConflictException`, the retry finishes all groups and the tournament. Both failed before the fix (no exception, tournament stuck). 411 tests green, property tests included. No browser check: nothing the UI shows or reads changed.
  - **Judgment calls**: the lock is the tournament, bumped by every group match result in `GroupProgressStrategy` (one lock covers both cases; a lock per group would not cover two groups). The cost: any two group results of one tournament that overlap get one 409, also when they are not the last matches. Bracket results do not take the lock: two matches that feed the same next match already write the same row, and only the final finishes the tournament. The test pauses request A through a small `IUnitOfWork` wrapper that saves without an explicit transaction, because the SQLite test database has a single connection and cannot hold two transactions; the save and its version check are the real ones. `TournamentRunner.BuildServices` became public so the test reuses the property test's registrations. `CLAUDE.md` concurrency section describes the lock.
  - **Obstacles and errors**: the test file could not be written through a shell heredoc (quoting error), it was written with the file tool. The first test build failed because the API started for the browser checks still locked the DLLs; the API was stopped.
- [x] 5.1 flags and messages
  - **Result**: done (697fd55).
  - **Verified**: build 20 warnings, 411 tests green; UI 106 tests green, `npm run build` clean. Browser (tournament 382, 4 teams in 2 groups, 1 advancing): Start groups, both group matches confirmed in the score popup (group shown finished, status "Ready for playoffs"), Start bracket, final confirmed (status Finished, Results tab appears).
  - **Judgment calls**: removed: `Success` (both start response DTOs, both contexts, UI `StartStageResponse.success`), `ShouldContinue` and `Standings` on both start contexts, `entriesUpdated`, `MatchResultContext.Message`, `MatchResultResponseDTO.Message` and UI `MatchResultResponse.message`. Kept: `Message` of the two start responses (and its UI field): the item lists only the match result message, and removing it would also empty both `BuildResponseStep`s, which is a bigger change than the item. Kept: `StandingProgressResult.Message` (written to the log by `HandleStandingProgressStep`) and its `ShouldContinuePipeline`, which really varies. The two tests that asserted the context `Success` lost that one line; their other assertions are unchanged.
  - **Obstacles and errors**: one screenshot timed out in the browser (renderer busy right after a tab switch); repeated once, fine.
- [x] 5.2 unused methods and DTOs
  - **Result**: done (771b6a1).
  - **Verified**: every name was searched in code and tests before deleting (no caller, no mock setup); build 20 warnings, 411 tests green. No browser check: no DTO the UI reads changed.
  - **Judgment calls**: `IGameService.GenerateGames` now returns a plain `Task` (the returned `GenerateGamesDTO` was never read); the mock in `ByePipelineStepsTests` returns a completed task. `IMatchRepository` / `MatchRepository` are left as an empty interface and class: three services and five test files inject them, so replacing them with `IRepository<Match>` is more than this item. Two `using` lines that only the removed methods needed were removed with them.
  - **Obstacles and errors**: the build failed once because `Application.DTOs.Game` no longer exists (its only file was `GenerateGamesDTO`); the two `using` lines were removed. Removing the first line of `IGameService.cs` with sed left an empty first line after the BOM; fixed by hand.
- [x] 5.3 unused dependencies
  - **Result**: done (75d9edd).
  - **Verified**: a script listed every `private readonly` field in Application, Infrastructure and webAPI that is only declared and assigned; build 20 warnings, 411 tests green (the property tests resolve the pipelines and steps through DI). The API started with the new constructors in the 5.4 browser check.
  - **Judgment calls**: besides the five known ones, the scan found and removed: the standing repository in `MarkBracketAsSeededStep` and `MarkStandingsAsSeededStep` (the two Validate steps use theirs), and the unused loggers of `MatchService`, `TeamService` and `TournamentService` (same kind as the listed `AuthController` logger). Test constructor calls lost the matching argument, nothing else.
  - **Obstacles and errors**: None.
- [x] 5.4 `TournamentStore.loading`
  - **Result**: done (496fd15).
  - **Verified**: no reader in templates, components or specs; UI 106 tests green, `npm run build` clean. Browser (tournament 383): page load, Start groups, both group matches confirmed, Start bracket, final confirmed, tournament Finished; the `/demo` page (snapshot load) renders as before.
  - **Judgment calls**: None.
  - **Obstacles and errors**: the first click on "Start groups" came before the page had rendered and did nothing; clicked again.
- [x] 5.5 `NextPowerOfTwo`
  - **Result**: done (bd71f4e).
  - **Verified**: the two bodies were identical; build 20 warnings, 411 tests green (bracket sizes at creation are covered by the tournament service and property tests).
  - **Judgment calls**: `StandingService` calls `BracketSeedingUtility.NextPowerOfTwo`; the helper stays where its tests are instead of moving to a new shared place.
  - **Obstacles and errors**: None.
- [x] 5.6 hygiene
  - **Result**: done (0d86c33).
  - **Verified**: build 20 warnings, 411 tests green. All six points existed as described: the `nul` item group in `TO2.csproj`, the match result body in `CLAUDE.md` (now `{ matchId, standingId, tournamentId, teamAWins, teamBWins, version }`), the commented-out `UpdateAsync`, seven route comments saying `api/Tournament`, two broken characters in the `FormatMetadata` comment (now plain quotes), `CreateTournamentValidator` in the global namespace (now `Application.Validations.Tournament`, like the other tournament validators).
  - **Judgment calls**: the validator got a block namespace like its neighbours, so the file's diff is the whole file (indentation only; `git diff -w` shows 3 added lines). `Program.cs` and the validator's test got the matching `using`. `CLAUDE.md` is gitignored, so its change is not in the commit.
  - **Obstacles and errors**: None.

The final report is this list. Under each step:

- **Result**: done (commit hash) or skipped (reason).
- **Verified**: test counts, what was looked at in the browser.
- **Judgment calls**: every decision this plan did not spell out, and why that option.
- **Obstacles and errors**: what went wrong on the way (build or test failures, tool or migration errors, failed browser checks), the cause, and how it was solved.

"None" is written out for the last two when there were none, so an empty line never means "not reported".
