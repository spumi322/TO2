using Application.Contracts;
using Domain.AggregateRoots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TO2.Hubs
{
    [Authorize]
    public class TournamentHub : Hub
    {
        private readonly ITenantService _tenantService;
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly ILogger<TournamentHub> _logger;

        public TournamentHub(
            ITenantService tenantService,
            IRepository<Tournament> tournamentRepository,
            ILogger<TournamentHub> logger)
        {
            _tenantService = tenantService;
            _tournamentRepository = tournamentRepository;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");
            await base.OnConnectedAsync();
        }

        public async Task JoinTournament(long tournamentId)
        {
            // A query (not Find), so the tenant query filter applies: another tenant's tournament is "not found"
            if (!await _tournamentRepository.ExistsAsync(t => t.Id == tournamentId))
            {
                _logger.LogWarning(
                    "Refused hub join: user {UserName} (tenant {TenantId}) tried to join tournament {TournamentId}",
                    _tenantService.GetCurrentUserName(), _tenantService.GetCurrentTenantId(), tournamentId);
                throw new HubException("Tournament not found.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"tournament-{tournamentId}");
        }

        public async Task LeaveTournament(long tournamentId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tournament-{tournamentId}");
        }
    }
}
