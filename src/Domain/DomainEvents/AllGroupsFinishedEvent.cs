using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DomainEvents
{
    public class AllGroupsFinishedEvent : DomainEvent
    {
        public long TournamentId { get; }

        public AllGroupsFinishedEvent(long tournamentId)
        {
            TournamentId = tournamentId;
        }
    }
}
