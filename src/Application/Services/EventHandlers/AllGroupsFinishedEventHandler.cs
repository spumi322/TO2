using Application.Contracts;
using Application.DTOs.Standing;
using Domain.DomainEvents;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.EventHandlers
{
    public class AllGroupsFinishedEventHandler : IDomainEventHandler<AllGroupsFinishedEvent>
    {
        private readonly ILogger<AllGroupsFinishedEventHandler> _logger;
        private readonly ITO2DbContext _dbContext;
        private readonly IGenericRepository<Standing> _standingRepository;

        public AllGroupsFinishedEventHandler(
            ILogger<AllGroupsFinishedEventHandler> logger,
            ITO2DbContext dbContext,
            IGenericRepository<Standing> standingRepository)
        {
            _logger = logger;
            _dbContext = dbContext;
            _standingRepository = standingRepository;
        }

        public async Task HandleAsync(AllGroupsFinishedEvent domainEvent)
        {
            var tournamentId = domainEvent.TournamentId;
            var groups = (await _standingRepository.GetAllByFK("TournamentId", tournamentId))
                .Where(s => s.Type == StandingType.Group)
                .ToList();

            var bracket = (await _standingRepository.GetAllByFK("TournamentId", tournamentId))
                .Where(s => s.Type == StandingType.Bracket)
                .FirstOrDefault();

            var participants = await _dbContext.TournamentParticipants
                .Where(tp => tp.TournamentId == tournamentId)
                .ToListAsync();

            var topX = bracket.MaxTeams / groups.Count;
            var teamsToAdvance = new List<BracketSeedDTO>();
            var teamsToEliminate = new List<TournamentParticipants>();

            foreach (var group in groups)
            {
                var groupParticipants = participants
                    .Where(tp => tp.StandingId == group.Id)
                    .OrderByDescending(gp => gp.Points)
                    .ThenByDescending(gp => gp.Wins)
                    .ToList();

                var topTeams = groupParticipants.Take(topX).ToList();
                var bottomTeams = groupParticipants.Skip(topX).ToList();

                for (var i = 0; i < topTeams.Count; i++)
                {
                    teamsToAdvance.Add(new BracketSeedDTO
                    {
                        TeamId = topTeams[i].TeamId,
                        GroupId = group.Id,
                        Placement = i + 1,
                        TeamName = topTeams[i].TeamName,
                    });
                }

                teamsToEliminate.AddRange(bottomTeams);
            }

            foreach (var team in teamsToAdvance)
            {
                var tournamentEntry = new TournamentParticipants(
                    team.GroupId,
                    tournamentId,
                    bracket.Id,
                    TeamStatus.Advanced,
                    false,
                    0,
                    0,
                    0,
                    team.TeamName);

                _dbContext.TournamentParticipants.Add(tournamentEntry);
            }

            foreach (var team in teamsToEliminate)
            {
                team.Status = TeamStatus.Eliminated;
                team.Eliminated = true;
                _dbContext.TournamentParticipants.Update(team);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
