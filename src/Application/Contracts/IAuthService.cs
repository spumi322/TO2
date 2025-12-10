using Application.DTOs.Auth;

namespace Application.Contracts
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task RevokeTokenAsync(string refreshToken);
        Task<bool> ValidateRefreshTokenAsync(string refreshToken);
    }
}
