using Domain.Common;
using Domain.Entities;
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
        public List<Standing> Groups { get; }

        public AllGroupsFinishedEvent(long tournamentId, List<Standing> groups)
        {
            TournamentId = tournamentId;
            Groups = groups;
        }
    }
}
