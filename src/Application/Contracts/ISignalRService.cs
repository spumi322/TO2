using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts
{
    public interface ISignalRService
    {
        Task BroadcastTournamentCreated(long tournamentId, string createdBy);
        Task BroadcastTournamentUpdated(long tournamentId, string updatedBy);
        Task BroadcastMatchUpdated(long tournamentId, long matchId, string updatedBy);
        Task BroadcastGameUpdated(long tournamentId, long gameId, string updatedBy);
        Task BroadcastStandingUpdated(long tournamentId, long standingId, string updatedBy);
        Task BroadcastTeamAdded(long tournamentId, long teamId, string updatedBy);
        Task BroadcastTeamRemoved(long tournamentId, long teamId, string updatedBy);
        Task BroadcastGroupsStarted(long tournamentId, string updatedBy);
        Task BroadcastBracketStarted(long tournamentId, string updatedBy);
    }
}
