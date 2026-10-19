using Application.Contracts;
using Application.Contracts.Repositories;
using Application.DTOs.Standing;
using Application.DTOs.Team;
using Application.DTOs.Tournament;
using Application.Mappings;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class StandingService : IStandingService
    {
        private readonly IStandingRepository _standingRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IRepository<Team> _teamRepository;
        private readonly IRepository<TournamentTeam> _tournamentTeamRepository;
        private readonly IFormatService _formatService;
        private readonly ILogger<StandingService> _logger;

        public StandingService(IStandingRepository standingRepository,
                               IMatchRepository matchRepository,
                               IRepository<Tournament> tournamentRepository,
                               IGroupRepository groupRepository,
                               IRepository<Team> teamRepository,
                               IRepository<TournamentTeam> tournamentTeamRepository,
                               IFormatService formatService,
                               ILogger<StandingService> logger)
        {
            _standingRepository = standingRepository;
            _matchRepository = matchRepository;
            _tournamentRepository = tournamentRepository;
            _groupRepository = groupRepository;
            _teamRepository = teamRepository;
            _tournamentTeamRepository = tournamentTeamRepository;
            _formatService = formatService;
            _logger = logger;
        }

        // Setup

        public async Task InitializeStandingsForTournamentAsync(Tournament tournament, int? numberOfGroups)
        {
            var metadata = _formatService.GetFormatMetadata(tournament.Format);

            if (metadata.RequiresBracket)
            {
                int bracketSize = tournament.Format == Format.BracketOnly
                    ? NextPowerOfTwo(tournament.MaxTeams)
                    : NextPowerOfTwo(numberOfGroups!.Value * tournament.AdvancingPerGroup!.Value);

                var standing = new Standing("Main Bracket", bracketSize, StandingType.Bracket);
                standing.Tournament = tournament;
                await _standingRepository.AddAsync(standing);
            }

            if (metadata.RequiresGroups && numberOfGroups.HasValue)
            {
                int groups = numberOfGroups.Value;
                int baseSize = tournament.MaxTeams / groups;
                int extra = tournament.MaxTeams % groups;

                for (int i = 0; i < groups; i++)
                {
                    int groupSize = baseSize + (i < extra ? 1 : 0);
                    var standing = new Standing($"Group {i + 1}", groupSize, StandingType.Group);
                    standing.Tournament = tournament;
                    await _standingRepository.AddAsync(standing);
                }
            }
            // No SaveChangesAsync — caller controls persistence; FK resolved via navigation (tournament may be unsaved)
        }

        public async Task<List<Standing>> GetStandingsAsync(long tournamentId)
        {
            var standings = await _standingRepository.FindAllAsync(s => s.TournamentId == tournamentId);

            return standings.ToList();
        }

        private static int NextPowerOfTwo(int n)
        {
            if (n <= 1) return 2;
            int p = 1;
            while (p < n) p <<= 1;
            return p;
        }

        // Group progress

        public async Task<bool> IsGroupFinished(long standingId)
        {
            var standing = await _standingRepository.GetByIdAsync(standingId)
                ?? throw new NotFoundException("Standing", standingId);
            var matches = await _matchRepository.FindAllAsync(m => m.StandingId == standingId);
            bool standingFinished = false;

            if (matches.All(m => m.WinnerId is not null && m.LoserId is not null))
            {
                standingFinished = true;
            }

            return standingFinished;
        }

        public async Task MarkGroupAsFinished(long standingId)
        {
            var standing = await _standingRepository.GetByIdAsync(standingId)
                ?? throw new NotFoundException("Standing", standingId);

            standing.IsFinished = true;
        }

        public async Task FinalizeGroupTeams(long standingId)
        {
            var groupEntries = await _groupRepository.GetByStandingIdOrderedAsync(standingId);
            int position = 1;
            foreach (var entry in groupEntries)
            {
                entry.Status = position == 1 ? TeamStatus.Champion : TeamStatus.Eliminated;
                entry.Eliminated = position != 1;
                position++;
            }
        }

        public async Task<bool> CheckAllGroupsAreFinished(long tournamentId)
        {
            var allStandings = await _standingRepository.FindAllAsync(s => s.TournamentId == tournamentId);
            var allGroups = allStandings.Where(s => s.StandingType == StandingType.Group);

            bool allGroupsFinished = allGroups.Any() && allGroups.All(ag => ag.IsFinished);

            if (allGroupsFinished)
            {
                _logger.LogInformation($"ALL GROUPS FINISHED for tournament {tournamentId}!");
            }
            else
            {
                _logger.LogInformation($"Groups not all finished yet for tournament {tournamentId}.");
            }

            return allGroupsFinished;
        }

        // Bracket entry

        // NOTED N+1 PROBLEM EXAMPLE, LEAVE IT FOR EDUCATIONAL PURPOSE
        // var teams = await _teamRepository.FindAllAsync(t => teamIds.Contains(t.Id));
        public async Task<List<BracketEntrant>> GetTeamsForBracketByFormat(long tournamentId)
        {
            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId)
                ?? throw new NotFoundException("Tournament", tournamentId);

            var metadata = _formatService.GetFormatMetadata(tournament.Format);

            // BracketOnly: All registered teams
            if (!metadata.RequiresGroups)
            {
                _logger.LogInformation("Selecting all registered teams for BracketOnly tournament {Id}", tournamentId);

                var tournamentTeams = await _tournamentTeamRepository.FindAllAsync(tt => tt.TournamentId == tournamentId);
                var teamIds = tournamentTeams.Select(tt => tt.TeamId).ToList();

                var teams = await _teamRepository.FindAllAsync(t => teamIds.Contains(t.Id));

                _logger.LogInformation("{Count} registered teams selected for bracket", teams.Count);

                return teams.Select(t => new BracketEntrant(t, null, null)).ToList();
            }

            // GroupsAndBracket: Advanced teams from groups
            _logger.LogInformation("Selecting advanced teams from groups for tournament {Id}", tournamentId);

            var standings = await GetStandingsAsync(tournamentId);
            var groups = standings.Where(s => s.StandingType == StandingType.Group).ToList();
            if (groups.Count == 0)
                throw new InvalidOperationException($"No groups found for GroupsAndBracket tournament {tournamentId}");

            int teamsAdvancingPerGroup = tournament.AdvancingPerGroup
                ?? throw new InvalidOperationException("AdvancingPerGroup not set on tournament.");

            _logger.LogInformation("Teams advancing per group: {TeamsPerGroup} (Groups: {GroupCount})",
                teamsAdvancingPerGroup, groups.Count);

            var allGroupIds = groups.Select(g => g.Id).ToList();
            var allGroupEntries = await _groupRepository.FindAllAsync(g => allGroupIds.Contains(g.StandingId));

            // Advancing group entries with their placement in the group (1 = group winner)
            var advancing = new List<(GroupEntry Entry, int Placement)>();

            foreach (var group in groups)
            {
                var ranked = allGroupEntries
                    .Where(g => g.StandingId == group.Id)
                    .OrderByDescending(g => WinRate(g))
                    .ThenByDescending(g => g.Wins)
                    .ThenBy(g => g.Losses)
                    .ToList();

                var groupAdvancing = ranked.Take(teamsAdvancingPerGroup).ToList();
                groupAdvancing.ForEach(e => e.Status = TeamStatus.Advanced);
                advancing.AddRange(groupAdvancing.Select((entry, index) => (entry, index + 1)));

                ranked.Skip(teamsAdvancingPerGroup).ToList()
                    .ForEach(e => { e.Status = TeamStatus.Eliminated; e.Eliminated = true; });
            }

            // 1 query for all advancing teams
            var advancingTeamIds = advancing.Select(a => a.Entry.TeamId).ToList();
            var advancedTeams = await _teamRepository.FindAllAsync(t => advancingTeamIds.Contains(t.Id));

            var entrants = advancing
                .Select(a => new BracketEntrant(
                    advancedTeams.First(t => t.Id == a.Entry.TeamId),
                    a.Entry.StandingId,
                    a.Placement))
                .ToList();

            _logger.LogInformation("{Count} teams advanced from {GroupCount} groups", entrants.Count, groups.Count);
            return entrants;
        }

        private static double WinRate(GroupEntry e) =>
            e.Wins + e.Losses == 0 ? 0.0 : (double)e.Wins / (e.Wins + e.Losses);

        // Read models (UI)

        public async Task<List<GetGroupsWithDetailsResponseDTO>> GetGroupsWithDetailsAsync(long tournamentId)
        {
            var standings = await _standingRepository.GetGroupsWithMatchesAsync(tournamentId);

            var entriesByStanding = (await _groupRepository.FindAllAsync(g => g.TournamentId == tournamentId)).ToLookup(e => e.StandingId);

            return standings.Select(s => BuildGroupDTO(s, entriesByStanding[s.Id])).ToList();
        }

        public async Task<GetGroupsWithDetailsResponseDTO?> GetGroupWithDetailsByStandingIdAsync(long standingId)
        {
            var standing = await _standingRepository.GetGroupWithMatchesByStandingIdAsync(standingId);
            if (standing == null) return null;

            var entries = await _groupRepository.FindAllAsync(e => e.StandingId == standingId);

            return BuildGroupDTO(standing, entries);
        }

        public async Task<GetBracketWithDetailsResponseDTO?> GetBracketWithDetailsAsync(long tournamentId)
        {
            var bracketStanding = await _standingRepository.GetBracketWithMatchesAsync(tournamentId);

            if (bracketStanding == null) return null;

            var matches = bracketStanding.Matches.Select(m => m.ToStandingMatchDTO() with
            {
                TeamAWins = CalculateTeamWins(m, m.TeamAId),
                TeamBWins = CalculateTeamWins(m, m.TeamBId)
            }).ToList();

            return new GetBracketWithDetailsResponseDTO
            {
                Id = bracketStanding.Id,
                Name = bracketStanding.Name,
                IsFinished = bracketStanding.IsFinished,
                IsSeeded = bracketStanding.IsSeeded,
                Version = bracketStanding.Version,
                Matches = matches
            };
        }

        private int CalculateTeamWins(Match match, long? teamId)
        {
            return match.Games.Count(g => g.WinnerId == teamId);
        }

        private GetGroupsWithDetailsResponseDTO BuildGroupDTO(Standing standing, IEnumerable<GroupEntry> entries)
        {
            var groupEntries = entries
                .OrderByDescending(e => e.Points)
                .ThenByDescending(e => e.Wins)
                .ThenBy(e => e.Losses)
                .ToList();

            var matches = standing.Matches.Select(m => m.ToStandingMatchDTO() with
            {
                TeamAWins = CalculateTeamWins(m, m.TeamAId),
                TeamBWins = CalculateTeamWins(m, m.TeamBId)
            }).ToList();

            return new GetGroupsWithDetailsResponseDTO
            {
                Id = standing.Id,
                Name = standing.Name,
                IsFinished = standing.IsFinished,
                IsSeeded = standing.IsSeeded,
                Version = standing.Version,
                Teams = groupEntries.Select(e => e.ToGroupTeamDTO()).ToList(),
                Matches = matches
            };
        }
    }
}
