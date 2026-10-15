# Task: Tournament cancel / delete lifecycle

> **Status: DONE** — implemented in `027c5a3`, follow-up fixes in `c725ef0`. Kept for history; "Current state" below describes the code *before* the task.
> Known bugs below: #1 fixed in `c725ef0` (scoring guard + game ownership check), #2 still open (see CLAUDE.md "Known issues"), #3 was already fixed in `9e8543c`.

## Goal
Admins (multiple per tenant) need a clear way to stop or remove a tournament. The action depends on the tournament's state:

| Status | User action | Meaning |
|---|---|---|
| Setup | Delete | Nothing played yet, remove it entirely (existing behavior) |
| Any started state before Finished | Cancel | Tournament stops for good; data kept, read-only |
| Finished | none | Permanent |
| Cancelled | none | Final, no un-cancel |

There is no user-facing delete after Setup. We clean up the DB ourselves when needed, outside the app.

## Current state (verified)
- `DELETE /api/tournaments/{id}` works for Setup only, incl. cleanup of teams exclusive to the tournament. DB cascades from Tournament (standings, matches, games, group entries, tournament-teams) are already configured.
- `Cancelled` status exists; state machine allows it from every non-terminal state.
- `TournamentService.SoftDeleteTournamentAsync` is commented-out leftover code.
- `PUT /api/tournaments/{id}/status` is a leftover generic status setter, unused by UI. Lets clients jump states without running pipelines.
- UI: navbar "Cancel Tournament" button (Setup only) actually calls delete, behind a native `confirm()`. SignalR delete toast says "cancelled".
- `TournamentStateDTO` already exposes state-derived flags (`CanScoreMatches`, `CanModifyTeams`) that the UI consumes.
- `Tournament.Name` has a global unique index. Leftover from pre-multi-tenant design; blocks other tenants from reusing a name.
- Tournament list groups into upcoming / ongoing / finished; Cancelled currently lands in finished.

## Requirements

### Cancel
- Available in any state after Setup and before Finished.
- Final. Confirmation must say so and say that played data stays viewable.
- Record who cancelled and when. Optional free-text reason.
- Other admins viewing the tournament are notified live (SignalR) and the page becomes read-only.
- Cancelled tournament page: shows a cancelled notice (who, when, reason if given); standings/bracket still viewable; no scoring/seeding/team actions.

### Delete
- Available only in Setup (whole Setup, not only while registration is open).
- Keep existing exclusive-team cleanup.
- Confirmation says it is permanent.
- Deleting admin returns to the list; other viewers get a correct "deleted" notice and are moved off the page.

### Rules live in the backend
- Which actions are allowed per state is decided server-side (domain/state machine level) and exposed to the UI. UI must not duplicate the status rules.
- Wrong-state requests return a proper 4xx via existing domain exceptions, not 500.

### Cleanup of leftovers
- Remove `PUT /api/tournaments/{id}/status` and its service method if nothing else uses it.
- Remove commented-out `SoftDeleteTournamentAsync`.
- Remove the tournament name unique index. Duplicate names are allowed (also within the same tenant).

### Tournament list
- Cancelled tournaments get their own section/filter. Not shown under upcoming, ongoing or finished.

### UI
- Replace native `confirm()` with the existing shared confirm-dialog component.
- Cancel and Delete are never shown together. Both styled as destructive.

## Scope
- Backend: domain rule(s), service methods, endpoint(s), DTO changes, one EF migration (cancel info fields + name index removal).
- Frontend: navbar actions, confirm dialogs, cancelled read-only view, SignalR handling for cancel/delete, list section.
- Tests (xUnit + FluentAssertions, existing test bases):
  - allowed actions per status, for all statuses
  - cancel rejected in Setup, Finished, Cancelled; accepted otherwise; stores who/when/reason
  - delete rejected outside Setup
  - two tournaments with the same name can be created

## Restrictions
- No soft-delete flag, no archive concept, no admin/cleanup endpoint, no roles.
- No un-cancel.
- Do not change the state machine transition table.
- Follow CLAUDE.md patterns: UnitOfWork for saves, domain exceptions, DI registration, NgModule components.
- No refactors outside this feature. Keep changes minimal and readable (solo/junior project).
- Do not fix unrelated bugs. Note them (see below) and continue.

## Known bugs, OUT OF SCOPE (do not fix, separate task)
1. Server-side scoring is not guarded by status: `GameResultPipeline` never checks `CanScoreMatches`. A cancelled tournament stays scorable via API; UI hides the controls. Leave as is.
2. `TournamentStateMachine.ValidateTransition` throws `InvalidOperationException`, unmapped in `GlobalExceptionHandler` -> 500. New cancel/delete code must use domain exceptions itself; do not change `ValidateTransition` globally.
3. Format-blind `ValidateTransition` calls in the Start pipelines.

If you find other unrelated bugs, list them in your final report with file/line. Do not fix.

## Done when
- Build and all tests pass (backend + frontend).
- Migration generated and applies cleanly.
- Manual flow works: create -> delete in Setup; start -> cancel mid-groups and mid-bracket; second browser session sees live cancel/delete; cancelled appears only in its own list section; same name reusable.
- Final report: what changed, decisions you made, open questions, unrelated bugs found.
