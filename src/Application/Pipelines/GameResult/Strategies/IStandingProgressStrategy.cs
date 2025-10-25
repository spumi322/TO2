using Application.Pipelines.GameResult.Contracts;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Pipelines.GameResult.Strategies
{
    /// <summary>
    /// Strategy interface for progressing a standing after a match completes.
    /// Different implementations for Group vs Bracket standings.
    /// </summary>
    public interface IStandingProgressStrategy
    {
        /// <summary>
        /// The standing type this strategy handles (Group or Bracket)
        /// </summary>
        StandingType StandingType { get; }

        /// <summary>
        /// Progresses the standing after a match completes.
        /// Determines if standing/tournament is finished and what should happen next.
        /// </summary>
        /// <param name="tournamentId">The tournament ID</param>
        /// <param name="standingId">The standing ID</param>
        /// <param name="matchId">The match ID that just completed</param>
        /// <param name="winnerId">The winning team ID</param>
        /// <returns>Result indicating pipeline flow control and status updates</returns>
        Task<StandingProgressResult> ProgressStandingAsync(long tournamentId, long standingId, long matchId, long winnerId);
    }
}
