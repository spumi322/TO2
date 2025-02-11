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

namespace Application.Services.EventHandlers
{
    public class StandingFinishedEventHandler : IDomainEventHandler<StandingFinishedEvent>
    {
        private readonly ILogger<StandingFinishedEventHandler> _logger;
        private readonly IGenericRepository<Standing> _standingsRepository;    
        

        public StandingFinishedEventHandler(ILogger<StandingFinishedEventHandler> logger, IGenericRepository<Standing> standingsRepositry)
        {
            _logger = logger;
            _standingsRepository = standingsRepositry;
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
        }
    }
}
