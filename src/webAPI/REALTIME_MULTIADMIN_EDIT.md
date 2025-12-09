# Real-Time Multi-Admin Editing for Tournaments (Plan for Junior Dev)

Goal (for this demo project)
- Multiple admins from the **same tenant** can edit the **same tournament** at the same time.
- Backend prevents silent overwrites (optimistic concurrency).
- UI auto-refreshes the tournament when someone else changes it (no polling).
- Tech stack: .NET 8 WebAPI + PostgreSQL + EF Core + Angular 17 + PrimeNG.   

This is a **guide/checklist**, not code. You implement the code following these steps.

---

## 0. Scope & Dependencies

You will touch:

- Backend:
  - Domain: `Tournament`, `Match` (and possibly `Group`).
  - Application: DTOs + pipelines that change tournaments/matches (e.g. `GameResultPipeline`).   
  - Infrastructure: `TO2DbContext` and migrations.   
  - WebAPI: `Program.cs`, new SignalR hub, new notifier service, global exception handler.

- Frontend (Angular):
  - `src/UI` project.   
  - Tournament details component that shows a **single** tournament.
  - Optional: Tournament context service if you want to centralize the refresh.

Order matters. Follow sections 1 → 2 → 3 → 4 → 5.

---

## 1. Optimistic Concurrency (backend) – High-Level Checklist

Purpose: prevent “last write wins” overwrites when two admins save at the same time.

### 1.1 Decide which entities need concurrency

- [ ] Confirm which entities can be edited concurrently by admins:
  - At minimum: `Tournament`, `Match`.
  - Optional: `Group`, `Standing` if they are directly edited.

### 1.2 Add concurrency field in Domain

- [ ] Add a `RowVersion` property to each selected entity in `Domain`:
  - Type: `byte[]` (standard for EF row version).
  - Mark as private set or similar (Domain stays in control).

### 1.3 Configure concurrency in Infrastructure

- [ ] In `TO2DbContext` or entity configuration:
  - Mark `RowVersion` as a concurrency token using EF configuration (e.g. `IsRowVersion`).
- [ ] Create and apply a migration:
  - Add `RowVersion` columns to the relevant tables.

### 1.4 Surface RowVersion in DTOs

- [ ] Identify all **update** DTOs for tournaments and matches (e.g. “update match result”, “edit tournament”).
- [ ] Add `RowVersion` to these DTOs.
- [ ] Ensure mapping:
  - Entity → DTO includes `RowVersion`.
  - DTO → Entity lets EF track the original `RowVersion`.

### 1.5 Global handling of concurrency conflicts

- [ ] In global exception handling (WebAPI), handle EF’s concurrency exception:
  - Map it to HTTP `409 Conflict`.
  - Response body should tell the client that the resource was modified by another user.
- [ ] Decide how the API responds:
  - Minimal: just return the error with message.
  - Better: optionally include a small code or key (e.g. `errorCode: "ConcurrencyConflict"`).

### 1.6 Frontend basic handling (minimal)

Even before SignalR:

- [ ] For update calls that get a `409 Conflict`:
  - Show a clear message: “Someone else changed this. Data has been updated; try again.”
  - Option A (very simple): ask user to refresh manually.
  - Option B (still simple, recommended): auto-call `GET /tournaments/{id}` (or `/matches/{id}`) and update UI.

Once 1.x is done, you have safe saves. Next step is real-time sync.

---

## 2. Real-Time Notifications (backend) – Plan

Goal: after successful save, notify other clients watching this tournament, without polling.

### 2.1 Choose notification mechanism

- [ ] Use **SignalR** in WebAPI:
  - It supports WebSockets and falls back automatically.
  - Keep this hub **very thin**; no business logic inside.

### 2.2 Define grouping strategy for multi-tenant

- [ ] Use a **group name pattern** that includes tenant and tournament:
  - Example pattern (conceptual, not code):  
    `tenant:{TenantId}:tournament:{TournamentId}`.
- [ ] Use existing `ITenantService` to get `TenantId` in WebAPI.   

This ensures:
- Only users from the same tenant get each other’s notifications.
- Only users who joined a specific tournament get its updates.

### 2.3 Add and register the SignalR hub

- [ ] Create a new hub class in WebAPI (e.g. `TournamentHub`):
  - Methods:
    - `JoinTournament(tournamentId)` → adds connection to the correct group.
    - `LeaveTournament(tournamentId)` (optional) → removes connection.
  - Use `ITenantService` to compute group name.
  - Mark hub with `[Authorize]` (same auth model as REST).
- [ ] In `Program.cs`:
  - Add SignalR services.
  - Map the hub endpoint (e.g. `/hubs/tournament`).

### 2.4 Abstract notifier interface

Keep Application layer unaware of SignalR:

- [ ] Add a small interface to `Application` (e.g. `ITournamentRealtimeNotifier`):
  - Single method: `NotifyTournamentUpdatedAsync(tournamentId, cancellationToken)`.
- [ ] Implement interface in WebAPI:
  - Use `IHubContext<TournamentHub>` + `ITenantService` to:
    - Build group name.
    - Send an event to that group (event name like `"TournamentUpdated"` with simple payload).
- [ ] Register implementation in DI (Program.cs) as scoped.

### 2.5 Decide when to send notifications

