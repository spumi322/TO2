using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{
    public record RefreshTokenRequest([Required][MaxLength(200)] string RefreshToken);
}
