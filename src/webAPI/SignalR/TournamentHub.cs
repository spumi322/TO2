using Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TO2.Hubs
{
    [Authorize]
    public class TournamentHub : Hub
    {
        private readonly ITenantService _tenantService;

        public TournamentHub(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        public override async Task OnConnectedAsync()
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");
            await base.OnConnectedAsync();
        }

        public async Task JoinTournament(long tournamentId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tournament-{tournamentId}");
        }

        public async Task LeaveTournament(long tournamentId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tournament-{tournamentId}");
        }
    }
}
