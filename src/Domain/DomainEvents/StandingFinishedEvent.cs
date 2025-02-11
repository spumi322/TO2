using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DomainEvents
{
    public class StandingFinishedEvent : DomainEvent
    {
        public long StandingId { get; }

        public StandingFinishedEvent(long standingId)
        {
            StandingId = standingId;
        }
    }
}
