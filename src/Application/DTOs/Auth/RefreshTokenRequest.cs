using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{
    public record RefreshTokenRequest([Required] string RefreshToken);
}
