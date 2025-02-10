using Application.Services.EventHandling;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.DomainEvents;
using Application.Contracts;

namespace Application.Services.EventHandlers
{
    public class StandingFinishedEventHandler : IDomainEventHandler<StandingFinishedEvent>
    {
        private readonly ILogger<StandingFinishedEventHandler> _logger;

        public StandingFinishedEventHandler(ILogger<StandingFinishedEventHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(StandingFinishedEvent domainEvent)
        {
            _logger.LogInformation($"Standing {domainEvent.StandingId} has been finished!");
            await Task.CompletedTask;
        }
    }
}
