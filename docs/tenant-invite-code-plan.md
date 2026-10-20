# Tenant invite codes

## Context
`AuthService.RegisterAsync` joins any existing tenant whose name is typed (`FindAsync(t => t.Name == tenantName)`), so anyone who knows an org name gets into its data. Fix: a new name still creates an org; an existing name needs that org's invite code. Any member generates the code from the user menu; one active code per org, reusable, expires after 1 day. No roles or ownership. One user = one tenant already holds (`User.TenantId`, `RequireUniqueEmail`); no change there.

Work phase by phase.

## Decisions
- **Storage**: on `Tenant`, not a new table: `InviteCodeHash` (string?, 64) + `InviteCodeExpiresAt` (DateTime?). Generating overwrites both, so the old code dies and there is only ever one. Plain value never stored (same reasoning as refresh tokens).
- **Code**: 24 chars from a 32-char unambiguous uppercase alphabet (no 0/O/1/I), ~120 bits, `RandomNumberGenerator.GetString`. Shown as `XXXX-XXXX-XXXX-XXXX-XXXX-XXXX`; input normalized (strip `-`/spaces, uppercase) before hashing.
- **Hash**: extract `Application/Utilities/SecretHasher.Sha256Hex(string)` (BCL only, uppercase hex). `JwtTokenService.HashRefreshToken` delegates to it, so refresh token hashes and the `HashRefreshTokens` migration stay valid.
- **Name rule**: trim; case-insensitive. New `Tenant.NormalizedName` (= trimmed `ToUpperInvariant()`, Identity's `NormalizedUserName` pattern), unique index on it; `Name` keeps display casing, its old unique index is dropped. The DB index is what stops two simultaneous "new" orgs with the same name. Local data: 30 tenants, no case/space duplicates, all ASCII.
- **Register flow** (`RegisterRequest` gets `InviteCode?`):
  - name unknown + no code: create tenant (as now)
  - name unknown + code: 400 "Organization not found" (user meant to join; avoids creating a typo org)
  - name known + no code: 409 "Invite code required"
  - name known + wrong/expired code: 400 "Invalid or expired invite code" (same message for both)
  - name known + valid code: join; code stays valid for others until expiry
  - race on create (unique index violation, `DbUpdateException`): `UnitOfWork` maps SQL state 23505 to `ConflictException`, `RegisterAsync` rethrows it as 409 "Organization name was just taken"
- **Org exists check**: `GET api/auth/organizations/exists?name=` (anonymous) → `{ exists }`, so the register form can show the code field as the name is typed. Enumeration is an accepted trade-off (documented in CLAUDE.md Known trade-offs).
- **Generate**: `POST api/tenant/invite-code` ([Authorize]) → `{ code, expiresAt }`. `TenantInviteService.CreateInviteCodeAsync`: tenant from `ITenantService.GetCurrentTenantId()`, inactive → `ForbiddenException`, `tenant.SetInviteCode(hash, now + 1 day)`, save. Concurrent generate → existing `Version` check → 409.
- **Rate limit**: built-in `AddRateLimiter` (no package), fixed window per client IP, policy `auth`: 10 req/min on register, exists check, login → 429. `UseForwardedHeaders` (X-Forwarded-For, `ForwardedLimit = 1`, cleared known proxies) so Render's proxy doesn't put everyone in one bucket.

## Phase 1: backend
- Domain `Tenant`: `NormalizedName`, `InviteCodeHash`, `InviteCodeExpiresAt`; `static NormalizeName(string)`, `Name` setter also sets `NormalizedName`, `SetInviteCode(hash, expiresAt)`, `IsInviteCodeValid(hash, now)`.
- `Application/Utilities/SecretHasher.cs`; `JwtTokenService.HashRefreshToken` uses it.
- `TO2DbContext`: `NormalizedName` required, max 100, unique; drop `Name` unique; `InviteCodeHash` max 64.
- Migration `TenantInviteCodes`: add columns, `UPDATE "Tenants" SET "NormalizedName" = upper(trim("Name"))`, swap indexes.
- `RegisterRequest.InviteCode` (`[MaxLength(50)]`); `AuthService.RegisterAsync` flow above; `AuthService.OrganizationExistsAsync(name)`.
- New `Contracts/ITenantInviteService` + `Services/TenantInviteService`, DTOs `InviteCodeResponse(Code, ExpiresAt)`, `OrganizationExistsResponse(Exists)`.
- `AuthController`: exists endpoint, `[EnableRateLimiting("auth")]` on register/login/exists. New `TenantController` with `POST invite-code`.
- `Program.cs`: register service, rate limiter, forwarded headers (`app.UseForwardedHeaders()` first, `app.UseRateLimiter()` after `UseCors`).
- Tests (`AuthServiceTests`, new `TenantInviteServiceTests`): create on new name; existing name no code → 409; wrong / expired / other org's code → 400; valid code joins; same code used twice; case/space variant of name needs code; code for unknown org → 400; generate stores only hash, expiry ~1 day, regenerate kills old code; inactive tenant → 403; duplicate `NormalizedName` insert → `DbUpdateException` (index exists).
- CLAUDE.md: register/invite flow, endpoints, rate limit, enumeration trade-off.

CONFIRM: tests green; curl on real API: create org, exists true/false, register without code 409, generate code, register with code 200, wrong code 400, 11th register in a minute 429.

## Phase 2: UI (minimal)
- `register.component`: debounced exists check on org name; if exists show "Invite code" field + hint "Organization exists. Ask a member for an invite code."; send `inviteCode`; 429 message.
- Navbar user menu: "Invite code" item → small `InviteCodeDialogComponent` (p-dialog): text "Generating replaces the current code", Generate button, then code + expiry + Copy.
- `AuthService` (UI): `organizationExists(name)`, `createInviteCode()`; models updated.

CONFIRM: short browser check: generate in one account, register second account with it.

## Out of scope / stop-and-ask
- No roles, ownership, kicking members, leaving/switching orgs, email invites, showing the active code again (it's hashed).
- Stop and ask if: existing tenant data breaks the normalized unique index, or rate limiting needs Render-specific config beyond forwarded headers.

## Resolved
- No active-code status GET; login is rate-limited too; 10 req/min per IP.
- Same hasher as refresh tokens: yes. Plain SHA-256 is enough for random secrets with 120+ bits (an offline brute force of 2^120 is not feasible). Slow, salted hashes (bcrypt/PBKDF2) are for guessable secrets like passwords. With no salt the hash is deterministic, so lookup by hash can use an index.
