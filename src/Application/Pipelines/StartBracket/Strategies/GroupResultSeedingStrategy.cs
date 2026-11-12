using Application.DTOs.Standing;
using Application.Pipelines.StartBracket.Utilities;
using Domain.AggregateRoots;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Strategies
{
    /// <summary>
    /// GroupsAndBracket: seeds teams by their group placement,
    /// then separates teams of the same group in round 1.
    /// </summary>
    public class GroupResultSeedingStrategy : IBracketSeedingStrategy
    {
        private readonly ILogger<GroupResultSeedingStrategy> _logger;

        public GroupResultSeedingStrategy(ILogger<GroupResultSeedingStrategy> logger)
        {
            _logger = logger;
        }

        public Format Format => Format.GroupsAndBracket;

        public List<(Team? teamA, Team? teamB)> CreateFirstRoundPairs(List<BracketEntrant> entrants)
        {
            _logger.LogInformation("Seeding {TeamCount} teams by group results", entrants.Count);

            // Seed order: all group winners first, then all runners-up, then 3rd places...
            // The bracket pairs seed 1 vs last, seed 2 vs second-to-last, etc. (BYEs fill the last seeds),
            // so group winners face runners-up or BYEs in round 1, never each other.
            // (Unavoidable only when more than half of the bracket slots are group winners, e.g. 1 team advancing per group.)
            var seededTeams = entrants
                .OrderBy(e => e.GroupPlacement)
                .ThenBy(e => e.GroupId)
                .Select(e => e.Team)
                .ToList();

            var paddedTeams = BracketSeedingUtility.PadToPowerOfTwo(seededTeams);
            var pairs = BracketSeedingUtility.CreateSingleEliminationPairs(paddedTeams, _logger);

            // Teams from the same group should not meet again in round 1
            var groupOf = entrants.ToDictionary(e => e.Team.Id, e => e.GroupId!.Value);
            int unresolved = BracketSeedingUtility.SeparateSameGroupPairs(pairs, groupOf);

            if (unresolved > 0)
            {
                _logger.LogWarning("{Count} round 1 matches between teams of the same group could not be avoided", unresolved);
            }

            return pairs;
        }
    }
}
