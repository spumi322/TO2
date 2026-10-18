# Clean Architecture leaks plan

Remaining layer leaks between Domain, Application and Infrastructure. Each phase builds, keeps all tests green and ends with a browser check (register, login, token refresh, one tournament page).

`Program.cs` registering Infrastructure types (DbContext, repositories, interceptors) is accepted: it is the composition root, the one place allowed to know every layer.

## Leaks covered

| # | Leak | Where |
|---|---|---|
| A | Application calls ASP.NET Identity and builds JWTs | `AuthService`: `UserManager<User>`, `JwtSecurityTokenHandler`, `SymmetricSecurityKey`; `Application.csproj` references `System.IdentityModel.Tokens.Jwt` |
| B | Domain depends on ASP.NET Identity | `Domain/Entities/User.cs` (`User : IdentityUser<long>`), `Domain.csproj` references `Microsoft.Extensions.Identity.Stores`; `Tenant.Users`, `RefreshToken.User` navigations |

## Out of scope

- Behavior changes: auth flow, claims, token lifetimes, tenant join rules, SignalR group checks stay as they are.
- DataAnnotations on entities, `Expression<Func<T,bool>>` in `IRepository` (minor, kept).
- Schema changes. Phase 2 may add a migration that only updates the model snapshot; any real column/table change = stop and ask.

## Phase 1 - Identity and JWT behind Application interfaces (A)

- `Application/Contracts/ITokenService`: `string CreateAccessToken(long userId, string email, string userName, Tenant tenant)` (claims unchanged: `sub`, `email`, `name`, `tenantId`, `tenantName`, `jti`) + `string CreateRefreshTokenValue()`.
- `Application/Contracts/IIdentityService`: `FindByEmailAsync`, `FindByIdAsync`, `CreateUserAsync(email, userName, password, tenantId)`, `CheckPasswordAsync`; returns a small Application record (`IdentityUserInfo(Id, Email, UserName, TenantId)`), not the Identity type.
- Implementations in `Infrastructure/Identity/` (`JwtTokenService`, `IdentityService` wrapping `UserManager<User>`). `JwtSettings` moves to Infrastructure with the token service.
- `AuthService` keeps the flow (tenant lookup/create, transaction, refresh token rows) and only talks to the two interfaces.
- `Application.csproj` drops `System.IdentityModel.Tokens.Jwt`.
- Tests: `AuthService` unit tests with mocked interfaces (register new/existing tenant, inactive tenant, wrong password, refresh).

CONFIRM: AuthService has no `Microsoft.AspNetCore.Identity`/`IdentityModel` usings, login/register/refresh behave the same.

## Phase 2 - Identity user out of Domain (B)

Recommended: Identity user type lives in Infrastructure.
- `Infrastructure/Identity/ApplicationUser : IdentityUser<long>` with `TenantId`; `TO2DbContext : IdentityDbContext<ApplicationUser, IdentityRole<long>, long>`.
- Domain: delete `User`; `RefreshToken` keeps `UserId` only; `Tenant.Users` removed. Relationships configured in `TO2DbContext` (`HasOne<ApplicationUser>().WithMany().HasForeignKey(...)`).
- `Domain.csproj` drops `Microsoft.Extensions.Identity.Stores` (Domain has zero package references).
- New migration expected to change only the model snapshot (type names). Generated SQL must be empty, else stop.

Alternative: keep `User : IdentityUser<long>` in Domain and document it as an accepted trade-off. Less work, one leak stays.

CONFIRM: Domain has no package references, migration SQL empty, existing users can still log in.

## Stop and ask

- A migration generates real DDL.
- A claim name or token shape would change.
- Tests need rewriting beyond constructor changes.
- File writes fail repeatedly.

## Open questions

1. Phase 2: move user to Infrastructure, or keep in Domain + document?
2. Phase 1: `JwtSettings` to Infrastructure ok?
