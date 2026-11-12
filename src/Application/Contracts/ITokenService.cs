using Application.DTOs.Auth;
using Domain.AggregateRoots;

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
        /// New refresh token: random value for the client and an entity holding only its hash; the caller persists the entity.
        /// </summary>
        IssuedRefreshToken CreateRefreshToken(long userId);

        /// <summary>
        /// Hash stored in place of the refresh token value; used to look up a token the client sends.
        /// </summary>
        string HashRefreshToken(string token);
    }
}
