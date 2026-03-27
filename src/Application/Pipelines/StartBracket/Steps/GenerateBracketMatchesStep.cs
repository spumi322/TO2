using Application.Contracts;
using Application.Pipelines.StartBracket.Contracts;
using Domain.AggregateRoots;
using Microsoft.Extensions.Logging;


namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 6: Generates all bracket matches.
    /// Round 1: real teams with games (BYE matches auto-resolved, no games).
    /// Round 2+: TBD matches with no games — games generated when both teams are known.
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
            var matchesByPosition = new Dictionary<(int round, int seed), Match>();

            try
            {
                // Pass 1: generate all matches across all rounds
                for (int round = 1; round <= totalRounds; round++)
                {
                    int matchesInRound = (int)Math.Pow(2, totalRounds - round);

                    for (int seed = 1; seed <= matchesInRound; seed++)
                    {
                        Match match;

                        if (round == 1)
                        {
                            var pairIndex = seed - 1;
                            var (teamA, teamB) = context.SeededPairs[pairIndex];

                            // BYE match: one team is null — auto-resolve immediately
                            if (teamA == null || teamB == null)
                            {
                                var realTeam = teamA ?? teamB!;
                                match = await _matchService.GenerateMatch(realTeam, null, round, seed, context.BracketStanding.Id);
                                match.WinnerId = realTeam.Id;
                                matchesGenerated++;
                                _logger.LogInformation("R{Round} Match {Seed}: {TeamA} vs BYE (auto-resolved)",
                                    round, seed, realTeam.Name);
                            }
                            else
                            {
                                match = await _matchService.GenerateMatch(teamA, teamB, round, seed, context.BracketStanding.Id);
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
                        }
                        else
                        {
                            // Round 2+: no games yet — generated when both teams advance
                            match = await _matchService.GenerateMatch(null, null, round, seed, context.BracketStanding.Id);
                            matchesGenerated++;
                            _logger.LogInformation("R{Round} Match {Seed}: TBD vs TBD", round, seed);
                        }

                        matchesByPosition[(round, seed)] = match;
                    }
                }

                // Pass 2: advance BYE winners into their round 2 slots
                foreach (var kvp in matchesByPosition.Where(kvp => kvp.Key.round == 1 && kvp.Value.WinnerId.HasValue))
                {
                    int byeSeed = kvp.Key.seed;
                    var byeMatch = kvp.Value;
                    int nextSeed = (int)Math.Ceiling(byeSeed / 2.0);

                    if (!matchesByPosition.TryGetValue((2, nextSeed), out var nextMatch)) continue;

                    if (byeSeed % 2 == 1)
                        nextMatch.TeamAId = byeMatch.WinnerId;
                    else
                        nextMatch.TeamBId = byeMatch.WinnerId;

                    _logger.LogInformation("BYE winner {WinnerId} from R1S{Seed} advanced to R2S{NextSeed}",
                        byeMatch.WinnerId, byeSeed, nextSeed);

                    // Both slots filled by BYEs — generate games now (teams already set on entity)
                    if (nextMatch.TeamAId.HasValue && nextMatch.TeamBId.HasValue)
                    {
                        var gameResponse = await _gameService.GenerateGames(nextMatch);
                        if (!gameResponse.Success)
                        {
                            context.Success = false;
                            context.Message = $"Failed to generate games for double-BYE match R2S{nextSeed}: {gameResponse.Message}";
                            _logger.LogError("Failed to generate games for double-BYE R2S{NextSeed}: {Message}", nextSeed, gameResponse.Message);
                            return false;
                        }
                        _logger.LogInformation("Both slots BYE-filled at R2S{NextSeed} — games generated", nextSeed);
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
