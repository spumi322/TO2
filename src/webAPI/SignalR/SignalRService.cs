using Application.Contracts;
using Application.DTOs.SignalR;
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

        public async Task BroadcastTournamentCreated(long tournamentId, string createdBy)
        {
            // Note: Can't get tenantId here without injecting ITenantService
            // Broadcast to all for now - clients are already filtered by tenant via JWT
            await _hubContext.Clients
                .All
                .SendAsync("TournamentCreated", new { tournamentId, updatedBy = createdBy });
        }

        public async Task BroadcastTournamentUpdated(long tournamentId, string updatedBy)
        {
            // Broadcast to all clients so tournament list can receive updates
            await _hubContext.Clients
                .All
                .SendAsync("TournamentUpdated", new { tournamentId, updatedBy });
        }

        public async Task BroadcastMatchUpdated(long tournamentId, long matchId, string updatedBy)
        {
            await _hubContext.Clients
                .Group($"tournament-{tournamentId}")
                .SendAsync("MatchUpdated", new { tournamentId, matchId, updatedBy });
        }

        public async Task BroadcastGameUpdated(GameUpdatedEvent eventPayload)
        {
            await _hubContext.Clients
                .Group($"tournament-{eventPayload.TournamentId}")
                .SendAsync("GameUpdated", eventPayload);
        }

        public async Task BroadcastStandingUpdated(long tournamentId, long standingId, string updatedBy)
        {
            await _hubContext.Clients
                .Group($"tournament-{tournamentId}")
                .SendAsync("StandingUpdated", new { tournamentId, standingId, updatedBy });
        }

        public async Task BroadcastTeamAdded(long tournamentId, long teamId, string updatedBy)
        {
            await _hubContext.Clients
                .Group($"tournament-{tournamentId}")
                .SendAsync("TeamAdded", new { tournamentId, teamId, updatedBy });
        }

        public async Task BroadcastTeamRemoved(long tournamentId, long teamId, string updatedBy)
        {
            await _hubContext.Clients
                .Group($"tournament-{tournamentId}")
                .SendAsync("TeamRemoved", new { tournamentId, teamId, updatedBy });
        }

        public async Task BroadcastGroupsStarted(long tournamentId, string updatedBy)
        {
            await _hubContext.Clients
                .Group($"tournament-{tournamentId}")
                .SendAsync("GroupsStarted", new { tournamentId, updatedBy });
        }

        public async Task BroadcastBracketStarted(long tournamentId, string updatedBy)
        {
            await _hubContext.Clients
                .Group($"tournament-{tournamentId}")
                .SendAsync("BracketStarted", new { tournamentId, updatedBy });
        }
    }
}
