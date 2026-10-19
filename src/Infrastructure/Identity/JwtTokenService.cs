using Application.Contracts;
using Application.DTOs.Auth;
using Application.Utilities;
using Domain.AggregateRoots;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Identity
{
    public class JwtTokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;

        public JwtTokenService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public AccessToken CreateAccessToken(AuthUser user, Tenant tenant)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.UserName),
                new Claim("tenantId", tenant.Id.ToString()),
                new Claim("tenantName", tenant.Name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        public IssuedRefreshToken CreateRefreshToken(long userId)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            return new IssuedRefreshToken(token, new RefreshToken
            {
                TokenHash = HashRefreshToken(token),
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                UserId = userId
            });
        }

        public string HashRefreshToken(string token)
        {
            return SecretHasher.Sha256Hex(token);
        }
    }
}
