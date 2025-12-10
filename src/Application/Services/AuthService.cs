using Application.Configurations;
using Application.Contracts;
using Application.DTOs.Auth;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly IRepository<RefreshToken> _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<User> userManager,
            IRepository<Tenant> tenantRepository,
            IRepository<RefreshToken> refreshTokenRepository,
            IUnitOfWork unitOfWork,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _tenantRepository = tenantRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                throw new ConflictException("User with this email already exists");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Create tenant
                var tenant = new Tenant
                {
                    Name = request.TenantName,
                    IsActive = true
                };
                await _tenantRepository.AddAsync(tenant);
                await _unitOfWork.SaveChangesAsync();

                // Create user
                var user = new User
                {
                    UserName = request.UserName,
                    Email = request.Email,
                    TenantId = tenant.Id,
                    EmailConfirmed = true // Auto-confirm for development
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new ValidationException($"User creation failed: {errors}");
                }

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("User {Email} registered successfully with tenant {TenantId}",
                    request.Email, tenant.Id);

                // Generate tokens and return response
                return await GenerateAuthResponseAsync(user, tenant);
            }
            catch
            {
                if (_unitOfWork.HasActiveTransaction)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }
                throw;
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                throw new UnauthorizedException("Invalid email or password");

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
                throw new UnauthorizedException("Invalid email or password");

            var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);
            if (tenant == null)
                throw new NotFoundException("Tenant", user.TenantId);

            if (!tenant.IsActive)
                throw new UnauthorizedException("Tenant account is inactive");

            _logger.LogInformation("User {Email} logged in successfully", request.Email);

            return await GenerateAuthResponseAsync(user, tenant);
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var refreshToken = await _refreshTokenRepository
                .FindAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken == null || !refreshToken.IsActive)
                throw new UnauthorizedException("Invalid or expired refresh token");

            var user = await _userManager.FindByIdAsync(refreshToken.UserId.ToString());
            if (user == null)
                throw new NotFoundException("User", refreshToken.UserId);

            var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);
            if (tenant == null)
                throw new NotFoundException("Tenant", user.TenantId);

            // Revoke old refresh token
            refreshToken.Revoked = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(refreshToken);

            // Generate new tokens
            var response = await GenerateAuthResponseAsync(user, tenant);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Refresh token used for user {UserId}", user.Id);

            return response;
        }

        public async Task RevokeTokenAsync(string refreshToken)
        {
            var token = await _refreshTokenRepository
                .FindAsync(rt => rt.Token == refreshToken);

            if (token == null)
                throw new UnauthorizedException("Invalid refresh token");

            if (!token.IsActive)
                throw new ValidationException("Token is already revoked");

            token.Revoked = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(token);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Refresh token revoked for user {UserId}", token.UserId);
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {
            var token = await _refreshTokenRepository
                .FindAsync(rt => rt.Token == refreshToken);

            return token != null && token.IsActive;
        }

        // Private helper methods

        private async Task<AuthResponse> GenerateAuthResponseAsync(User user, Tenant tenant)
        {
            var accessToken = GenerateAccessToken(user, tenant);
            var refreshToken = await GenerateRefreshTokenAsync(user.Id);

            return new AuthResponse(
                UserId: user.Id,
                UserName: user.UserName!,
                Email: user.Email!,
                TenantId: tenant.Id,
                TenantName: tenant.Name,
                AccessToken: accessToken,
                RefreshToken: refreshToken.Token,
                ExpiresAt: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            );
        }

        private string GenerateAccessToken(User user, Tenant tenant)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Name, user.UserName!),
                new Claim("tenantId", tenant.Id.ToString()),
                new Claim("tenantName", tenant.Name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<RefreshToken> GenerateRefreshTokenAsync(long userId)
        {
            var refreshToken = new RefreshToken
            {
                Token = GenerateSecureRandomToken(),
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                UserId = userId
            };

            await _refreshTokenRepository.AddAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();

            return refreshToken;
        }

        private static string GenerateSecureRandomToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
