namespace Application.DTOs.Auth
{
    public record InviteCodeResponse(string Code, DateTime ExpiresAt);
}
