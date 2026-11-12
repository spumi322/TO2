using Application.Contracts;
using Application.Services;
using Application.Utilities;
using Domain.AggregateRoots;
using Domain.Exceptions;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Base;

namespace Tests.Unit.Services
{
    /// <summary>
    /// TenantInviteService: one hashed, day-long invite code per tenant; tenant name uniqueness.
    /// </summary>
    public class TenantInviteServiceTests : IntegrationTestBase
    {
        private readonly TenantInviteService _service;

        public TenantInviteServiceTests()
        {
            var tenantServiceMock = new Mock<ITenantService>();
            tenantServiceMock.Setup(x => x.GetCurrentTenantId()).Returns(1L);
            tenantServiceMock.Setup(x => x.GetCurrentUserName()).Returns("TestUser");

            _service = new TenantInviteService(
                new Repository<Tenant>(DbContext),
                tenantServiceMock.Object,
                new UnitOfWork(DbContext),
                new Mock<ILogger<TenantInviteService>>().Object);
        }

        private async Task SeedTenantAsync(bool isActive = true)
        {
            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1", IsActive = isActive });
            await DbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task CreateInviteCode_StoresOnlyHash_AndExpiresInOneDay()
        {
            await SeedTenantAsync();

            var response = await _service.CreateInviteCodeAsync();

            response.Code.Should().MatchRegex("^([A-Z2-9]{4}-){5}[A-Z2-9]{4}$");
            response.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(1), TimeSpan.FromSeconds(5));
            DbContext.ChangeTracker.Clear();
            var tenant = await DbContext.Tenants.SingleAsync();
            tenant.InviteCodeHash.Should().Be(InviteCode.Hash(response.Code));
            tenant.InviteCodeHash.Should().NotContain(InviteCode.Normalize(response.Code));
            tenant.IsInviteCodeValid(InviteCode.Hash(response.Code), DateTime.UtcNow).Should().BeTrue();
        }

        [Fact]
        public async Task CreateInviteCode_ReplacesPreviousCode()
        {
            await SeedTenantAsync();
            var first = await _service.CreateInviteCodeAsync();

            var second = await _service.CreateInviteCodeAsync();

            DbContext.ChangeTracker.Clear();
            var tenant = await DbContext.Tenants.SingleAsync();
            tenant.IsInviteCodeValid(InviteCode.Hash(first.Code), DateTime.UtcNow).Should().BeFalse();
            tenant.IsInviteCodeValid(InviteCode.Hash(second.Code), DateTime.UtcNow).Should().BeTrue();
        }

        [Fact]
        public async Task CreateInviteCode_ThrowsForbidden_When_TenantInactive()
        {
            await SeedTenantAsync(isActive: false);

            var act = () => _service.CreateInviteCodeAsync();

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task TenantNames_AreUniqueIgnoringCaseAndSpaces()
        {
            await SeedTenantAsync();
            DbContext.Tenants.Add(new Tenant { Name = " ORG1 " });

            var act = () => DbContext.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }
}
