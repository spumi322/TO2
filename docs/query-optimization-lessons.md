# Query Optimization Lessons (TO2)

A walkthrough of real N+1 and over-fetching problems in this codebase. Each lesson has the concept, where it is in the code, the evidence, why EF Core behaves that way, a hint for the fix, and questions to talk through. Fix them yourself; spoilers are folded.

Work through them in order: each one builds on the previous.

---

## 0. Measuring before fixing

Never optimize from a hunch. Count what the database actually does.

**What to count**
- **Round trips** (one `DbCommand` sent to the DB). EF logs every one as `Executed DbCommand (Xms)` at Information level, with the SQL below it. This app already logs them, so the API console output is your profiler.
- **Rows and columns returned** (over-fetching shows up as large result sets, not as many round trips).
- **Response bytes** (over-fetching on the HTTP side).

**Tool:** `docs/tools/measure_queries.py` creates a throwaway tenant, plays a whole GroupsAndBracket tournament through the API and prints round trips per request type. Usage is at the top of the file. Run it at two or three sizes: a count that is flat is fine; a count that grows with teams/groups/matches is an N+1.

**Baseline (before any fix)**

| Request | 8 teams / 2 groups | 16 / 4 | 32 / 8 | Grows with |
|---|---|---|---|---|
| POST start-groups | 26 | 46 | 86 | teams + matches |
| GET standings/{id}/groups | 3 | 5 | 9 | groups |
| PUT game result (group game) | – | 11–14 | 15–18 | groups |
| POST start-bracket | 9 | 11 | 15 | round 1 matches |
| PUT game result (bracket game) | 7–14 | 7–14 | 7–14 | flat |
| GET final-standings | 4 | 4 | 4 | flat (but see lesson 5) |
| GET list/details/state/teams | 1 | 1 | 1 | flat |
| POST add team to tournament | 6 | 6 | 6 | flat |

**Reading a growth pattern:** start-groups is `6 + teams + matches` (26 = 6 + 8 + 12, 86 = 6 + 32 + 48). Once you can write the formula, you know which loops to look for.

**Questions**
- Why measure at three sizes and not one?
- Why is "flat but 14 queries" a smaller problem than "9 queries that grow"?

CONFIRM: understood how to read the log and the table.

---

## 1. Classic N+1: a query inside a loop

**Concept.** You load N parent rows with one query, then loop and run one more query per row. 1 + N round trips instead of 1 or 2. Each round trip costs network latency even when the query itself is fast, so 50 tiny queries are much slower than 1 slightly bigger one.

**Where.** `Application/Pipelines/StartGroups/Steps/DistributeTeamsIntoGroupsStep.cs:42`

```csharp
var tournamentTeams = await _tournamentTeamRepository.FindAllAsync(tt => tt.TournamentId == context.TournamentId);
foreach (var tt in tournamentTeams)
{
    var team = await _teamRepository.GetByIdAsync(tt.TeamId);   // 1 query per team
    ...
}
```

**Evidence.** start-groups grows by exactly 1 per team (the `teams` part of the formula).

**Why EF does it.** `GetByIdAsync` is `FindAsync`: it checks the change tracker first and only hits the DB if the entity isn't tracked yet. Here nothing loaded the teams before, so every call is a round trip.

**Hint.** You already know all the team IDs after the first query. Ask for all of them at once. Look at how `StandingService` loads advanced teams for the bracket (`FindAllAsync(t => ids.Contains(t.Id))`) — what SQL does `Contains` turn into?

<details><summary>Solution sketch</summary>

Collect `teamIds`, load `FindAllAsync(t => teamIds.Contains(t.Id))` once (SQL `WHERE "Id" = ANY(@ids)` / `IN (...)`), then build a dictionary by Id if you need lookups. Alternative: load `TournamentTeams` with `.Include(tt => tt.Team)` in one query.
</details>

**Verify.** start-groups drops by `teams` (86 → 54 at 32 teams).

**Questions**
- `Include` (join) vs. a second `Contains` query: when would you pick each?
- Is there a limit to how many IDs you can put in `Contains`?

