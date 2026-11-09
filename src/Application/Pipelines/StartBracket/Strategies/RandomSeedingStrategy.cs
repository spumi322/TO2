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
        private readonly Random _random;

        public RandomSeedingStrategy(ILogger<RandomSeedingStrategy> logger, Random random)
        {
            _logger = logger;
            _random = random;
        }

        public Format Format => Format.BracketOnly;

        public List<(Team? teamA, Team? teamB)> CreateFirstRoundPairs(List<BracketEntrant> entrants)
        {
            _logger.LogInformation("Randomly seeding {TeamCount} teams", entrants.Count);

            var shuffledTeams = entrants.Select(e => e.Team).ToArray();
            _random.Shuffle(shuffledTeams);

            var paddedTeams = BracketSeedingUtility.PadToPowerOfTwo(shuffledTeams.ToList());
            return BracketSeedingUtility.CreateSingleEliminationPairs(paddedTeams, _logger);
        }
    }
}
