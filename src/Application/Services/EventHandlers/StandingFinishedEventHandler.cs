using Application.Services.EventHandling;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.DomainEvents;

namespace Application.Services.EventHandlers
{
    public class StandingCompletedEventHandler : DomainEventHandler<StandingFinishedEvent>
    {
        private readonly ILogger<StandingCompletedEventHandler> _logger;

        public StandingCompletedEventHandler(ILogger<StandingCompletedEventHandler> logger)
        {
            _logger = logger;
        }

        public override async Task HandleAsync(StandingFinishedEvent domainEvent)
        {
            _logger.LogInformation($"Standing {domainEvent.StandingId} has been finished!");
            await Task.CompletedTask;
        }
    }
}
