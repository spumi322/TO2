using Application.Contracts;
using Microsoft.AspNetCore.SignalR;
using TO2.Hubs;

namespace TO2.SignalR
{
    public class SignalRService : ISignalRService
    {
        private readonly IHubContext<TournamentHub> _hubContext;

        public SignalRService(IHubContext<TournamentHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastTournamentUpdated(long tournamentId, string updatedBy)
        {
            await _hubContext.Clients
                .Group($"tournament-{tournamentId}")
                .SendAsync("TournamentUpdated", new { tournamentId, updatedBy });
        }

        public async Task BroadcastMatchUpdated(long tournamentId, long matchId, string updatedBy)
        {
            await _hubContext.Clients
                .Group($"tournament-{tournamentId}")
                .SendAsync("MatchUpdated", new { tournamentId, matchId, updatedBy });
        }

        public async Task BroadcastGameUpdated(long tournamentId, long gameId, string updatedBy)
        {
            await _hubContext.Clients
                .Group($"tournament-{tournamentId}")
                .SendAsync("GameUpdated", new { tournamentId, gameId, updatedBy });
        }

        public async Task BroadcastStandingUpdated(long tournamentId, long standingId, string updatedBy)
        {
            await _hubContext.Clients
                .Group($"tournament-{tournamentId}")
                .SendAsync("StandingUpdated", new { tournamentId, standingId, updatedBy });
        }
    }
}
