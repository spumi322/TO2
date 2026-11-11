using Application.Contracts;
using Application.Contracts.Repositories;
using Application.DTOs.Match;
using Application.DTOs.Team;
using Application.DTOs.Tournament;
using Application.Pipelines.MatchResult;
using Application.Pipelines.MatchResult.Contracts;
using Application.Pipelines.MatchResult.Strategies;
using Application.Pipelines.StartBracket;
using Application.Pipelines.StartBracket.Contracts;
using Application.Pipelines.StartBracket.Strategies;
using Application.Pipelines.StartGroups;
using Application.Pipelines.StartGroups.Contracts;
using Application.Services;
using Domain.AggregateRoots;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.StateMachine;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Match = Domain.AggregateRoots.Match;
using Tests.Helpers;

namespace Tests.Integration.Property
{
    /// <summary>One generated tournament. Outcomes are cycled: bit 0 = winner (0 = team A), bit 1 = loser took a game.</summary>
    public record TournamentCase(Format Format, int MaxTeams, int Teams, int? Groups, int? Advancing, int DrawSeed, int[] Outcomes)
    {
        public override string ToString() =>
            $"{Format} max {MaxTeams}, {Teams} teams, groups {Groups?.ToString() ?? "-"}, advancing {Advancing?.ToString() ?? "-"}, " +
            $"draw seed {DrawSeed}, outcomes [{string.Join(",", Outcomes)}]";
    }

    /// <summary>Final DB rows of a played tournament.</summary>
    public record FinishedRun(Tournament Tournament, List<Standing> Standings, List<Match> Matches, List<GroupEntry> Entries, List<Team> Teams);

    /// <summary>
    /// Plays a tournament through the real services and pipelines on SQLite, like docs/tools/simulate_tournament.py:
    /// create, add teams, start groups, play open matches, start bracket, play until Finished.
    /// Checks after every request that no row's Version went down.
    /// </summary>
    public static class TournamentRunner
    {
        private const int MaxRequests = 1000;

        public static async Task<FinishedRun> PlayAsync(TournamentCase c)
        {
            using var factory = new TestDbContextFactory();
            using (var db = factory.CreateContext())
            {
                db.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
                await db.SaveChangesAsync();
            }
            using var services = BuildServices(factory, new Random(c.DrawSeed));

            long tournamentId = 0;
            await Request(services, async sp => tournamentId = (await sp.GetRequiredService<ITournamentService>()
                .CreateTournamentAsync(new CreateTournamentRequestDTO("Prop Cup", null, c.MaxTeams, c.Format, c.Groups, c.Advancing))).Id);
            await Request(services, sp => sp.GetRequiredService<ITeamService>()
                .AddTeamsToTournamentAsync(tournamentId, new AddTeamsRequestDTO(Enumerable.Range(1, c.Teams).Select(i => $"T{i:00}").ToList())));

            var versions = await ReadVersions(factory);
            int played = 0;
            for (int request = 0; request < MaxRequests; request++)
            {
                using var db = factory.CreateContext();
                var tournament = await db.Tournaments.AsNoTracking().SingleAsync();
                switch (tournament.Status)
                {
                    case TournamentStatus.Finished:
                        return await ReadFinished(db);
                    case TournamentStatus.Setup when c.Format != Format.BracketOnly:
                        await Request(services, sp => sp.GetRequiredService<IWorkFlowService>().StartGroups(tournamentId));
                        break;
                    case TournamentStatus.Setup:
                    case TournamentStatus.GroupsCompleted:
                        await Request(services, sp => sp.GetRequiredService<IWorkFlowService>().StartBracket(tournamentId));
                        break;
                    case TournamentStatus.GroupsInProgress:
                    case TournamentStatus.BracketInProgress:
                        var match = await db.Matches.AsNoTracking()
                            .Where(m => m.TeamAId > 0 && m.TeamBId > 0 && (m.WinnerId == null || m.WinnerId == 0))
                            .OrderBy(m => m.Id)
                            .FirstOrDefaultAsync()
                            ?? throw new Exception($"Stalled: no open match in status {tournament.Status}");
                        var outcome = c.Outcomes[played++ % c.Outcomes.Length];
                        int loserGames = outcome >> 1;
                        var (aWins, bWins) = (outcome & 1) == 0 ? (2, loserGames) : (loserGames, 2);
                        await Request(services, sp => sp.GetRequiredService<IMatchResultPipeline>().ExecuteAsync(
                            new SetMatchResultDTO(match.Id, match.StandingId, tournamentId, aWins, bWins, match.Version)));
                        break;
                    default:
                        throw new Exception($"Stalled in status {tournament.Status}");
                }

                var next = await ReadVersions(factory);
                var lowered = versions.Where(v => next.TryGetValue(v.Key, out var now) && now < v.Value).Select(v => v.Key).ToList();
                if (lowered.Count > 0)
                    throw new Exception($"Version went down after request {request}: {string.Join(", ", lowered)}");
                versions = next;
            }
            throw new Exception($"Not finished after {MaxRequests} requests");
        }

