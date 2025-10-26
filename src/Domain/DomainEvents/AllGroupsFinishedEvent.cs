using Domain.Common;
using Domain.Entities;

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
