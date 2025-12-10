using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{
    public record RegisterRequest(
        [Required][MaxLength(100)] string UserName,
        [Required][EmailAddress] string Email,
        [Required][MinLength(6)] string Password,
        [Required][MaxLength(100)] string TenantName
    );
}
