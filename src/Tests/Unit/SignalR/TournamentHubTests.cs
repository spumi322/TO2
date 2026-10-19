using Application.Contracts;
using Domain.AggregateRoots;
using FluentAssertions;
using Infrastructure.Persistence.Repository;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tests.Base;
using Tests.Helpers;
using TO2.Hubs;

namespace Tests.Unit.SignalR
{
    /// <summary>
    /// TournamentHub.JoinTournament: only tournaments of the caller's tenant (tenant query filter).
    /// </summary>
    public class TournamentHubTests : IntegrationTestBase
    {
        private readonly Mock<IGroupManager> _groupsMock = new();
        private readonly TournamentHub _hub;

        public TournamentHubTests()
        {
            var tenantServiceMock = new Mock<ITenantService>();
            tenantServiceMock.Setup(x => x.GetCurrentTenantId()).Returns(1L);
            tenantServiceMock.Setup(x => x.GetCurrentUserName()).Returns("TestUser");

            var contextMock = new Mock<HubCallerContext>();
            contextMock.Setup(x => x.ConnectionId).Returns("connection-1");

            _hub = new TournamentHub(
                tenantServiceMock.Object,
                new Repository<Tournament>(DbContext),
                NullLogger<TournamentHub>.Instance)
            {
                Context = contextMock.Object,
                Groups = _groupsMock.Object
            };
        }

        private async Task<(long OwnId, long OtherTenantId)> SeedTournamentsAsync()
        {
            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.Tenants.Add(new Tenant { Id = 2, Name = "Org2" });
            var own = TestDataBuilder.Tournaments.CreateDefault("Own");
            var other = TestDataBuilder.Tournaments.CreateDefault("Other");
            other.TenantId = 2;
            DbContext.Tournaments.AddRange(own, other);
            await SaveAndDetachAsync();
            return (own.Id, other.Id);
        }

        [Fact]
        public async Task JoinTournament_AddsToGroup_When_TournamentInOwnTenant()
        {
            var (ownId, _) = await SeedTournamentsAsync();

            await _hub.JoinTournament(ownId);

            _groupsMock.Verify(x => x.AddToGroupAsync("connection-1", $"tournament-{ownId}", default), Times.Once);
        }

        [Fact]
        public async Task JoinTournament_Throws_AndDoesNotJoin_When_TournamentInOtherTenant()
        {
            var (_, otherId) = await SeedTournamentsAsync();

            var act = () => _hub.JoinTournament(otherId);

            await act.Should().ThrowAsync<HubException>();
            _groupsMock.Verify(x => x.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
