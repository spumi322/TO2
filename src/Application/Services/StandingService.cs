using Application.Contracts;
using Application.Contracts.Repositories;
using Application.DTOs.Standing;
using Application.DTOs.Team;
using Application.DTOs.Tournament;
using AutoMapper;
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
        private readonly IMapper _mapper;

        private readonly IUnitOfWork _unitOfWork;

        public StandingService(IStandingRepository standingRepository,
                               IMatchRepository matchRepository,
                               IRepository<Tournament> tournamentRepository,
                               IGroupRepository groupRepository,
                               IRepository<Team> teamRepository,
                               IRepository<TournamentTeam> tournamentTeamRepository,
                               IFormatService formatService,
                               ILogger<StandingService> logger,
                               IMapper mapper,
                               IUnitOfWork unitOfWork)
        {
            _standingRepository = standingRepository;
            _matchRepository = matchRepository;
            _tournamentRepository = tournamentRepository;
            _groupRepository = groupRepository;
            _teamRepository = teamRepository;
            _tournamentTeamRepository = tournamentTeamRepository;
            _formatService = formatService;
            _logger = logger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task GenerateStanding(long tournamentId, string name, StandingType type, int? teamsPerStanding)
        {
            var standing = new Standing(name, teamsPerStanding, type);
            standing.TournamentId = tournamentId;

            await _standingRepository.AddAsync(standing);
            await _unitOfWork.SaveChangesAsync();
        }

        private static int NextPowerOfTwo(int n)
        {
            if (n <= 1) return 2;
            int p = 1;
            while (p < n) p <<= 1;
            return p;
        }

        public async Task InitializeStandingsForTournamentAsync(
            long tournamentId, Format format, int maxTeams, int? numberOfGroups, int? advancingPerGroup)
        {
            var metadata = _formatService.GetFormatMetadata(format);

            if (metadata.RequiresBracket)
            {
                int bracketSize = format == Format.BracketOnly
                    ? NextPowerOfTwo(maxTeams)
                    : NextPowerOfTwo(numberOfGroups!.Value * advancingPerGroup!.Value);

                var standing = new Standing("Main Bracket", bracketSize, StandingType.Bracket);
                standing.TournamentId = tournamentId;
                await _standingRepository.AddAsync(standing);
            }

            if (metadata.RequiresGroups && numberOfGroups.HasValue)
            {
                int groups = numberOfGroups.Value;
                int baseSize = maxTeams / groups;
                int extra = maxTeams % groups;

                for (int i = 0; i < groups; i++)
                {
                    int groupSize = baseSize + (i < extra ? 1 : 0);
                    var standing = new Standing($"Group {i + 1}", groupSize, StandingType.Group);
                    standing.TournamentId = tournamentId;
                    await _standingRepository.AddAsync(standing);
                }
            }
            // No SaveChangesAsync — caller controls persistence
        }

        public async Task<List<Standing>> GetStandingsAsync(long tournamentId)
        {
            var standings = await _standingRepository.FindAllAsync(s => s.TournamentId == tournamentId);

            return standings.ToList();
        }

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

        private static double WinRate(GroupEntry e) =>
            e.Wins + e.Losses == 0 ? 0.0 : (double)e.Wins / (e.Wins + e.Losses);


        // NOTED N+1 PROBLEM EXAMPLE, LEAVE IT FOR EDUCATIONAL PURPOSE
        // var teams = await _teamRepository.FindAllAsync(t => teamIds.Contains(t.Id));
        public async Task<List<Team>> GetTeamsForBracketByFormat(long tournamentId)
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

                return teams.ToList();
            }

            // BracketAndGroup: Advanced teams from groups
            _logger.LogInformation("Selecting advanced teams from groups for tournament {Id}", tournamentId);

            var standings = await GetStandingsAsync(tournamentId);
            var groups = standings.Where(s => s.StandingType == StandingType.Group).ToList();
            if (groups.Count == 0)
                throw new NotFoundException("No groups found for BracketAndGroup tournament");

            int teamsAdvancingPerGroup = tournament.AdvancingPerGroup
                ?? throw new InvalidOperationException("AdvancingPerGroup not set on tournament.");

            _logger.LogInformation("Teams advancing per group: {TeamsPerGroup} (Groups: {GroupCount})",
                teamsAdvancingPerGroup, groups.Count);

            var allGroupIds = groups.Select(g => g.Id).ToList();
            var allGroupEntries = await _groupRepository.FindAllAsync(g => allGroupIds.Contains(g.StandingId));

            foreach (var group in groups)
            {
                var ranked = allGroupEntries
                    .Where(g => g.StandingId == group.Id)
                    .OrderByDescending(g => WinRate(g))
                    .ThenByDescending(g => g.Wins)
                    .ThenBy(g => g.Losses)
                    .ToList();

                ranked.Take(teamsAdvancingPerGroup).ToList()
                    .ForEach(e => e.Status = TeamStatus.Advanced);

                ranked.Skip(teamsAdvancingPerGroup).ToList()
                    .ForEach(e => { e.Status = TeamStatus.Eliminated; e.Eliminated = true; });
            }

            // 1 query for all advancing teams
            var advancingTeamIds = allGroupEntries
                .Where(e => e.Status == TeamStatus.Advanced)
                .Select(e => e.TeamId)
                .ToList();

            var advancedTeams = await _teamRepository.FindAllAsync(t => advancingTeamIds.Contains(t.Id));

            _logger.LogInformation("{Count} teams advanced from {GroupCount} groups", advancedTeams.Count, groups.Count);
            return advancedTeams.ToList();
        }

        public async Task<List<TeamPlacementDTO>> GetFinalResultsAsync(long tournamentId)
        {
            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId)
                ?? throw new NotFoundException("Tournament", tournamentId);

            if (tournament.Status != TournamentStatus.Finished)
            {
                return new List<TeamPlacementDTO>();
            }

            var bracketStanding = (await GetStandingsAsync(tournamentId))
                .FirstOrDefault(s => s.StandingType == StandingType.Bracket);

            if (bracketStanding == null)
            {
                // No bracket (GroupsOnly) — read from stored TournamentTeam records
                return await GetFinalResultsFromTournamentTeams(tournamentId);
            }

            // Get all bracket matches using repository
            var allMatches = await _matchRepository.FindAllAsync(m => m.StandingId == bracketStanding.Id);
            if (!allMatches.Any())
            {
                return new List<TeamPlacementDTO>();
            }

            // Extract unique team IDs from matches
            var teamIds = allMatches
                .SelectMany(m => new[] { m.TeamAId, m.TeamBId })
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToHashSet();

            // Get all teams from repository and filter to bracket participants
            var allTeams = await _teamRepository.GetAllAsync();
            var bracketTeams = allTeams.Where(t => teamIds.Contains(t.Id)).ToList();
            var teamNameLookup = bracketTeams.ToDictionary(t => t.Id, t => t.Name);

            // Calculate total rounds
            int totalRounds = allMatches.Max(m => m.Round ?? 0);

            // Find the final match (last round, seed 1)
            var finalMatch = allMatches.FirstOrDefault(m => m.Round == totalRounds && m.Seed == 1);
            if (finalMatch == null || !finalMatch.WinnerId.HasValue)
            {
                return new List<TeamPlacementDTO>();
            }

            // Build standings by determining elimination round for each team
            var teamPlacements = new List<(long TeamId, string TeamName, int EliminationRound, TeamStatus Status)>();

            foreach (var teamId in teamIds)
            {
                // Find the match where this team lost (if any)
                var lossMatch = allMatches.FirstOrDefault(m => m.LoserId == teamId);

                var teamName = teamNameLookup.ContainsKey(teamId) ? teamNameLookup[teamId] : "Unknown Team";

                if (lossMatch == null)
                {
                    // No loss match = Champion
                    teamPlacements.Add((teamId, teamName, totalRounds + 1, TeamStatus.Champion));
                }
                else
                {
                    // Team was eliminated in the round they lost
                    int eliminationRound = lossMatch.Round ?? 0;
                    teamPlacements.Add((teamId, teamName, eliminationRound, TeamStatus.Eliminated));
                }
            }

            // Sort by elimination round (higher = better placement)
            // Then by TeamId for tie-breaking within same round
            var sortedTeams = teamPlacements
                .OrderByDescending(t => t.EliminationRound)
                .ThenBy(t => t.TeamId)
                .ToList();

            // Assign placements with tied ranks
            var standings = new List<TeamPlacementDTO>();
            int currentPlacement = 1;
            int? lastEliminationRound = null;
            int teamsAtCurrentRank = 0;

            foreach (var team in sortedTeams)
            {
                if (lastEliminationRound.HasValue && team.EliminationRound < lastEliminationRound.Value)
                {
                    // New elimination round = new placement (skip tied ranks)
                    currentPlacement += teamsAtCurrentRank;
                    teamsAtCurrentRank = 0;
                }

                standings.Add(new TeamPlacementDTO(
                    team.TeamId,
                    team.TeamName,
                    currentPlacement,
                    team.Status,
                    team.EliminationRound
                ));

                lastEliminationRound = team.EliminationRound;
                teamsAtCurrentRank++;
            }

            return standings;
        }

        private async Task<List<TeamPlacementDTO>> GetFinalResultsFromTournamentTeams(long tournamentId)
        {
            var tournamentTeams = await _tournamentTeamRepository.FindAllAsync(
                tt => tt.TournamentId == tournamentId && tt.FinalPlacement != null);

            var teamIds = tournamentTeams.Select(tt => tt.TeamId).ToHashSet();
            var allTeams = await _teamRepository.GetAllAsync();
            var teamNameLookup = allTeams.Where(t => teamIds.Contains(t.Id)).ToDictionary(t => t.Id, t => t.Name);

            return tournamentTeams
                .OrderBy(tt => tt.FinalPlacement)
                .ThenBy(tt => tt.TeamId)
                .Select(tt => new TeamPlacementDTO(
                    tt.TeamId,
                    teamNameLookup.GetValueOrDefault(tt.TeamId, "Unknown Team"),
                    tt.FinalPlacement!.Value,
                    tt.FinalPlacement == 1 ? TeamStatus.Champion : TeamStatus.Eliminated,
                    tt.EliminatedInRound ?? 0
                ))
                .ToList();
        }

        public async Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateBracketPlacements(long standingId)
        {
            _logger.LogInformation($"=== Calculating bracket placements for standing {standingId} ===");

            var placements = new List<(long TeamId, int Placement, int? EliminatedInRound)>();

            // Get all matches for this bracket
            var allMatches = await _matchRepository.FindAllAsync(m => m.StandingId == standingId);
            var matches = allMatches.OrderByDescending(m => m.Round).ThenBy(m => m.Seed).ToList();

            if (!matches.Any())
            {
                _logger.LogWarning("No matches found for standing");
                return placements;
            }

            // Find max round (final)
            int maxRound = matches.Max(m => m.Round ?? 0);
            _logger.LogInformation($"Max round: {maxRound}");

            // Calculate placement for each round, starting from final
            int currentPlacement = 1;

            for (int round = maxRound; round >= 1; round--)
            {
                var roundMatches = matches.Where(m => m.Round == round).OrderBy(m => m.Seed).ToList();
                int teamsEliminatedThisRound = roundMatches.Count;

                _logger.LogInformation($"Round {round}: {teamsEliminatedThisRound} matches");

                foreach (var match in roundMatches)
                {
                    if (round == maxRound && match.Seed == 1)
                    {
                        // Final match - special handling
                        if (match.WinnerId.HasValue)
                        {
                            // Champion (1st place)
                            placements.Add((match.WinnerId.Value, 1, null));
                            _logger.LogInformation($"  Team {match.WinnerId} = 1st place (Champion)");
                        }

                        if (match.LoserId.HasValue)
                        {
                            // Runner-up (2nd place)
                            placements.Add((match.LoserId.Value, 2, maxRound));
                            _logger.LogInformation($"  Team {match.LoserId} = 2nd place (Runner-up, eliminated R{maxRound})");
                        }

                        currentPlacement = 3; // Next placements start from 3rd
                    }
                    else
                    {
                        // Non-final matches - losers get placed based on elimination round
                        if (match.LoserId.HasValue)
                        {
                            placements.Add((match.LoserId.Value, currentPlacement, round));
                            _logger.LogInformation($"  Team {match.LoserId} = {currentPlacement} place (eliminated R{round})");
                            currentPlacement++;
                        }
                    }
                }
            }

            _logger.LogInformation($"Calculated {placements.Count} final placements");
            return placements;
        }

        public async Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateGroupOnlyPlacements(long tournamentId)
        {
            _logger.LogInformation($"=== Calculating group-only placements for tournament {tournamentId} ===");

            var placements = new List<(long TeamId, int Placement, int? EliminatedInRound)>();

            var standings = await GetStandingsAsync(tournamentId);
            var groupStandings = standings.Where(s => s.StandingType == StandingType.Group).ToList();

            foreach (var group in groupStandings)
            {
                var teams = await _groupRepository.GetByStandingIdOrderedAsync(group.Id);
                int position = 1;

                foreach (var team in teams)
                {
                    placements.Add((team.TeamId, position, null));

                    // Set group team status: 1st = Champion, rest = Eliminated
                    team.Status = position == 1 ? TeamStatus.Champion : TeamStatus.Eliminated;
                    team.Eliminated = position != 1;

                    _logger.LogInformation($"  {group.Name}: Team {team.TeamName} = position {position}, status {team.Status}");
                    position++;
                }
            }

            _logger.LogInformation($"Calculated {placements.Count} group-only placements");
            return placements;
        }

        public async Task SetFinalResults(long tournamentId, List<(long TeamId, int Placement, int? EliminatedInRound)> placements)
        {
            _logger.LogInformation($"=== Storing final results for tournament {tournamentId} ===");

            var now = DateTime.UtcNow;

            var tournamentTeams = await _tournamentTeamRepository.FindAllAsync(tt => tt.TournamentId == tournamentId);

            foreach (var placement in placements)
            {
                {
                    // Get TournamentTeam record
                    var tournamentTeam = tournamentTeams.FirstOrDefault(tt => tt.TeamId == placement.TeamId);

                    if (tournamentTeam == null)
                    {
                        _logger.LogWarning($"TournamentTeam not found for Team {placement.TeamId} in Tournament {tournamentId}");
                        continue;
                    }

                    // Update final results
                    tournamentTeam.FinalPlacement = placement.Placement;
                    tournamentTeam.EliminatedInRound = placement.EliminatedInRound;
                    tournamentTeam.ResultFinalizedAt = now;

                    _logger.LogInformation($"  Team {placement.TeamId}: Placement {placement}, Eliminated Round {placement.EliminatedInRound.ToString() ?? "N/A"}");
                }

                _logger.LogInformation($"✓ Stored {placements.Count} final results");
            }
        }

        private int CalculateTeamWins(Match match, long? teamId)
        {
            return match.Games.Count(g => g.WinnerId == teamId);
        }

        public async Task<List<GetGroupsWithDetailsResponseDTO>> GetGroupsWithDetailsAsync(long tournamentId)
        {
            var standings = await _standingRepository.GetGroupsWithMatchesAsync(tournamentId);

            var result = new List<GetGroupsWithDetailsResponseDTO>();

            foreach (var standing in standings)
            {
                var groupEntries = await _groupRepository.GetByStandingIdOrderedAsync(standing.Id);

                var matches = standing.Matches.Select(m =>
                {
                    var dto = _mapper.Map<StandingMatchDTO>(m);
                    return dto with
                    {
                        TeamAWins = CalculateTeamWins(m, m.TeamAId),
                        TeamBWins = CalculateTeamWins(m, m.TeamBId)
                    };
                }).ToList();

                var dto = new GetGroupsWithDetailsResponseDTO
                {
                    Id = standing.Id,
                    Name = standing.Name,
                    IsFinished = standing.IsFinished,
                    IsSeeded = standing.IsSeeded,
                    Teams = _mapper.Map<List<GroupTeamDTO>>(groupEntries),
                    Matches = matches
                };

                result.Add(dto);
            }

            return result;
        }

        public async Task<GetBracketWithDetailsResponseDTO?> GetBracketWithDetailsAsync(long tournamentId)
        {
            var bracketStanding = await _standingRepository.GetBracketWithMatchesAsync(tournamentId);

            if (bracketStanding == null) return null;

            var matches = bracketStanding.Matches.Select(m =>
            {
                var dto = _mapper.Map<StandingMatchDTO>(m);
                return dto with
                {
                    TeamAWins = CalculateTeamWins(m, m.TeamAId),
                    TeamBWins = CalculateTeamWins(m, m.TeamBId)
                };
            }).ToList();

            return new GetBracketWithDetailsResponseDTO
            {
                Id = bracketStanding.Id,
                Name = bracketStanding.Name,
                IsFinished = bracketStanding.IsFinished,
                IsSeeded = bracketStanding.IsSeeded,
                Matches = matches
            };
        }
    }
}
