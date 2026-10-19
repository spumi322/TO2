using Domain.Entities;

namespace Application.DTOs.Auth
{
    /// <summary>
    /// Raw refresh token value for the client, plus the entity to persist (holds only the hash).
    /// </summary>
    public record IssuedRefreshToken(string Token, RefreshToken Entity);
}
