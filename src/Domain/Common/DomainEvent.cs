using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Common
{
    public abstract class DomainEvent
    {
        public DateTime OccurredOn { get; private set; }

        protected DomainEvent()
        {
            OccurredOn = DateTime.UtcNow;
        }
    }
}
