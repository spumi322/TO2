using Application.Contracts;
using Application.DTOs.Standing;
using Application.DTOs.Tournament;
using Domain.AggregateRoots;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class StartTournamentService : IStartTournamentService
    {
        private readonly IMatchService _matchService;
        private readonly IGameService _gameService;
        private readonly IGenericRepository<Tournament> _tournamentRepository;
        private readonly ILogger<StartTournamentService> _logger;

        public StartTournamentService(
            IMatchService matchService,
            IGameService gameService,
            IGenericRepository<Tournament> tournamentRepository,
            ILogger<StartTournamentService> logger)
        {
            _matchService = matchService;
            _gameService = gameService;
            _tournamentRepository = tournamentRepository;
            _logger = logger;
        }

        public async Task<TournamentSeedResult> InitializeTournamentAsync(long tournamentId)
        {
            var createdStandingIds = new List<long>();

            try
            {
                // Get tournament to determine format
                var tournament = await _tournamentRepository.Get(tournamentId);
                if (tournament == null)
                {
                    return new TournamentSeedResult(
                        Success: false,
                        CreatedStandingsId: createdStandingIds,
                        ErrorMessage: "Tournament not found",
                        FailedStep: "Validation"
                    );
                }

                _logger.LogInformation(
                    "Initializing tournament {TournamentId} with format {Format}",
                    tournamentId,
                    tournament.Format
                );

                // Step 1: Seed matches based on format
                var seedResult = tournament.Format switch
                {
                    Format.BracketOnly => await _matchService.SeedBracketOnly(tournamentId),
                    Format.BracketAndGroup => await _matchService.SeedGroups(tournamentId),
                    _ => new SeedGroupsResponseDTO(
                        $"Unknown tournament format: {tournament.Format}",
                        false,
                        new List<long>()
                    )
                };

                if (!seedResult.Success)
                {
                    return new TournamentSeedResult(
                        Success: false,
                        CreatedStandingsId: createdStandingIds,
                        ErrorMessage: seedResult.Response,
                        FailedStep: "Seeding"
                    );
                }

                createdStandingIds.AddRange(seedResult.StandingId);

                // Step 2: Generate games for all seeded standings
                _logger.LogInformation(
                    "Generating games for {Count} standings",
                    seedResult.StandingId.Count
                );

                foreach (var standingId in seedResult.StandingId)
                {
                    var matches = await _matchService.GetMatchesAsync(standingId);

                    foreach (var match in matches)
                    {
                        await _gameService.GenerateGames(match.Id);
                    }
                }

                _logger.LogInformation("Tournament initialization completed successfully");

                return new TournamentSeedResult(
                    Success: true,
                    CreatedStandingsId: createdStandingIds,
                    ErrorMessage: string.Empty,
                    FailedStep: null
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing tournament {TournamentId}", tournamentId);
                return new TournamentSeedResult(
                    Success: false,
                    CreatedStandingsId: createdStandingIds,
                    ErrorMessage: ex.Message,
                    FailedStep: "Exception"
                );
            }
        }
    }
}
