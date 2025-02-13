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
        private readonly IStandingService _standingService;

        public AllGroupsFinishedEventHandler(
            ILogger<AllGroupsFinishedEventHandler> logger,
            ITO2DbContext dbContext,
            IGenericRepository<Standing> standingRepository,
            IStandingService standingService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _standingRepository = standingRepository;
            _standingService = standingService;
        }

        public async Task HandleAsync(AllGroupsFinishedEvent domainEvent)
        {
            var tournamentId = domainEvent.TournamentId;
            var groups = (await _standingRepository.GetAllByFK("TournamentId", tournamentId))
                .Where(s => s.Type == StandingType.Group);

            var bracket = (await _standingRepository.GetAllByFK("TournamentId", tournamentId))
                .Where(s => s.Type == StandingType.Bracket)
                .FirstOrDefault();

            var topX = await _standingService.TopX(domainEvent.TournamentId);
            var teamsToAdvanceToBracket = new List<BracketSeedDTO>();

            foreach (var group in groups)
            {
                var topTeams = await _dbContext.TournamentParticipants
                        .Where(tp => tp.TournamentId == tournamentId && tp.StandingId == group.Id)
                        .OrderByDescending(tp => tp.Points)
                        .ThenByDescending(tp => tp.Wins)
                        .Take(topX)
                        .ToListAsync();

                for (var i = 0; i < topTeams.Count; i++)
                {
                    teamsToAdvanceToBracket.Add(new BracketSeedDTO
                    {
                        TeamId = topTeams[i].TeamId,
                        GroupId = group.Id,
                        Placement = i + 1,
                        TeamName = topTeams[i].TeamName,
                    });
                }

                var bottomTeams = await _dbContext.TournamentParticipants
                        .Where(tp => tp.TournamentId == tournamentId && tp.StandingId == group.Id)
                        .OrderByDescending(tp => tp.Points)
                        .ThenByDescending(tp => tp.Wins)
                        .Skip(topX)
                        .ToListAsync();

                foreach (var team in bottomTeams)
                {
                    team.Status = TeamStatus.Eliminated;
                    team.Eliminated = true;
                }
            }

            foreach (var team in teamsToAdvanceToBracket)
            {
                _dbContext.TournamentParticipants.Add(new TournamentParticipants
                {
                    TeamId = team.TeamId,
                    TournamentId = tournamentId,
                    StandingId = bracket.Id,
                    Status = TeamStatus.Advanced,
                    Eliminated = false,
                    Wins = 0,
                    Losses = 0,
                    Points = 0,
                    TeamName = team.TeamName
                });
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
