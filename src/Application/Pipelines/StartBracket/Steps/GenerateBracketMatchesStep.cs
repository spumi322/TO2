using Application.Contracts;
using Application.Pipelines.StartBracket.Contracts;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 6: Generates all bracket matches with games.
    /// Round 1 matches use actual teams from seeding.
    /// Round 2+ matches use TBD (null) teams to be filled by winners.
    /// </summary>
    public class GenerateBracketMatchesStep : IStartBracketPipelineStep
    {
        private readonly ILogger<GenerateBracketMatchesStep> _logger;
        private readonly IMatchService _matchService;
        private readonly IGameService _gameService;

        public GenerateBracketMatchesStep(
            ILogger<GenerateBracketMatchesStep> logger,
            IMatchService matchService,
            IGameService gameService)
        {
            _logger = logger;
            _matchService = matchService;
            _gameService = gameService;
        }

        public async Task<bool> ExecuteAsync(StartBracketContext context)
        {
            _logger.LogInformation("Step 6: Generating bracket matches for tournament {TournamentId}",
                context.TournamentId);

            int totalRounds = context.TotalRounds;
            int matchesGenerated = 0;

            try
            {
                for (int round = 1; round <= totalRounds; round++)
                {
                    int matchesInRound = (int)Math.Pow(2, totalRounds - round);

                    for (int seed = 1; seed <= matchesInRound; seed++)
                    {
                        if (round == 1)
                        {
                            // Round 1: Use actual teams from seeding
                            var pairIndex = seed - 1;
                            var (teamA, teamB) = context.SeededPairs[pairIndex];

                            var match = await _matchService.GenerateMatch(teamA, teamB, round, seed, context.BracketStanding.Id);
                            var gameResponse = await _gameService.GenerateGames(match);

                            if (!gameResponse.Success)
                            {
                                context.Success = false;
                                context.Message = $"Failed to generate games for match: {gameResponse.Message}";
                                _logger.LogError("Failed to generate games for R{Round} Match {Seed}: {Message}",
                                    round, seed, gameResponse.Message);
                                return false;
                            }

                            matchesGenerated++;
                            _logger.LogInformation("R{Round} Match {Seed}: {TeamA} vs {TeamB}",
                                round, seed, teamA.Name, teamB.Name);
                        }
                        else
                        {
                            // Round 2+: TBD teams (null teams)
                            var match = await _matchService.GenerateMatch(null, null, round, seed, context.BracketStanding.Id);
                            var gameResponse = await _gameService.GenerateGames(match);

                            if (!gameResponse.Success)
                            {
                                context.Success = false;
                                context.Message = $"Failed to generate games for match: {gameResponse.Message}";
                                _logger.LogError("Failed to generate games for R{Round} Match {Seed}: {Message}",
                                    round, seed, gameResponse.Message);
                                return false;
                            }

                            matchesGenerated++;
                            _logger.LogInformation("R{Round} Match {Seed}: TBD vs TBD", round, seed);
                        }
                    }
                }

                _logger.LogInformation("Generated {MatchCount} matches across {RoundCount} rounds for tournament {TournamentId}",
                    matchesGenerated, totalRounds, context.TournamentId);

                return true; // Continue to next step
            }
            catch (Exception ex)
            {
                context.Success = false;
                context.Message = $"Error generating bracket matches: {ex.Message}";
                _logger.LogError(ex, "Error generating bracket matches for tournament {TournamentId}: {Message}",
                    context.TournamentId, ex.Message);
                return false;
            }
        }
    }
}
