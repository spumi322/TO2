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
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<DomainEvent> _events = new();

        public DomainEventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void QueueEvent(DomainEvent domainEvent)
        {
            _events.Add(domainEvent);
        }

        public async Task DispatchQueuedEvents()
        {
            var events = _events.ToList();
            _events.Clear();

            foreach (var domainEvent in events)
            {
                var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
                var handlers = _serviceProvider.GetServices(handlerType).Cast<object>().ToList();

                foreach (var handler in handlers)
                {
                    await (Task)((dynamic)handler).HandleAsync((dynamic)domainEvent);
                }
            }
        }
    }
}
