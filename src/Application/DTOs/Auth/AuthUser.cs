namespace Application.DTOs.Auth
{
    // Login account data AuthService needs, without the ASP.NET Identity user type
    public record AuthUser(long Id, string UserName, string Email, long TenantId);
}
