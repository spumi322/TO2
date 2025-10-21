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
        private readonly IGenericRepository<Standing> _standingRepository;
        private readonly IStandingService _standingService;

        public StandingFinishedEventHandler(
            ILogger<StandingFinishedEventHandler> logger,
            IGenericRepository<Standing> standingsRepositry,
            IStandingService standingService)
        {
            _logger = logger;
            _standingRepository = standingsRepositry;
            _standingService = standingService;
        }

        public async Task HandleAsync(StandingFinishedEvent domainEvent)
        {
            var standing = await _standingRepository.Get(domainEvent.StandingId);

            if (standing != null && !standing.IsFinished)
            {
                standing.IsFinished = true;
                _logger.LogInformation($"Standing {domainEvent.StandingId} has been finished!");
                // Removed Save() call - DbContext automatically tracks changes and saves after event handlers complete

                // Check if all groups are now finished
                await _standingService.CheckAllGroupsAreFinished(standing.TournamentId);
            }
        }
    }
}
