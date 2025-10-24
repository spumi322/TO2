using Domain.Enums;
using System.Threading.Tasks;

namespace Application.Pipelines.GameResult.Strategies
{
    /// <summary>
    /// Strategy interface for updating standing statistics after a match completes.
    /// Different implementations for Group vs Bracket standings.
    /// </summary>
    public interface IStandingStatsStrategy
    {
        /// <summary>
        /// The standing type this strategy handles (Group or Bracket)
        /// </summary>
        StandingType StandingType { get; }

        /// <summary>
        /// Updates the standing entries (Group or Bracket table) with match results.
        /// </summary>
        /// <param name="standingId">The standing ID</param>
        /// <param name="winnerId">The winning team ID</param>
        /// <param name="loserId">The losing team ID</param>
        Task UpdateStatsAsync(long standingId, long winnerId, long loserId);
    }
}
