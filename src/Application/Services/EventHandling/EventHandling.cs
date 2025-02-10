using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public abstract class DomainEventHandler<T> where T : DomainEvent
    {
        public abstract Task HandleAsync(T domainEvent);
    }
}
