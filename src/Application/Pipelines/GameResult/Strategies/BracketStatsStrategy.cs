using Application.Contracts;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Pipelines.GameResult.Strategies
{
    /// <summary>
    /// Strategy for updating Bracket standing statistics.
    /// Updates team status (Advanced/Eliminated) in bracket table.
    /// </summary>
    public class BracketStatsStrategy : IStandingStatsStrategy
    {
        private readonly IGenericRepository<Bracket> _bracketRepository;

        public StandingType StandingType => StandingType.Bracket;

        public BracketStatsStrategy(IGenericRepository<Bracket> bracketRepository)
        {
            _bracketRepository = bracketRepository;
        }

        public async Task UpdateStatsAsync(long standingId, long winnerId, long loserId)
        {
            var bracketEntries = await _bracketRepository.GetAllByFK("StandingId", standingId);

            var winner = bracketEntries.FirstOrDefault(b => b.TeamId == winnerId);
            var loser = bracketEntries.FirstOrDefault(b => b.TeamId == loserId);

            if (winner == null || loser == null)
                throw new Exception("Teams not found in bracket standings");

            // Update winner status
            winner.Status = TeamStatus.Advanced;

            // Update loser status
            loser.Status = TeamStatus.Eliminated;
            loser.Eliminated = true;

            // Save changes
            await _bracketRepository.Update(winner);
            await _bracketRepository.Update(loser);
            await _bracketRepository.Save();
        }
    }
}
