using Application.DTOs.Standing;
using Domain.AggregateRoots;
using Domain.Enums;

namespace Application.Pipelines.StartBracket.Strategies
{
    /// <summary>
    /// Strategy interface for forming bracket round 1 from the teams entering the bracket.
    /// One implementation per tournament format that has a bracket.
    /// </summary>
    public interface IBracketSeedingStrategy
    {
        /// <summary>
        /// The tournament format this strategy handles
        /// </summary>
        Format Format { get; }

        /// <summary>
        /// Creates the round 1 pairs. A null team is a BYE.
        /// The pair count is always a power of 2.
        /// </summary>
        List<(Team? teamA, Team? teamB)> CreateFirstRoundPairs(List<BracketEntrant> entrants);
    }
}
