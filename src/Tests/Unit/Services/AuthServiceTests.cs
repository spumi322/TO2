using Application.Contracts;
using Application.DTOs.Auth;
using Application.Utilities;
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
        private const string Code = "ABCD-EFGH-JKLM-NPQR-STUV-WXYZ";

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

        private async Task<Tenant> SeedTenantAsync(string name, bool isActive = true, string? inviteCode = null, DateTime? codeExpiresAt = null)
        {
            var tenant = new Tenant { Name = name, IsActive = isActive };
            if (inviteCode != null)
                tenant.SetInviteCode(InviteCode.Hash(inviteCode), codeExpiresAt ?? DateTime.UtcNow.AddHours(1));
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

        private static RegisterRequest Register(string tenantName, string? inviteCode = null, string email = "ann@test.com") =>
            new("ann", email, Password, tenantName, inviteCode);

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
        public async Task Register_JoinsExistingTenant_When_InviteCodeValid()
        {
            var existing = await SeedTenantAsync("Org1", inviteCode: Code);

            // Code typed without dashes and in lowercase still matches
            var response = await _service.RegisterAsync(Register("Org1", Code.Replace("-", "").ToLowerInvariant()));

            response.TenantId.Should().Be(existing.Id);
            (await DbContext.Tenants.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task Register_InviteCodeIsReusable_UntilItExpires()
        {
            var existing = await SeedTenantAsync("Org1", inviteCode: Code);

            var first = await _service.RegisterAsync(Register("Org1", Code, "ann@test.com"));
            var second = await _service.RegisterAsync(Register("Org1", Code, "bob@test.com"));

            first.TenantId.Should().Be(existing.Id);
            second.TenantId.Should().Be(existing.Id);
        }

        [Theory]
        [InlineData("Org1")]
        [InlineData("  org1 ")]
        public async Task Register_ThrowsConflict_When_NameExistsAndNoInviteCode(string typedName)
        {
            await SeedTenantAsync("Org1", inviteCode: Code);

            var act = () => _service.RegisterAsync(Register(typedName));

            await act.Should().ThrowAsync<ConflictException>().WithMessage("*invite code*");
            DbContext.ChangeTracker.Clear();
            (await DbContext.Tenants.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task Register_ThrowsValidation_When_InviteCodeWrong()
        {
            await SeedTenantAsync("Org1", inviteCode: Code);

            var act = () => _service.RegisterAsync(Register("Org1", "ZZZZ-ZZZZ-ZZZZ-ZZZZ-ZZZZ-ZZZZ"));

            await act.Should().ThrowAsync<ValidationException>().WithMessage("Invalid or expired invite code.");
        }

        [Fact]
        public async Task Register_ThrowsValidation_When_InviteCodeExpired()
        {
            await SeedTenantAsync("Org1", inviteCode: Code, codeExpiresAt: DateTime.UtcNow.AddMinutes(-1));

            var act = () => _service.RegisterAsync(Register("Org1", Code));

            await act.Should().ThrowAsync<ValidationException>().WithMessage("Invalid or expired invite code.");
        }

        [Fact]
        public async Task Register_ThrowsValidation_When_InviteCodeBelongsToOtherOrganization()
        {
            await SeedTenantAsync("Org1", inviteCode: Code);
            await SeedTenantAsync("Org2", inviteCode: "QQQQ-QQQQ-QQQQ-QQQQ-QQQQ-QQQQ");

            var act = () => _service.RegisterAsync(Register("Org2", Code));

            await act.Should().ThrowAsync<ValidationException>().WithMessage("Invalid or expired invite code.");
        }

        [Fact]
        public async Task Register_ThrowsValidation_AndCreatesNoTenant_When_InviteCodeForUnknownOrganization()
        {
            var act = () => _service.RegisterAsync(Register("Org1", Code));

            await act.Should().ThrowAsync<ValidationException>().WithMessage("Organization not found*");
            DbContext.ChangeTracker.Clear();
            (await DbContext.Tenants.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task OrganizationExists_IgnoresCaseAndSpaces()
        {
            await SeedTenantAsync("Org1");

            (await _service.OrganizationExistsAsync(" ORG1 ")).Exists.Should().BeTrue();
            (await _service.OrganizationExistsAsync("Org2")).Exists.Should().BeFalse();
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
            await SeedTenantAsync("Org1", isActive: false, inviteCode: Code);

            var act = () => _service.RegisterAsync(Register("Org1", Code));

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
