using Application.Contracts;
using Application.DTOs.Auth;
using Application.Services;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Tests.Base;

namespace Tests.Unit.Services
{
    /// <summary>
    /// AuthService flow (tenants, transaction, refresh tokens) with a mocked IIdentityService and the real JwtTokenService.
    /// </summary>
    public class AuthServiceTests : IntegrationTestBase
    {
        private const string Password = "Secret1";

        private readonly Mock<IIdentityService> _identityMock = new();
        private readonly JwtTokenService _tokenService;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _tokenService = new JwtTokenService(Options.Create(new JwtSettings
            {
                SecretKey = "test-secret-key-that-is-long-enough-for-hmac-sha256",
                Issuer = "TO2",
                Audience = "TO2"
            }));

            _service = new AuthService(
                _identityMock.Object,
                _tokenService,
                new Repository<Tenant>(DbContext),
                new Repository<RefreshToken>(DbContext),
                new UnitOfWork(DbContext),
                new Mock<ILogger<AuthService>>().Object);

            // Identity writes the user through the same DbContext; refresh tokens need the user row (FK)
            _identityMock
                .Setup(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                .Returns(async (string userName, string email, string _, long tenantId) =>
                {
                    var user = new User { UserName = userName, Email = email, TenantId = tenantId };
                    DbContext.Users.Add(user);
                    await DbContext.SaveChangesAsync();
                    return new AuthUser(user.Id, userName, email, tenantId);
                });
        }

        private async Task<Tenant> SeedTenantAsync(string name, bool isActive = true)
        {
            var tenant = new Tenant { Name = name, IsActive = isActive };
            DbContext.Tenants.Add(tenant);
            await DbContext.SaveChangesAsync();
            return tenant;
        }

        private async Task<AuthUser> SeedUserAsync(Tenant tenant, string email = "ann@test.com")
        {
            var user = new User { UserName = "ann", Email = email, TenantId = tenant.Id };
            DbContext.Users.Add(user);
            await DbContext.SaveChangesAsync();
            var authUser = new AuthUser(user.Id, "ann", email, tenant.Id);

            _identityMock.Setup(x => x.ValidateCredentialsAsync(email, Password)).ReturnsAsync(authUser);
            _identityMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(authUser);
            return authUser;
        }

        private string Hash(string token) => _tokenService.HashRefreshToken(token);

        private static RegisterRequest Register(string tenantName) => new("ann", "ann@test.com", Password, tenantName);

        [Fact]
        public async Task Register_CreatesTenant_When_NameIsNew()
        {
            var response = await _service.RegisterAsync(Register("  Org1  "));

            DbContext.ChangeTracker.Clear();
            var tenant = await DbContext.Tenants.SingleAsync();
            tenant.Name.Should().Be("Org1");
            response.TenantId.Should().Be(tenant.Id);
            response.AccessToken.Should().NotBeNullOrEmpty();
            (await DbContext.RefreshTokens.SingleAsync()).TokenHash.Should().Be(Hash(response.RefreshToken));
        }

        [Fact]
        public async Task Register_JoinsExistingTenant_When_NameExists()
        {
            var existing = await SeedTenantAsync("Org1");

            var response = await _service.RegisterAsync(Register("Org1"));

            response.TenantId.Should().Be(existing.Id);
            (await DbContext.Tenants.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task Register_ThrowsConflict_When_EmailExists()
        {
            _identityMock.Setup(x => x.EmailExistsAsync("ann@test.com")).ReturnsAsync(true);

            var act = () => _service.RegisterAsync(Register("Org1"));

            await act.Should().ThrowAsync<ConflictException>();
            (await DbContext.Tenants.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task Register_ThrowsValidation_AndCreatesNoUser_When_TenantInactive()
        {
            await SeedTenantAsync("Org1", isActive: false);

            var act = () => _service.RegisterAsync(Register("Org1"));

            await act.Should().ThrowAsync<ValidationException>();
            _identityMock.Verify(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task Register_RollsBackNewTenant_When_UserCreationFails()
        {
            _identityMock
                .Setup(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                .ThrowsAsync(new ValidationException("User creation failed: password too short"));

            var act = () => _service.RegisterAsync(Register("Org1"));

            await act.Should().ThrowAsync<ValidationException>();
            DbContext.ChangeTracker.Clear();
            (await DbContext.Tenants.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task Login_ReturnsTokens_When_CredentialsValid()
        {
            var tenant = await SeedTenantAsync("Org1");
            var user = await SeedUserAsync(tenant);

            var response = await _service.LoginAsync(new LoginRequest(user.Email, Password));

            response.UserId.Should().Be(user.Id);
            response.TenantName.Should().Be("Org1");
            (await DbContext.RefreshTokens.SingleAsync()).UserId.Should().Be(user.Id);
        }

        [Fact]
        public async Task Login_StoresOnlyTokenHash()
        {
            var tenant = await SeedTenantAsync("Org1");
            var user = await SeedUserAsync(tenant);

            var response = await _service.LoginAsync(new LoginRequest(user.Email, Password));

            var stored = await DbContext.RefreshTokens.SingleAsync();
            stored.TokenHash.Should().NotBe(response.RefreshToken);
            stored.TokenHash.Should().Be(Hash(response.RefreshToken));
        }

        [Fact]
        public async Task Login_ThrowsUnauthorized_When_CredentialsInvalid()
        {
            var act = () => _service.LoginAsync(new LoginRequest("ann@test.com", "wrong"));

            await act.Should().ThrowAsync<UnauthorizedException>();
        }

        [Fact]
        public async Task Login_ThrowsForbidden_When_TenantInactive()
        {
            var tenant = await SeedTenantAsync("Org1", isActive: false);
            var user = await SeedUserAsync(tenant);

            var act = () => _service.LoginAsync(new LoginRequest(user.Email, Password));

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Refresh_RevokesOldToken_AndIssuesNewOne()
        {
            var tenant = await SeedTenantAsync("Org1");
            var user = await SeedUserAsync(tenant);
            var login = await _service.LoginAsync(new LoginRequest(user.Email, Password));

            var refreshed = await _service.RefreshTokenAsync(new RefreshTokenRequest(login.RefreshToken));

            refreshed.RefreshToken.Should().NotBe(login.RefreshToken);
            DbContext.ChangeTracker.Clear();
            (await DbContext.RefreshTokens.SingleAsync(t => t.TokenHash == Hash(login.RefreshToken))).IsRevoked.Should().BeTrue();
            (await DbContext.RefreshTokens.SingleAsync(t => t.TokenHash == Hash(refreshed.RefreshToken))).IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Refresh_ThrowsUnauthorized_When_TokenRevoked()
        {
            var tenant = await SeedTenantAsync("Org1");
            var user = await SeedUserAsync(tenant);
            var login = await _service.LoginAsync(new LoginRequest(user.Email, Password));
            await _service.RevokeTokenAsync(login.RefreshToken);

            var act = () => _service.RefreshTokenAsync(new RefreshTokenRequest(login.RefreshToken));

            await act.Should().ThrowAsync<UnauthorizedException>();
        }
    }
}