        /// <summary>One API request: a fresh DI scope, so a fresh DbContext.</summary>
        private static async Task Request(ServiceProvider services, Func<IServiceProvider, Task> action)
        {
            using var scope = services.CreateScope();
            await action(scope.ServiceProvider);
        }

        private static async Task<FinishedRun> ReadFinished(TO2DbContext db) => new(
            await db.Tournaments.AsNoTracking().SingleAsync(),
            await db.Standings.AsNoTracking().ToListAsync(),
            await db.Matches.AsNoTracking().ToListAsync(),
            await db.GroupEntries.AsNoTracking().ToListAsync(),
            await db.Teams.AsNoTracking().ToListAsync());

        private static async Task<Dictionary<string, long>> ReadVersions(TestDbContextFactory factory)
        {
            using var db = factory.CreateContext();
            var versions = new Dictionary<string, long>();
            async Task Add<T>(IQueryable<T> rows, string type) where T : EntityBase
            {
                foreach (var row in await rows.AsNoTracking().Select(r => new { r.Id, r.Version }).ToListAsync())
                    versions[$"{type} {row.Id}"] = row.Version;
            }
            await Add(db.Tournaments, "Tournament");
            await Add(db.Teams, "Team");
            await Add(db.Standings, "Standing");
            await Add(db.Matches, "Match");
            await Add(db.Games, "Game");
            await Add(db.GroupEntries, "GroupEntry");
            return versions;
        }

        /// <summary>The registrations of Program.cs that playing a tournament needs, with a test DbContext, tenant and seeded Random.</summary>
        public static ServiceProvider BuildServices(TestDbContextFactory factory, Random random)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddScoped(_ => factory.CreateContext());
            services.AddSingleton(factory.TenantService);
            services.AddSingleton(new Mock<ISignalRService>().Object);
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton(random);
            services.AddSingleton<IMatchDraftStore, MatchDraftStore>();

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ITournamentRepository, TournamentRepository>();
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<IStandingRepository, StandingRepository>();

            services.AddScoped<ITournamentStateMachine, TournamentStateMachine>();
            services.AddScoped<IFormatService, FormatService>();
            services.AddScoped<ITournamentService, TournamentService>();
            services.AddScoped<IStandingService, StandingService>();
            services.AddScoped<IPlacementService, PlacementService>();
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<IMatchService, MatchService>();
            services.AddScoped<IGameService, GameService>();
            services.AddScoped<IWorkFlowService, WorkFlowService>();

            services.AddScoped<IMatchResultPipeline, MatchResultPipeline>();
            foreach (var step in MatchResultPipeline.StepOrder)
                services.AddScoped(typeof(IMatchResultPipelineStep), step);
            services.AddScoped<IStandingProgressStrategy, GroupProgressStrategy>();
            services.AddScoped<IStandingProgressStrategy, BracketProgressStrategy>();
            services.AddScoped<IStartGroupsPipeline, StartGroupsPipeline>();
            foreach (var step in StartGroupsPipeline.StepOrder)
                services.AddScoped(typeof(IStartGroupsPipelineStep), step);
            services.AddScoped<IStartBracketPipeline, StartBracketPipeline>();
            foreach (var step in StartBracketPipeline.StepOrder)
                services.AddScoped(typeof(IStartBracketPipelineStep), step);
            services.AddScoped<IBracketSeedingStrategy, RandomSeedingStrategy>();
            services.AddScoped<IBracketSeedingStrategy, GroupResultSeedingStrategy>();

            return services.BuildServiceProvider();
        }
    }
}
