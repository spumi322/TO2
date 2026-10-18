# Bracket seeding strategy per format - implementation plan

Status: all phases implemented and tested (unit tests + real app); awaiting review/commit.

## Goal

GroupsAndBracket bracket round 1:
- group winners never face each other (when avoidable)
- opponents never come from the same group (when avoidable; otherwise log a warning and continue)

Achieved by moving "how round 1 is formed" into one seeding strategy per format, inside the shared StartBracket pipeline.

## Target design

- `StandingService.GetTeamsForBracketByFormat` answers only "who advances": returns `List<BracketEntrant>` (team + group id + group placement; group fields null for BracketOnly). No ordering.
- `IBracketSeedingStrategy` (`Application/Pipelines/StartBracket/Strategies/`) - one implementation per format:
  - `RandomSeedingStrategy` (BracketOnly): shuffle -> pad with BYEs -> pairs
  - `GroupResultSeedingStrategy` (GroupsAndBracket): winners first -> pad -> pairs -> separate same-group pairs
- `CalculateBracketStructureStep` selects the strategy by `Tournament.Format` (same pattern as `HandleStandingProgressStep` + `IStandingProgressStrategy`).
- `RandomSeedTeamsStep` is deleted.
- Strategies are pure and synchronous (group info travels with the entrants, no DB access).

## Phases

Each phase ends with: tests green, browser check, summary + suggested commit message. The user reviews and commits; the next phase starts only after confirmation.

### Phase 0 - Baseline (done)

Winners-first ordering in `StandingService` committed (`e87004b`).

### Phase 1 - Strategy scaffolding (no behavior change)

- Add `IBracketSeedingStrategy` (`Format`, `CreateFirstRoundPairs(List<Team>)`), `RandomSeedingStrategy` (shuffle from `RandomSeedTeamsStep`), `GroupResultSeedingStrategy` (pad + pair the list as received).
- `CalculateBracketStructureStep` injects `IEnumerable<IBracketSeedingStrategy>`, selects by format; no matching strategy -> clear exception.
- Delete `RandomSeedTeamsStep`; update DI in `Program.cs`.
- Tests: strategy selection by format; random strategy keeps every team; adjust existing step tests.
- Browser: BracketOnly start + GroupsAndBracket start behave as before.

CONFIRM: end of Phase 1.

### Phase 2 - Seeding ownership moves to the strategy

- Add `BracketEntrant(Team Team, long? GroupId, int? GroupPlacement)` in `Application/DTOs/Standing`.
- `GetTeamsForBracketByFormat` returns `List<BracketEntrant>`; still marks Advanced/Eliminated; no ordering.
- `StartBracketContext.AdvancedTeams` (name kept) becomes `List<BracketEntrant>`; adjust `ValidateTeamCountStep`, `BuildResponseStep`, strategy signature.
- Winners-first ordering lives in `GroupResultSeedingStrategy`.
- Tests: service test covers selection/statuses only; new pure unit test covers strategy ordering.
- Browser: 3 groups x 2 advancing bracket identical to Phase 0 result.

CONFIRM: end of Phase 2.

### Phase 3 - No same-group opponents in round 1

- `BracketSeedingUtility.SeparateSameGroupPairs(pairs, groupOf)`: pure; swaps lower-seeded teams between real round-1 matches; returns number of unresolved clashes.
- `GroupResultSeedingStrategy` calls it; unresolved > 0 -> log warning, bracket still starts.
- Tests: 3x2, 5x3, 7x2 -> no clash; winners still never meet; single group and impossible under-registered setups -> clash kept + warning.
- Browser: 3x2 bracket has no same-group round-1 match; 2x2 sanity check.

CONFIRM: end of Phase 3.

### Phase 4 - Docs and comment review (after the implementation is tested and ready)

- `CLAUDE.md`: StartBracket step list, Strategy pattern section (add `IBracketSeedingStrategy`), "Adding new formats" checklist (add seeding strategy); remove historical wording across the file ("Previously...", "moved from...").
- SKILL.md files: none exist for TO2 at plan time; update any that exist by then.
- Claude memory (`MEMORY.md` + memory files): reflect the new design where relevant.
- Code comments in touched files: `GetAdvancedTeamsStep` ("BracketAndGroup" -> GroupsAndBracket), step-number comments after the step deletion, `GenerateSeedingOrder` doc example (comment only).
- All text describes current state only: no "previously", "moved from", "replaces X".

CONFIRM: end of Phase 4.

## Boundaries

In scope:
- StartBracket pipeline (steps, contracts, strategies, utilities)
- Bracket part of `StandingService` + `IStandingService`
- `BracketEntrant` DTO
- `Program.cs` DI lines for the StartBracket pipeline
- Tests for the above
- Phase 4 docs/comments listed above

Out of scope (mention only, never change):
- `GenerateSeedingOrder` algorithm (non-standard order: seed 1 meets seed 3 in semis)
- Same-group avoidance in round 2+ or across bracket halves
- UI (BYE vs TBD label in later rounds, native `confirm()` dialogs)
- GameResult / StartGroups pipelines, state machine, GroupsOnly format, group ranking tie-breaks
- New pipelines, changes to pipeline/step interfaces, `PipeLineBase`
- DB schema, migrations, API response contracts
- Known Issues list, `docs/archive`, interview prep doc
- Renames not required by the change

Stop and ask when:
- a phase needs a file outside the scope above
- tests fail outside the touched area
- the browser misbehaves twice
- the design does not fit as planned

## Process

- Claude does not commit; each phase ends at "ready to commit" with a suggested message.
- Browser testing: test data prepared via API script; browser used briefly to start the bracket and inspect it; one tab, closed afterwards. Each run creates a new test tenant in the local DB.
