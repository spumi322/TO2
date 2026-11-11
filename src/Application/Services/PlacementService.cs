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
        private readonly IRepository<Match> _matchRepository;
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IRepository<Team> _teamRepository;
        private readonly ILogger<PlacementService> _logger;

        public PlacementService(IStandingRepository standingRepository,
                                IRepository<Match> matchRepository,
                                IRepository<Tournament> tournamentRepository,
                                IGroupRepository groupRepository,
                                IRepository<Team> teamRepository,
                                ILogger<PlacementService> logger)
        {
            _standingRepository = standingRepository;
            _matchRepository = matchRepository;
            _tournamentRepository = tournamentRepository;
            _groupRepository = groupRepository;
            _teamRepository = teamRepository;
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
                // Placement = position in the group
                var teams = GroupRanking.Rank(
                    await _groupRepository.GetByStandingIdAsync(group.Id),
                    await _matchRepository.FindAllAsync(m => m.StandingId == group.Id));
                placements.AddRange(teams.Select((team, index) => (team.TeamId, index + 1, (int?)null)));
            }

            return placements;
        }

        public async Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateGroupStageKnockoutPlacements(long tournamentId)
        {
            var entries = await _groupRepository.GetByTournamentIdAsync(tournamentId);
            var groupIds = entries.Select(e => e.StandingId).Distinct().ToList();
            var matches = (await _matchRepository.FindAllAsync(m => groupIds.Contains(m.StandingId))).ToList();

            // Group position of each knocked-out team (the status was set when the bracket started)
            var knockedOut = entries
                .GroupBy(e => e.StandingId)
                .SelectMany(group => GroupRanking.Rank(group, matches.Where(m => m.StandingId == group.Key)).Select((entry, index) => (Entry: entry, GroupPosition: index + 1)))
                .Where(x => x.Entry.Status == TeamStatus.Eliminated)
                .ToList();

            var placements = new List<(long TeamId, int Placement, int? EliminatedInRound)>();

            // Below every team that reached the bracket; teams with the same group position share a rank
            int rank = entries.Count(e => e.Status != TeamStatus.Eliminated) + 1;

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
            var teams = await _teamRepository.FindAllAsync(t => t.TournamentId == tournamentId);

            foreach (var placement in placements)
            {
                var team = teams.FirstOrDefault(t => t.Id == placement.TeamId);

                if (team == null)
                {
                    _logger.LogWarning("Team {TeamId} is not registered in tournament {TournamentId}, placement {Placement} not stored",
                        placement.TeamId, tournamentId, placement.Placement);
                    continue;
                }

                team.SetFinalResult(placement.Placement, placement.EliminatedInRound);
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
            var teams = (await _teamRepository.FindAllAsync(t => t.TournamentId == tournamentId))
                .Where(t => t.FinalPlacement != null)
                .ToList();

            var groupNameLookup = await GetGroupPlacementNamesAsync(tournament);

            return teams
                .OrderBy(t => t.FinalPlacement)
                .ThenBy(t => t.Id)
                .Select(t => new TeamPlacementDTO(
                    t.Id,
                    t.Name,
                    t.FinalPlacement!.Value,
                    t.EliminatedInRound ?? 0,
                    groupNameLookup.GetValueOrDefault(t.Id)
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
                .Where(e => tournament.Format == Format.GroupsOnly || e.Status == TeamStatus.Eliminated)
                .ToDictionary(e => e.TeamId, e => groupNames.GetValueOrDefault(e.StandingId, ""));
        }
    }
}
