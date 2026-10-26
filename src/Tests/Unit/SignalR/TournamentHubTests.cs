using Application.Contracts;
using Application.DTOs.SignalR;
using Application.Services;
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
    /// TournamentHub: joins and drafts only for tournaments of the caller's tenant (tenant query filter);
    /// drafts are kept in the draft store and sent to pages that join later.
    /// </summary>
    public class TournamentHubTests : IntegrationTestBase
    {
        private readonly Mock<IGroupManager> _groupsMock = new();
        private readonly MatchDraftStore _draftStore = new(TimeProvider.System);
        private readonly Mock<ISingleClientProxy> _callerMock = new();
        private readonly Mock<IClientProxy> _othersMock = new();
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
                _draftStore,
                NullLogger<TournamentHub>.Instance)
            {
                Context = contextMock.Object,
                Groups = _groupsMock.Object
            };
        }

        private void MockClients(long tournamentId)
        {
            var clients = new Mock<IHubCallerClients>();
            clients.Setup(x => x.Caller).Returns(_callerMock.Object);
            clients.Setup(x => x.OthersInGroup($"tournament-{tournamentId}")).Returns(_othersMock.Object);
            _hub.Clients = clients.Object;
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

        [Fact]
        public async Task ShareMatchDraft_RelaysToOthersInTournament_When_TournamentInOwnTenant()
        {
            var (ownId, _) = await SeedTournamentsAsync();
            MockClients(ownId);

            await _hub.ShareMatchDraft(ownId, 42, 1, 0);

            var draft = new MatchDraftUpdatedEvent(ownId, 42, 1, 0, "TestUser");
            _othersMock.Verify(x => x.SendCoreAsync(
                "MatchDraftUpdated",
                It.Is<object?[]>(args => args.Length == 1 && args[0]!.Equals(draft)),
                default), Times.Once);
            _draftStore.GetAll(ownId).Should().Equal(draft);
        }

        [Fact]
        public async Task JoinTournament_SendsStoredDraftsToTheJoiningPage()
        {
            var (ownId, _) = await SeedTournamentsAsync();
            MockClients(ownId);
            await _hub.ShareMatchDraft(ownId, 42, 1, 0);

            await _hub.JoinTournament(ownId);

            _callerMock.Verify(x => x.SendCoreAsync(
                "MatchDraftUpdated",
                It.Is<object?[]>(args => args.Length == 1 && args[0]!.Equals(new MatchDraftUpdatedEvent(ownId, 42, 1, 0, "TestUser"))),
                default), Times.Once);
        }

        [Fact]
        public async Task ShareMatchDraft_Throws_AndRelaysNothing_When_TournamentInOtherTenant()
        {
            var (_, otherId) = await SeedTournamentsAsync();
            MockClients(otherId);

            var act = () => _hub.ShareMatchDraft(otherId, 42, 1, 0);

            await act.Should().ThrowAsync<HubException>();
            _othersMock.Verify(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
            _draftStore.GetAll(otherId).Should().BeEmpty();
        }
    }
}
