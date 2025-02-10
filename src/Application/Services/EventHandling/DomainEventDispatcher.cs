using Application.Contracts;
using Domain.Common;
using Domain.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.EventHandling
{
    public class DomainEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public DomainEventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task DispatchAsync(DomainEvent domainEvent)
        {
            var handlerType = typeof(DomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = _serviceProvider.GetServices(handlerType).Cast<object>().ToList();

            foreach (var handler in handlers)
            {
                await (Task)((dynamic)handler).HandleAsync((dynamic)domainEvent);
            }
        }
    }
}
