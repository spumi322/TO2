using Application.DTOs.Auth;
using Domain.AggregateRoots;
using Domain.Entities;

namespace Application.Contracts
{
    /// <summary>
    /// Issues tokens. Implemented in Infrastructure with JWT.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Signed access token with the sub, email, name, tenantId, tenantName and jti claims.
        /// </summary>
        AccessToken CreateAccessToken(AuthUser user, Tenant tenant);

        /// <summary>
        /// New refresh token with a random value and expiry; the caller persists it.
        /// </summary>
        RefreshToken CreateRefreshToken(long userId);
    }
}
