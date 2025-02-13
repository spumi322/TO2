using Application.Services.EventHandling;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.DomainEvents;
using Application.Contracts;
using Domain.Entities;
using Domain.AggregateRoots;
using Domain.Enums;
using System.Reflection;

namespace Application.Services.EventHandlers
{
    public class StandingFinishedEventHandler : IDomainEventHandler<StandingFinishedEvent>
    {
        private readonly ILogger<StandingFinishedEventHandler> _logger;
        private readonly IGenericRepository<Standing> _standingsRepository;
        private readonly IGenericRepository<Tournament> _tournamentRepository;


        public StandingFinishedEventHandler(
            ILogger<StandingFinishedEventHandler> logger,
            IGenericRepository<Standing> standingsRepositry,
            IGenericRepository<Tournament> tournamentRepository)
        {
            _logger = logger;
            _standingsRepository = standingsRepositry;
            _tournamentRepository = tournamentRepository;
        }

        public async Task HandleAsync(StandingFinishedEvent domainEvent)
        {
            var standing = await _standingsRepository.Get(domainEvent.StandingId);

            if (standing != null && !standing.IsFinished)
            {
                standing.IsFinished = true;
                _logger.LogInformation($"Standing {domainEvent.StandingId} has been finished!");
                await _standingsRepository.Save();
            }

            var allGroups = (await _standingsRepository.GetAllByFK("TournamentId", standing.TournamentId))
                .Where(s => s.Type == StandingType.Group);

            if (allGroups.All(ag => ag.IsFinished))
            {
                var tournament = await _tournamentRepository.Get(standing.TournamentId);

                if (tournament != null && !tournament.DomainEvents.Any(e => e is AllGroupsFinishedEvent))
                {
                    tournament.AddDomainEvent(new AllGroupsFinishedEvent(standing.TournamentId));
                    await _tournamentRepository.Save();
                }
            }
        }
    }
}
