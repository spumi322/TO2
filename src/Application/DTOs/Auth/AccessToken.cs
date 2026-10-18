namespace Application.DTOs.Auth
{
    public record AccessToken(string Token, DateTime ExpiresAt);
}
