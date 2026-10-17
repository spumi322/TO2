using Domain.AggregateRoots;
using Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Base;
using Tests.Helpers;

namespace Tests.Unit.Persistence
{
    public class ConcurrencyVersionTests : IntegrationTestBase
    {
        public ConcurrencyVersionTests()
        {
            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.SaveChanges();
        }

        private async Task<long> SeedTournamentAsync()
        {
            var tournament = TestDataBuilder.Tournaments.CreateInStatus(TournamentStatus.Setup, name: "Summer Cup");
            DbContext.Tournaments.Add(tournament);
            await SaveAndDetachAsync();
            return tournament.Id;
        }

        [Fact]
        public async Task Should_StartAtZero_When_Inserted()
        {
            var id = await SeedTournamentAsync();

            (await DbContext.Tournaments.SingleAsync(t => t.Id == id)).Version.Should().Be(0);
        }

        [Fact]
        public async Task Should_IncrementVersion_On_EachUpdate()
        {
            var id = await SeedTournamentAsync();
            var tournament = await DbContext.Tournaments.SingleAsync(t => t.Id == id);

            tournament.Description = "First";
            await DbContext.SaveChangesAsync();
            tournament.Description = "Second";
            await DbContext.SaveChangesAsync();

            DbContext.ChangeTracker.Clear();
            (await DbContext.Tournaments.SingleAsync(t => t.Id == id)).Version.Should().Be(2);
        }

        [Fact]
        public async Task Should_NotIncrementVersion_When_NothingChanged()
        {
            var id = await SeedTournamentAsync();
            await DbContext.Tournaments.SingleAsync(t => t.Id == id);

            await DbContext.SaveChangesAsync();

            DbContext.ChangeTracker.Clear();
            (await DbContext.Tournaments.SingleAsync(t => t.Id == id)).Version.Should().Be(0);
        }

        [Fact]
        public async Task Should_ThrowConcurrencyException_When_RowChangedSinceLoad()
        {
            // Two requests load the same row; the second save must not overwrite the first
            var id = await SeedTournamentAsync();
            using var otherContext = DbFactory.CreateContext();

            var first = await DbContext.Tournaments.SingleAsync(t => t.Id == id);
            var second = await otherContext.Tournaments.SingleAsync(t => t.Id == id);

            first.Description = "From first";
            await DbContext.SaveChangesAsync();

            second.Description = "From second";
            var act = () => otherContext.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
            DbContext.ChangeTracker.Clear();
            (await DbContext.Tournaments.SingleAsync(t => t.Id == id)).Description.Should().Be("From first");
        }

        [Fact]
        public async Task Should_ThrowConcurrencyException_When_DeletingRowChangedSinceLoad()
        {
            var id = await SeedTournamentAsync();
            using var otherContext = DbFactory.CreateContext();

            var first = await DbContext.Tournaments.SingleAsync(t => t.Id == id);
            var second = await otherContext.Tournaments.SingleAsync(t => t.Id == id);

            first.Description = "Changed";
            await DbContext.SaveChangesAsync();

            otherContext.Tournaments.Remove(second);
            var act = () => otherContext.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        }
    }
}
