using Domain.Common;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DomainEvents
{
    public class StartTournamentEvent : DomainEvent
    {
        public long TournamentId { get; }
        public Format Format { get; }

        public StartTournamentEvent(long tournamentId, Format format)
        {
            TournamentId = tournamentId;
            Format = format;
        }
    }
}
