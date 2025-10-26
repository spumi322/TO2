using Domain.Common;

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
