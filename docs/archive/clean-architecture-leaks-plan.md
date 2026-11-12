# Clean Architecture leaks plan

Layer leaks between Domain, Application and Infrastructure, and how each one is handled.

## Decisions

| Leak | Decision |
|---|---|
| `Program.cs` registers Infrastructure types (DbContext, repositories, interceptors) | Accepted. `Program.cs` is the composition root, the one place allowed to know every layer. An `AddInfrastructure()` extension would only move the same lines. |
| Application called ASP.NET Identity and built JWTs in `AuthService` | Fixed. `IIdentityService` and `ITokenService` (Application contracts), implemented by `IdentityService` and `JwtTokenService` in `Infrastructure/Identity`. `JwtSettings` and the JWT package live in Infrastructure. |
| Domain `User : IdentityUser<long>` (package `Microsoft.Extensions.Identity.Stores`) | Accepted. A separate Identity user type in Infrastructure would add mapping and relationship config for no feature gain. Only `IdentityService` and `TO2DbContext` use `User` as an Identity type. |
| DataAnnotations on entities, `Expression<Func<T,bool>>` in `IRepository` | Accepted (minor). |

## Verification for the fixed leak

- `AuthService` has no `Microsoft.AspNetCore.Identity`/`IdentityModel` usings; `Application.csproj` has no JWT package.
- Tests: `AuthServiceTests` (register new/existing tenant, duplicate email, inactive tenant, rollback on failed user creation, login, refresh, revoked refresh), `JwtTokenServiceTests` (claims, issuer/audience, expiry).
- Real API: register, login, protected endpoint, refresh, error codes 401/409/400 unchanged; failed register leaves no tenant row.