- [ ] Identify all **write paths** that change tournament data visible in UI:
  - Pipelines: e.g. `GameResultPipeline`, `StartGroupsPipeline`, `StartBracketPipeline`, tournament metadata updates.   
- [ ] For each relevant pipeline/service:
  - After successful `UnitOfWork.SaveChangesAsync()`:
    - Call `ITournamentRealtimeNotifier.NotifyTournamentUpdatedAsync(tournamentId, ct)`.

Important:
- Only send notifications **after** save succeeded.
- Send once per request, not per individual step.

---

## 3. Real-Time Sync (frontend, Angular) – Plan

Goal: when the backend sends `"TournamentUpdated"` for a tournament the user is viewing, the UI re-fetches that tournament and updates.

### 3.1 Add SignalR client dependency

- [ ] In `src/UI`:
  - Add NPM dependency for SignalR JavaScript/TypeScript client.
- [ ] Ensure types are available so you can write strongly typed services.

### 3.2 Create a TournamentRealtimeService

This service does all SignalR handling for tournaments.

- [ ] Location: `src/UI/src/app/services/tournament-realtime.service.ts` (or similar).
- [ ] Responsibilities:
  - Start and manage the SignalR connection (single instance).
  - Expose methods:
    - `connect()` / `ensureConnected()`.
    - `joinTournament(tournamentId: number)`.
    - `leaveTournament(tournamentId: number)`.
    - `onTournamentUpdated(callback)` / expose as RxJS observable.
  - Handle reconnects if the connection drops (basic retry).

- [ ] Internals:
  - Use the same hub URL as in WebAPI (e.g. `/hubs/tournament`).
  - On join:
    - Invoke hub method `JoinTournament(tournamentId)`.
  - On leave:
    - Invoke `LeaveTournament(tournamentId)` (if you implement it).

### 3.3 Integrate with Tournament details page

- [ ] Find the **tournament details** component (the one that shows a single tournament + matches).
- [ ] On component init:
  - Get `tournamentId` from route.
  - Call `TournamentRealtimeService.ensureConnected()`.
  - Call `joinTournament(tournamentId)`.
  - Subscribe to the `"TournamentUpdated"` stream from the service.
- [ ] On receiving a `"TournamentUpdated"` event:
  - Check that `payload.TournamentId` matches the active `tournamentId`.
  - Call your existing `TournamentService.getTournamentDetails(tournamentId)` or equivalent.
  - Update local state (likely via `TournamentContextService`).
  - Optional: show a small toast (“Updated by another admin, view refreshed”).
- [ ] On component destroy:
  - Unsubscribe from events.
  - Call `leaveTournament(tournamentId)` (optional but clean).

### 3.4 Interaction with concurrency errors on the client

- [ ] For update calls:
  - If you get `409 Conflict`:
    - Call the same `getTournamentDetails(tournamentId)` to refresh, using the same logic as the real-time handler.
    - Show a specific message (“Your changes were not saved because someone else updated it first.”).

By doing this, you have:

- Push-based refresh for “viewing” changes.
- Optimistic concurrency to protect “saving”.

---

## 4. Testing Checklist

### 4.1 Concurrency behavior

Use two browsers or one browser + incognito, same tenant, different users.

- [ ] Both users open the same tournament and the same match result form.
- [ ] User A changes and saves first:
  - Save works, UI updates.
- [ ] User B (still with old data) changes and saves:
  - Request returns `409 Conflict`.
  - UI refreshes tournament data (either auto or manual).
  - User B sees updated scores from A.

### 4.2 Real-time updates

- [ ] User A and User B both open the same tournament details page.
- [ ] User A edits some match, saves successfully:
  - User A sees updated data.
- [ ] Within a short time:
  - User B sees the UI update **without** manual refresh or polling.
  - If you added a toast, it appears.

### 4.3 Tenant isolation

- [ ] User from Tenant A and user from Tenant B open their own tournaments.
- [ ] User A updates Tournament X (Tenant A):
  - Only other connections in Tenant A watching Tournament X receive `"TournamentUpdated"`.
- [ ] Verify Tenant B does **not** get any SignalR event for Tenant A’s tournament.

---

## 5. “Enterprise” Direction (for reference only)

If this were more than a demo/learning project, natural next steps:

- Presence and “who is editing what”:
  - Show avatars or initials for admins currently on the same tournament page.
  - Broadcast join/leave events in the same hub.

- More granular notifications:
  - Different events: `MatchUpdated`, `StandingUpdated`, `TournamentStatusChanged`.
  - Frontend updates only affected parts of the UI.

- Soft locks:
  - Optional “edit lock” per match/tournament with timeout, to reduce hard conflicts.

- Full audit / event sourcing:
  - Persist domain events like `MatchScored`, `TournamentStateChanged`.
  - Allow replay, audit per admin, and possibly conflict resolution tools.

---

## 6. Open Questions (please answer before implementation)

1. Exact list of entities to protect with concurrency? (`Tournament`, `Match`, `Group`, `Standing`?)
2. Which specific update endpoints should send real-time notifications? (all tournament writes or only match results?)
3. Angular: do you already have a shared WebSocket/SignalR service, or should this be the first one?
4. Do you want the client to auto-refresh **just** the current tournament, or also child views (e.g. selected group/match component) separately?
