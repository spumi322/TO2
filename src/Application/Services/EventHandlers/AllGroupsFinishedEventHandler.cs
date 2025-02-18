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
                .Where(g => g.Type == StandingType.Group)
                .ToList();
            var bracket = (await _standingRepository.GetAllByFK("TournamentId", tournamentId))
                .Where(g => g.Type == StandingType.Bracket)
                .ToList();
            var topTeams = new List<Team>();
            var bottomTeams = new List<Team>();
            var topX = bracket.First().MaxTeams / groups.Count;

            // Get top/bottom teams each group
            foreach (var group in groups)
            {
                
            }
        }
    }
}
