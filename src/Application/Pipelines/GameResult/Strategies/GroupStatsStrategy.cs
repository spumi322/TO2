using Application.Contracts;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Pipelines.GameResult.Strategies
{
    /// <summary>
    /// Strategy for updating Group standing statistics.
    /// Updates wins, losses, and points for teams in a group.
    /// </summary>
    public class GroupStatsStrategy : IStandingStatsStrategy
    {
        private readonly IGenericRepository<Group> _groupRepository;

        public StandingType StandingType => StandingType.Group;

        public GroupStatsStrategy(IGenericRepository<Group> groupRepository)
        {
            _groupRepository = groupRepository;
        }

        public async Task UpdateStatsAsync(long standingId, long winnerId, long loserId)
        {
            var groupEntries = await _groupRepository.GetAllByFK("StandingId", standingId);

            var winner = groupEntries.FirstOrDefault(g => g.TeamId == winnerId);
            var loser = groupEntries.FirstOrDefault(g => g.TeamId == loserId);

            if (winner == null || loser == null)
                throw new Exception("Teams not found in group standings");

            // Update winner stats
            winner.Wins += 1;
            winner.Points += 3;

            // Update loser stats
            loser.Losses += 1;

            // Save changes
            await _groupRepository.Update(winner);
            await _groupRepository.Update(loser);
            await _groupRepository.Save();
        }
    }
}
