using Application.Contracts;
using Application.Contracts.Repositories;
using Application.DTOs.Tournament;
using Application.Utilities;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class PlacementService : IPlacementService
    {
        private readonly IStandingRepository _standingRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IRepository<Team> _teamRepository;
        private readonly IRepository<TournamentTeam> _tournamentTeamRepository;
        private readonly ILogger<PlacementService> _logger;

        public PlacementService(IStandingRepository standingRepository,
                                IMatchRepository matchRepository,
                                IRepository<Tournament> tournamentRepository,
                                IGroupRepository groupRepository,
                                IRepository<Team> teamRepository,
                                IRepository<TournamentTeam> tournamentTeamRepository,
                                ILogger<PlacementService> logger)
        {
            _standingRepository = standingRepository;
            _matchRepository = matchRepository;
            _tournamentRepository = tournamentRepository;
            _groupRepository = groupRepository;
            _teamRepository = teamRepository;
            _tournamentTeamRepository = tournamentTeamRepository;
            _logger = logger;
        }

        public async Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateBracketPlacements(long standingId)
        {
            var placements = new List<(long TeamId, int Placement, int? EliminatedInRound)>();

            // Get all matches for this bracket
            var allMatches = await _matchRepository.FindAllAsync(m => m.StandingId == standingId);
            var matches = allMatches.OrderByDescending(m => m.Round).ThenBy(m => m.Seed).ToList();

            if (!matches.Any())
            {
                _logger.LogWarning("No matches found for bracket standing {StandingId}, no placements calculated", standingId);
                return placements;
            }

            // Find max round (final)
            int maxRound = matches.Max(m => m.Round ?? 0);

            // Champion = winner of the final
            var finalMatch = matches.FirstOrDefault(m => m.Round == maxRound && m.Seed == 1);
            if (finalMatch?.WinnerId != null)
            {
                placements.Add((finalMatch.WinnerId.Value, 1, null));
            }

            // Teams eliminated in the same round share a rank:
            // one rank below the teams that reached the next round (one winner per match of this round)
            for (int round = maxRound; round >= 1; round--)
            {
                var roundMatches = matches.Where(m => m.Round == round).ToList();
                int rank = roundMatches.Count + 1;

                foreach (var match in roundMatches.Where(m => m.LoserId.HasValue))
                {
                    placements.Add((match.LoserId!.Value, rank, round));
                }
            }

            return placements;
        }

        public async Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateGroupOnlyPlacements(long tournamentId)
        {
            var placements = new List<(long TeamId, int Placement, int? EliminatedInRound)>();

            var standings = await _standingRepository.FindAllAsync(s => s.TournamentId == tournamentId);
            var groupStandings = standings.Where(s => s.StandingType == StandingType.Group).ToList();

            foreach (var group in groupStandings)
            {
                // Placement = position in the group (team statuses were already set when the group finished)
                var teams = GroupRanking.Rank(await _groupRepository.GetByStandingIdAsync(group.Id));
                placements.AddRange(teams.Select((team, index) => (team.TeamId, index + 1, (int?)null)));
            }

            return placements;
        }

        public async Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateGroupStageKnockoutPlacements(long tournamentId)
        {
            var entries = await _groupRepository.GetByTournamentIdAsync(tournamentId);

            // Group position of each knocked-out team (Eliminated was set when the bracket started)
            var knockedOut = entries
                .GroupBy(e => e.StandingId)
                .SelectMany(group => GroupRanking.Rank(group).Select((entry, index) => (Entry: entry, GroupPosition: index + 1)))
                .Where(x => x.Entry.Eliminated)
                .ToList();

            var placements = new List<(long TeamId, int Placement, int? EliminatedInRound)>();

            // Below every team that reached the bracket; teams with the same group position share a rank
            int rank = entries.Count(e => !e.Eliminated) + 1;

            foreach (var samePosition in knockedOut.GroupBy(x => x.GroupPosition).OrderBy(g => g.Key))
            {
                foreach (var x in samePosition)
                {
                    placements.Add((x.Entry.TeamId, rank, null));
                }

                rank += samePosition.Count();
            }

            return placements;
        }

        public async Task SetFinalResults(long tournamentId, List<(long TeamId, int Placement, int? EliminatedInRound)> placements)
        {
            var now = DateTime.UtcNow;

            var tournamentTeams = await _tournamentTeamRepository.FindAllAsync(tt => tt.TournamentId == tournamentId);

            foreach (var placement in placements)
            {
                var tournamentTeam = tournamentTeams.FirstOrDefault(tt => tt.TeamId == placement.TeamId);

                if (tournamentTeam == null)
                {
                    _logger.LogWarning("Team {TeamId} is not registered in tournament {TournamentId}, placement {Placement} not stored",
                        placement.TeamId, tournamentId, placement.Placement);
                    continue;
                }

                tournamentTeam.FinalPlacement = placement.Placement;
                tournamentTeam.EliminatedInRound = placement.EliminatedInRound;
                tournamentTeam.ResultFinalizedAt = now;
            }
        }

        public async Task<List<TeamPlacementDTO>> GetFinalResultsAsync(long tournamentId)
        {
            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId)
                ?? throw new NotFoundException("Tournament", tournamentId);

            if (tournament.Status != TournamentStatus.Finished)
            {
                return new List<TeamPlacementDTO>();
            }

            // Single source: the placements stored by SetFinalResults (all formats).
            // FinalPlacement is filtered in memory, not in SQL: CalculateFinalPlacementsStep reads the results
            // before the pipeline commits, and only the tracked entities hold the new placements by then.
            var tournamentTeams = (await _tournamentTeamRepository.FindAllAsync(tt => tt.TournamentId == tournamentId))
                .Where(tt => tt.FinalPlacement != null)
                .ToList();

            var teamIds = tournamentTeams.Select(tt => tt.TeamId).ToList();
            var teams = await _teamRepository.FindAllAsync(t => teamIds.Contains(t.Id));
            var teamNameLookup = teams.ToDictionary(t => t.Id, t => t.Name);
            var groupNameLookup = await GetGroupPlacementNamesAsync(tournament);

            return tournamentTeams
                .OrderBy(tt => tt.FinalPlacement)
                .ThenBy(tt => tt.TeamId)
                .Select(tt => new TeamPlacementDTO(
                    tt.TeamId,
                    teamNameLookup.GetValueOrDefault(tt.TeamId, "Unknown Team"),
                    tt.FinalPlacement!.Value,
                    tt.FinalPlacement == 1 ? TeamStatus.Champion : TeamStatus.Eliminated,
                    tt.EliminatedInRound ?? 0,
                    groupNameLookup.GetValueOrDefault(tt.TeamId)
                ))
                .ToList();
        }

        // Team id -> group name, for teams whose placement is their group position
        private async Task<Dictionary<long, string>> GetGroupPlacementNamesAsync(Tournament tournament)
        {
            if (tournament.Format == Format.BracketOnly)
                return new Dictionary<long, string>();

            var groups = await _standingRepository.FindAllAsync(s => s.TournamentId == tournament.Id && s.StandingType == StandingType.Group);
            var groupNames = groups.ToDictionary(g => g.Id, g => g.Name);
            var entries = await _groupRepository.GetByTournamentIdAsync(tournament.Id);

            // GroupsOnly: every team; GroupsAndBracket: only teams knocked out in the groups
            return entries
                .Where(e => tournament.Format == Format.GroupsOnly || e.Eliminated)
                .ToDictionary(e => e.TeamId, e => groupNames.GetValueOrDefault(e.StandingId, ""));
        }
    }
}
