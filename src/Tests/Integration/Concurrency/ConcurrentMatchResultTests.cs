using Application.Contracts;
using Application.DTOs.Match;
using Application.DTOs.Team;
using Application.DTOs.Tournament;
using Application.Pipelines.MatchResult;
using Application.Pipelines.MatchResult.Contracts;
using Domain.Enums;
using Domain.Exceptions;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Helpers;
using Tests.Integration.Property;
using Match = Domain.AggregateRoots.Match;
using Tenant = Domain.AggregateRoots.Tenant;

namespace Tests.Integration.Concurrency
{
    /// <summary>
    /// Two match results saved at the same moment: each request checks "is the group / are all groups finished?"
    /// before the other one is saved. The later save must be a conflict, or nobody finishes the group stage.
    /// Request A is paused right before its save, request B runs completely in between.
    /// </summary>
    public class ConcurrentMatchResultTests
    {
        [Fact]
        public async Task LastTwoMatchesOfOneGroup_SecondSaveIsConflict_RetryFinishesTheTournament()
        {
            using var factory = new TestDbContextFactory();
            using var services = await StartGroupsOnlyAsync(factory, teams: 4, groups: 1);

            // Two matches without a shared team stay open, so the two results write different rows
            var matches = await OpenMatchesAsync(factory);
            var matchA = matches[0];
            var matchB = matches.First(m => m.TeamAId != matchA.TeamAId && m.TeamAId != matchA.TeamBId
                                         && m.TeamBId != matchA.TeamAId && m.TeamBId != matchA.TeamBId);
            foreach (var match in matches.Where(m => m.Id != matchA.Id && m.Id != matchB.Id))
                await ConfirmAsync(services, match);

            await AssertSecondSaveConflictsAndRetryFinishesAsync(factory, services, matchA, matchB);
        }

        [Fact]
        public async Task LastMatchesOfTwoGroups_SecondSaveIsConflict_RetryFinishesTheTournament()
        {
            using var factory = new TestDbContextFactory();
            using var services = await StartGroupsOnlyAsync(factory, teams: 8, groups: 2);

            // One match stays open in each group
            var matches = await OpenMatchesAsync(factory);
            var lastOfEachGroup = matches.GroupBy(m => m.StandingId).Select(g => g.First()).ToList();
            foreach (var match in matches.Where(m => lastOfEachGroup.All(last => last.Id != m.Id)))
                await ConfirmAsync(services, match);

            await AssertSecondSaveConflictsAndRetryFinishesAsync(factory, services, lastOfEachGroup[0], lastOfEachGroup[1]);
        }

        private static async Task AssertSecondSaveConflictsAndRetryFinishesAsync(
            TestDbContextFactory factory, ServiceProvider services, Match matchA, Match matchB)
        {
            // A has run all its steps (with the match of B still open) when B is saved; then A saves
            var saveA = () => ConfirmAsync(services, matchA, beforeSave: () => ConfirmAsync(services, matchB));

            await saveA.Should().ThrowAsync<ConflictException>();
            using (var db = factory.CreateContext())
            {
                (await db.Matches.AsNoTracking().SingleAsync(m => m.Id == matchA.Id)).WinnerId.Should().BeNull();
                (await db.Matches.AsNoTracking().SingleAsync(m => m.Id == matchB.Id)).WinnerId.Should().NotBeNull();
                (await db.Tournaments.AsNoTracking().SingleAsync()).Status.Should().Be(TournamentStatus.GroupsInProgress);
            }

            // The admin confirms again
            await ConfirmAsync(services, matchA);

            using (var db = factory.CreateContext())
            {
                (await db.Standings.AsNoTracking().Where(s => s.StandingType == StandingType.Group).ToListAsync())
                    .Should().OnlyContain(s => s.IsFinished);
                (await db.Tournaments.AsNoTracking().SingleAsync()).Status.Should().Be(TournamentStatus.Finished);
            }
        }

        private static async Task<ServiceProvider> StartGroupsOnlyAsync(TestDbContextFactory factory, int teams, int groups)
        {
            using (var db = factory.CreateContext())
            {
                db.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
                await db.SaveChangesAsync();
            }
            var services = TournamentRunner.BuildServices(factory, new Random(1));

            long tournamentId;
            using (var scope = services.CreateScope())
                tournamentId = (await scope.ServiceProvider.GetRequiredService<ITournamentService>().CreateTournamentAsync(
                    new CreateTournamentRequestDTO("Cup", null, teams, Format.GroupsOnly, groups, null))).Id;
            using (var scope = services.CreateScope())
                await scope.ServiceProvider.GetRequiredService<ITeamService>().AddTeamsToTournamentAsync(
                    tournamentId, new AddTeamsRequestDTO(Enumerable.Range(1, teams).Select(i => $"T{i:00}").ToList()));
            using (var scope = services.CreateScope())
                await scope.ServiceProvider.GetRequiredService<IWorkFlowService>().StartGroups(tournamentId);

            return services;
        }

        private static async Task<List<Match>> OpenMatchesAsync(TestDbContextFactory factory)
        {
            using var db = factory.CreateContext();
            return await db.Matches.AsNoTracking().Where(m => m.WinnerId == null).OrderBy(m => m.Id).ToListAsync();
        }

        /// <summary>One request (own scope, own DbContext): team A wins 2-0. With beforeSave, the request pauses before its save.</summary>
        private static async Task ConfirmAsync(ServiceProvider services, Match match, Func<Task>? beforeSave = null)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;
            var tournamentId = await provider.GetRequiredService<TO2DbContext>().Tournaments
                .AsNoTracking().Select(t => t.Id).SingleAsync();

            IMatchResultPipeline pipeline = beforeSave == null
                ? provider.GetRequiredService<IMatchResultPipeline>()
                : ActivatorUtilities.CreateInstance<MatchResultPipeline>(
                    provider, new PausedUnitOfWork(provider.GetRequiredService<IUnitOfWork>(), beforeSave));

            await pipeline.ExecuteAsync(new SetMatchResultDTO(match.Id, match.StandingId, tournamentId, 2, 0, match.Version));
        }

        /// <summary>
        /// Runs a callback right before the save. The SQLite test database has one connection, so two requests cannot
        /// both hold a transaction: the paused request saves without one (its single save is atomic anyway).
        /// The save itself and its version check are the real ones.
        /// </summary>
        private sealed class PausedUnitOfWork : IUnitOfWork
        {
            private readonly IUnitOfWork _inner;
            private readonly Func<Task> _beforeSave;

            public PausedUnitOfWork(IUnitOfWork inner, Func<Task> beforeSave)
            {
                _inner = inner;
                _beforeSave = beforeSave;
            }

            public bool HasActiveTransaction => false;
            public Task BeginTransactionAsync() => Task.CompletedTask;
            public Task RollbackTransactionAsync() => Task.CompletedTask;
            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => _inner.SaveChangesAsync(cancellationToken);

            public async Task CommitTransactionAsync()
            {
                await _beforeSave();
                await _inner.SaveChangesAsync();
            }
        }
    }
}
