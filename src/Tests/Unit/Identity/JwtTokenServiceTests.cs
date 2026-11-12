using Application.DTOs.Auth;
using Domain.AggregateRoots;
using FluentAssertions;
using Infrastructure.Identity;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

namespace Tests.Unit.Identity
{
    /// <summary>
    /// JwtTokenService: claims the API and SignalR rely on, issuer/audience, expiry.
    /// </summary>
    public class JwtTokenServiceTests
    {
        private readonly JwtTokenService _service = new(Options.Create(new JwtSettings
        {
            SecretKey = "test-secret-key-that-is-long-enough-for-hmac-sha256",
            Issuer = "TO2-issuer",
            Audience = "TO2-audience",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        }));

        [Fact]
        public void CreateAccessToken_ContainsUserAndTenantClaims()
        {
            var user = new AuthUser(5, "ann", "ann@test.com", 3);
            var tenant = new Tenant { Id = 3, Name = "Org1" };

            var accessToken = _service.CreateAccessToken(user, tenant);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);
            var claims = jwt.Claims.ToDictionary(c => c.Type, c => c.Value);
            claims["sub"].Should().Be("5");
            claims["email"].Should().Be("ann@test.com");
            claims["name"].Should().Be("ann");
            claims["tenantId"].Should().Be("3");
            claims["tenantName"].Should().Be("Org1");
            claims.Should().ContainKey("jti");
            jwt.Issuer.Should().Be("TO2-issuer");
            jwt.Audiences.Should().Equal("TO2-audience");
            accessToken.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void CreateRefreshToken_IsRandomAndExpiresAfterConfiguredDays()
        {
            var first = _service.CreateRefreshToken(5);
            var second = _service.CreateRefreshToken(5);

            first.Token.Should().NotBe(second.Token);
            first.Entity.UserId.Should().Be(5);
            first.Entity.Expires.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
            first.Entity.IsActive.Should().BeTrue();
        }

        [Fact]
        public void CreateRefreshToken_EntityHoldsHashNotValue()
        {
            var issued = _service.CreateRefreshToken(5);

            issued.Entity.TokenHash.Should().NotBe(issued.Token);
            issued.Entity.TokenHash.Should().Be(_service.HashRefreshToken(issued.Token));
            issued.Entity.TokenHash.Should().HaveLength(64);
        }
    }
}
