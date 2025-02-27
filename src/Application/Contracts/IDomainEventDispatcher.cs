using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts
{
    public interface IDomainEventDispatcher
    {
        void QueueEvent(DomainEvent domainEvent);
        Task DispatchQueuedEvents();
    }
}