CONFIRM: fixed and measured.

---

## 2. N+1 hidden in a helper method

**Concept.** Same pattern, but the loop is inside a service method, so the caller can't see it. These are the ones that survive code review.

**Where.** `Application/Services/StandingService.cs:509` `GetGroupsWithDetailsAsync`

```csharp
var standings = await _standingRepository.GetGroupsWithMatchesAsync(tournamentId);   // 1 query
foreach (var standing in standings)
{
    var groupEntries = await _groupRepository.GetByStandingIdOrderedAsync(standing.Id); // 1 per group
    ...
}
```

**Evidence.** GET groups = 1 + groups (3, 5, 9). It is also called on every group game result (lesson 4), so the cost multiplies.

**Hint.** All group entries of a tournament can be fetched in one query (there's already a repository method by tournament). Then split them per group in memory. Watch the ordering that `GetByStandingIdOrderedAsync` applies — the response must stay identical.

<details><summary>Solution sketch</summary>

One query for all entries of the tournament, then `ToLookup(e => e.StandingId)` (or `GroupBy`) and apply the same ordering per group in memory. 2 queries total, regardless of group count.
</details>

**Verify.** GET groups = 2 at every size. Response JSON unchanged (diff it).

**Questions**
- `ToLookup` vs `GroupBy(...).ToDictionary(...)`: any difference here?
- Why is sorting in memory fine here but would be wrong for a paged list?

CONFIRM: fixed and measured.

---

## 3. A query that can never return anything

**Concept.** Not every wasted query is an N+1 of useful work. Sometimes the query is pointless because of the entity's state, and it happens to sit in a loop.

**Where.** `Application/Services/GameService.cs:35` `GenerateGames`

```csharp
var gamesAlreadyGenerated = await _gameRepository.FindAllAsync(g => g.MatchId == match.Id);
```

`GenerateGames` is called right after `GenerateMatch`, which only `AddAsync`s the match. The match isn't saved yet, so `match.Id` is `0`, and the query runs `WHERE "MatchId" = 0` once per new match.

**Evidence.** The `matches` part of the start-groups formula (48 queries at 32 teams), and the `round 1 matches` part of start-bracket.

**Why EF does it.** IDs are generated by the database on `SaveChanges`. Until then a new entity has the CLR default. EF can still link the games to the match through the navigation (`new Game(match, ...)`), which is why the insert works without a real ID.

**Hint.** What does a guard check protect against? Can that situation happen for a match that was just created in memory? Where would the check make sense?

<details><summary>Solution sketch</summary>

Skip the check when the match is new (`match.Id == 0`), or move the guard to the callers that work with existing matches. Re-run the StartGroups/StartBracket tests: the guard's behaviour for saved matches must stay.
</details>

**Verify.** start-groups ≈ 7 at every size (after lesson 1), start-bracket ≈ 7.

**Questions**
- How would you have found this one without the SQL log?
- Is "ID = 0 means unsaved" a safe rule in this codebase? (Check CLAUDE.md "Old data" known issue.)

CONFIRM: fixed and measured.

---

## 4. Loading everything to use one piece

**Concept.** Reusing a big "get everything" method for a small need. Each call looks harmless; the waste is in rows you load and throw away.

**Where.** `Application/Pipelines/GameResult/GameResultPipeline.cs:173` `BuildGameUpdatedEventAsync`, line 191:

```csharp
var allGroups = await _standingService.GetGroupsWithDetailsAsync(context.Tournament.Id);
updatedGroup = allGroups.FirstOrDefault(g => g.Id == context.GameResult.StandingId);
```

After every group game is committed, this loads **all** groups with all matches and all games (plus the lesson 2 N+1), to send **one** group in the SignalR event.

**Evidence.** Group game result = 6–8 pipeline queries + `1 + groups` broadcast queries. At 8 groups: 15–18. In the log the extra queries appear right before `Pipeline completed successfully`.

**Hint.** What does the event actually need? Look at `GetGroupsWithDetailsAsync`: could the per-group part become its own method that takes one standing ID?

<details><summary>Solution sketch</summary>

Extract "build one group DTO" into a method that loads one standing with matches + games and that standing's entries (2 queries). Use it in the broadcast; reuse it inside `GetGroupsWithDetailsAsync` only if it doesn't bring the N+1 back.
</details>

**Verify.** Group game result flat at ~8 (game only) / ~10 (match done), any group count.

**Questions**
- The bracket branch sends the whole bracket on every bracket game. Is that also a problem? (Payload ~7.6 KB at 16 teams; the UI replaces the whole bracket with it.)
- Why is the broadcast outside the transaction, and does that change how expensive it may be?

CONFIRM: fixed and measured.

---

## 5. Filtering in memory instead of in the database

**Concept.** Over-fetching rows: load a whole table, then filter with LINQ in C#. The SQL has no `WHERE`, so cost grows with the table, not with what you need.

**Where.** `Application/Services/StandingService.cs:277` and `:357`

```csharp
var allTeams = await _teamRepository.GetAllAsync();                 // every team in the tenant
var bracketTeams = allTeams.Where(t => teamIds.Contains(t.Id)).ToList();
```

**Evidence.** The round-trip count is flat (4), so lesson 0's table hides it. The cost is in the row count: the measurement tenant has 16 teams, a real tenant with many tournaments has hundreds, and this runs on every final-standings request.

**Why it happens.** `GetAllAsync` returns `IReadOnlyList<T>` (already executed). The `.Where` after it is LINQ-to-Objects, not SQL. Once you leave `IQueryable`, every following operator runs in memory.

**Hint.** You already used the fix in lesson 1.

**Questions**
- Why can't a generic `GetAllAsync()` ever be safe on a multi-tenant table that keeps growing?
- How do you tell from a method signature whether filtering will happen in SQL or in memory?

CONFIRM: fixed and verified (log shows `WHERE ... = ANY` / `IN`).

---

## 6. Include loads whole graphs

**Concept.** Over-fetching related data. `Include` loads the full related entities (all columns, all rows of that navigation). Chaining `ThenInclude` multiplies it.

**Where.** `Infrastructure/Persistence/Repository/TournamentRepository.cs:17` `GetWithTeamsAsync`:

```csharp
.Include(t => t.TournamentTeams)
    .ThenInclude(tt => tt.Team)
        .ThenInclude(t => t.TournamentParticipations)   // every team's rows in ALL tournaments
```

It is built for delete (needs "is this team in another tournament?"), but `TournamentService.GetTeamsByTournamentAsync` (line 150) reuses it just to list team id + name. The UI calls that on every details-page load and reload.

**Hint.** Projection: `Select` only what the DTO needs and EF generates SQL for just those columns, no tracking, no extra navigations. AutoMapper has `ProjectTo<T>()` for this.

**Questions**
- Why is a repository method named after one use case ("WithTeams") risky to reuse?
- Projection vs Include: which one gives you tracked entities, and when do you need them?

CONFIRM: fixed and verified (SQL selects only needed columns, no `TournamentParticipations` join).

---

## 7. Counting by loading

**Concept.** Loading child rows only to call `.Count` on them in C#.

**Where.** `TournamentRepository.cs:24` `GetAllForListAsync` (`.Include(t => t.TournamentTeams)`) + `TournamentService.cs:86`:

```csharp
.Select(t => _mapper.Map<GetTournamentListResponseDTO>(t) with { CurrentTeams = t.TournamentTeams.Count })
```

Every registration row of every tournament is loaded to produce one number per tournament.

**Hint.** Inside a query, `t.TournamentTeams.Count()` becomes a `COUNT(*)` subquery in SQL. Move the count into the query with a projection.

**Questions**
- `.Count` (property on a loaded list) vs `.Count()` inside an `IQueryable` expression: what decides where it runs?
- This is the tournament list page. What would you add when a tenant has 1000 tournaments?

CONFIRM: fixed and verified (SQL has `COUNT`, no join to `TournamentTeams` rows).

---

## 8. Cartesian explosion (concept, measure first)

**Concept.** `Include` of a collection inside a collection is one SQL query with joins. Every parent column is repeated for every child row. `Standings → Matches → Games` returns one row per game, with standing and match columns duplicated on each.

**Where.** `Infrastructure/Persistence/Repository/StandingRepository.cs:21` `GetGroupsWithMatchesAsync` (`Include(Matches).ThenInclude(Games)`); same in `GetBracketWithMatchesAsync`.

**Numbers.** 32 teams / 8 groups: 48 matches × 3 games = 144 rows for 8 standings. Small. It becomes a real problem with two sibling collection includes (rows multiply: A × B), which this code doesn't have.

**The tool.** `AsSplitQuery()` sends one query per included collection instead of one join. Fewer duplicated rows, more round trips, and no single consistent snapshot unless inside a transaction.

**Questions**
- Is it worth changing here? What would you measure to decide?
- Why does EF warn about this only for multiple collection includes?

CONFIRM: decided (change or leave) with a reason.

---

## 9. Tracking: know when you rely on it

**Concept.** Every tracked entity costs memory and a snapshot for change detection. Read-only queries can use `AsNoTracking()`. But tracking is also a cache: `FindAsync` returns an already tracked entity without a query.

**Where this codebase relies on it.** `GameResultPipeline.ValidateCanScoreAsync` (line 248) loads the game, match and standing. `ScoreGameStep` then calls `GetByIdAsync` for the same game: no SQL in the log, because the entity is already tracked. The same tracked instances are modified and saved, and their `Version` is checked on save.

**Questions**
- What would break, or get slower, if `Repository<T>` used `AsNoTracking()` for everything?
- Which endpoints are pure reads and could safely use no-tracking or projection?

CONFIRM: understood; no change, or a scoped change to read endpoints only.

---

## 10. Over-fetching from the client

The UI decides how often the API runs all of the above.

**Where.** `src/UI/src/app/components/tournament/tournament-details/tournament-details.component.ts`
- **Line 71 / 458 `loadAllTeams`:** loads every team in the tenant on each page open, also when teams can no longer change (`canModifyTeams` false).
- **Line 327 `loadStandings`:** always requests both groups and bracket, whatever the format and stage.
- **Line 253 `loadBracketMatches`:** fetches bracket matches that the bracket response already contains.
- **Line 683 `reloadTournamentData`:** many events trigger a full reload (tournament + teams + state + groups + bracket). Start-groups and start-bracket each broadcast two events (`StartGroupsPipeline.cs:119`, `StartBracketPipeline.cs:120`), so other viewers reload twice. Tenant-wide events aren't filtered by tournament (CLAUDE.md known issue 1).

**Contrast.** Game results already do it right: the event carries the changed group, and the UI patches its state instead of refetching.

**Questions**
- Which of these does the backend fix, and which only the client can fix?
- Patching from the event payload vs refetching after an event: trade-offs (payload size, consistency, missed events)?

CONFIRM: picked which client changes to do.

---

## 11. When a flat count is fine

Not every query is worth removing.

- **Add team** (`TeamService.cs:92`): 6 queries, all small guard checks (exists, state, duplicate, name taken, count). Flat, user-triggered once per team.
- **Game result validation** (`GameResultPipeline.cs:248`): 4 lookups (tournament, game, match, standing). They make later steps free through tracking (lesson 9).

**Questions**
- Could the add-team checks be one query? Would it be more readable?
- What makes an optimization worth its complexity in an interview-reference project?

CONFIRM: agreed what to leave alone.

---

## Target after lessons 1–5

| Request | Before (32 / 8) | Target |
|---|---|---|
| POST start-groups | 86 | ~7 |
| GET groups | 9 | 2 |
| PUT game result (group) | 15–18 | ~8–10 |
| POST start-bracket | 15 | ~7 |

Lessons 6–7 don't change round trips; verify them in the SQL (columns, joins, `COUNT`).
