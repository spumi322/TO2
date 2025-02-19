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
        private readonly IGenericRepository<TournamentParticipants> _participantsRepository;

        public AllGroupsFinishedEventHandler(
            ILogger<AllGroupsFinishedEventHandler> logger,
            ITO2DbContext dbContext,
            IGenericRepository<Standing> standingRepository,
            IGenericRepository<TournamentParticipants> participantsRepository)
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
                .Where(g => g.Type == StandingType.Bracket)
                .FirstOrDefault();
            var topTeams = new List<TournamentParticipants>();
            //var bottomTeams = new List<TournamentParticipants>();
            var topX = bracket.MaxTeams / groups.Count;

            foreach (var group in groups)
            {
                var teams = await _dbContext.TournamentParticipants
                    .Where(t => t.StandingId == group.Id && t.TournamentId == tournamentId)
                    .ToListAsync();

                var ordered = teams.OrderByDescending(t => t.Points).ToList();

                topTeams.AddRange(ordered.Take(topX));
                //bottomTeams.AddRange(ordered.Skip(topX));
            }

            foreach (var team in topTeams)
            {

                var teamToAdvance = new TournamentParticipants(
                    team.Id,
                    tournamentId,
                    bracket.Id,
                    TeamStatus.Advanced,
                    team.TeamName);
                
                await _participantsRepository.Add(teamToAdvance);
                await _participantsRepository.Save();
            }

            //foreach (var team in bottomTeams)
            //{
            //    team.Eliminated = true;
            //    team.Status = TeamStatus.Eliminated;

            //    await _participantsRepository.Update(team);
            //    await _participantsRepository.Save();
            //}
        }
    }
}
