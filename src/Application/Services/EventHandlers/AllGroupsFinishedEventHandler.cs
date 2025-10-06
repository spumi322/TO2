using Application.Contracts;
using Application.DTOs.Standing;
using Domain.AggregateRoots;
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
        private readonly IGenericRepository<Group> _participantsRepository;

        public AllGroupsFinishedEventHandler(
            ILogger<AllGroupsFinishedEventHandler> logger,
            ITO2DbContext dbContext,
            IGenericRepository<Standing> standingRepository,
            IGenericRepository<Group> participantsRepository)
        {
            _logger = logger;
            _dbContext = dbContext;
            _standingRepository = standingRepository;
            _participantsRepository = participantsRepository;
        }

        public async Task HandleAsync(AllGroupsFinishedEvent domainEvent)
        {
            var tournamentId = domainEvent.TournamentId;
            var groups = domainEvent.Groups;
            var bracket = (await _standingRepository.GetAllByFK("TournamentId", tournamentId))
                .Where(g => g.StandingType == StandingType.Bracket)
                .FirstOrDefault();

            if (bracket == null)
            {
                _logger.LogWarning("No bracket found for tournament {TournamentId}", tournamentId);
                return;
            }

            var topTeams = new List<Group>();
            var bottomTeams = new List<Group>();
            var topX = bracket.MaxTeams / groups.Count;

            if (topX < 1)
            {
                _logger.LogWarning("Invalid bracket configuration: MaxTeams={MaxTeams}, Groups={GroupCount}",
                    bracket.MaxTeams, groups.Count);
                return;
            }

            foreach (var group in groups)
            {
                var teams = await _dbContext.GroupEntries
                    .Where(t => t.StandingId == group.Id && t.TournamentId == tournamentId)
                    .ToListAsync();

                var ordered = teams.OrderByDescending(t => t.Points)
                    .ThenByDescending(t => t.Wins)
                    .ToList();

                topTeams.AddRange(ordered.Take(topX));
                bottomTeams.AddRange(ordered.Skip(topX));
            }

            // Mark eliminated teams
            foreach (var team in bottomTeams)
            {
                team.Eliminated = true;
                team.Status = TeamStatus.Eliminated;
            }

            // Update bracket participants with advancing teams
            foreach (var team in topTeams)
            {
                team.StandingId = bracket.Id;
                team.Status = TeamStatus.Competing;
                team.Eliminated = false;
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Advanced {TopTeamCount} teams to bracket, eliminated {BottomTeamCount} teams",
                topTeams.Count, bottomTeams.Count);
        }
    }
}
