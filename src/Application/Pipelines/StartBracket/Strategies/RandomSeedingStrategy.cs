using Application.DTOs.Standing;
using Application.Pipelines.StartBracket.Utilities;
using Domain.AggregateRoots;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Strategies
{
    /// <summary>
    /// BracketOnly: no group results to seed by, so teams are shuffled randomly before pairing.
    /// </summary>
    public class RandomSeedingStrategy : IBracketSeedingStrategy
    {
        private readonly ILogger<RandomSeedingStrategy> _logger;

        public RandomSeedingStrategy(ILogger<RandomSeedingStrategy> logger)
        {
            _logger = logger;
        }

        public Format Format => Format.BracketOnly;

        public List<(Team? teamA, Team? teamB)> CreateFirstRoundPairs(List<BracketEntrant> entrants)
        {
            _logger.LogInformation("Randomly seeding {TeamCount} teams", entrants.Count);

            var shuffledTeams = entrants
                .Select(e => e.Team)
                .OrderBy(_ => Random.Shared.Next())
                .ToList();

            var paddedTeams = BracketSeedingUtility.PadToPowerOfTwo(shuffledTeams);
            return BracketSeedingUtility.CreateSingleEliminationPairs(paddedTeams, _logger);
        }
    }
}
