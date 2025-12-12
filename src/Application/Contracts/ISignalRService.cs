using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts
{
    public interface ISignalRService
    {
        Task BroadcastTournamentUpdated(long tournamentId, string updatedBy);
        Task BroadcastMatchUpdated(long tournamentId, long matchId, string updatedBy);
        Task BroadcastGameUpdated(long tournamentId, long gameId, string updatedBy);
        Task BroadcastStandingUpdated(long tournamentId, long standingId, string updatedBy);
    }
}
