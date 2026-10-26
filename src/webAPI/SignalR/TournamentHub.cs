using Application.Contracts;
using Application.DTOs.SignalR;
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
        private readonly IMatchDraftStore _draftStore;
        private readonly ILogger<TournamentHub> _logger;

        public TournamentHub(
            ITenantService tenantService,
            IRepository<Tournament> tournamentRepository,
            IMatchDraftStore draftStore,
            ILogger<TournamentHub> logger)
        {
            _tenantService = tenantService;
            _tournamentRepository = tournamentRepository;
            _draftStore = draftStore;
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

            // Drafts made before this page joined (first load, refresh, reconnect)
            foreach (var draft in _draftStore.GetAll(tournamentId))
                await Clients.Caller.SendAsync("MatchDraftUpdated", draft);
        }

        public async Task LeaveTournament(long tournamentId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tournament-{tournamentId}");
        }

        // Relays a match's draft score to the others on the tournament page and keeps it in memory for pages that join
        // later. Not saved to the database: Confirm saves the result
        public async Task ShareMatchDraft(long tournamentId, long matchId, int teamAWins, int teamBWins)
        {
            // Same tenant check as JoinTournament: only into a tournament group of the caller's own tenant
            if (!await _tournamentRepository.ExistsAsync(t => t.Id == tournamentId))
            {
                _logger.LogWarning(
                    "Refused match draft: user {UserName} (tenant {TenantId}) sent a draft for tournament {TournamentId}",
                    _tenantService.GetCurrentUserName(), _tenantService.GetCurrentTenantId(), tournamentId);
                throw new HubException("Tournament not found.");
            }

            var draft = new MatchDraftUpdatedEvent(tournamentId, matchId, teamAWins, teamBWins, _tenantService.GetCurrentUserName());
            _draftStore.Set(draft);
            await Clients.OthersInGroup($"tournament-{tournamentId}").SendAsync("MatchDraftUpdated", draft);
        }
    }
}
