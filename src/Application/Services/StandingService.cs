using Application.Contracts;
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
        private readonly IRepository<Standing> _standingRepository;
        private readonly IRepository<Match> _matchRepository;
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly IRepository<Group> _groupRepository;
        private readonly IRepository<Team> _teamRepository;
        private readonly IRepository<TournamentTeam> _tournamentTeamRepository;
        private readonly ILogger<StandingService> _logger;
        private readonly IMapper _mapper;

        private readonly IUnitOfWork _unitOfWork;

        public StandingService(IRepository<Standing> standingRepository,
                               IRepository<Match> matchRepository,
                               IRepository<Tournament> tournamentRepository,
                               IRepository<Group> groupRepository,
                               IRepository<Team> teamRepository,
                               IRepository<TournamentTeam> tournamentTeamRepository,
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

        public async Task<List<Standing>> GetStandingsAsync(long tournamentId)
        {
            var standings = await _standingRepository.FindAllAsync(s => s.TournamentId == tournamentId);

            return standings.ToList();
        }

        public async Task<bool> IsGroupFinished(long standingId)
        {
            var standing = await _standingRepository.GetByIdAsync(standingId)
                ?? throw new NotFoundException("Standing", standingId);
            var matches = await _matchRepository.FindAllAsync(m => m.StandingId ==  standingId);
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
            await _standingRepository.UpdateAsync(standing);
        }

        public async Task<bool> CheckAllGroupsAreFinished(long tournamentId)
        {
            var allStandings = await _standingRepository.FindAllAsync(s => s.TournamentId ==  tournamentId);
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

        // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ MARK FOR REFACTOR
        public async Task<List<Team>> GetTeamsForBracket(long tournamentId)
        {
            var advancingTeams = new List<Team>();
            var standings = await GetStandingsAsync(tournamentId);
            var groups = standings.Where(s => s.StandingType == StandingType.Group).ToList();
            var bracket = standings.FirstOrDefault(s => s.StandingType == StandingType.Bracket)
                ?? throw new NotFoundException("Bracket standing not found");

            if (groups.Count == 0)
                throw new NotFoundException("No groups found");

            // Calculate how many teams advance per group
            int teamsAdvancingPerGroup = bracket.MaxTeams / groups.Count;

            _logger.LogInformation($"Teams advancing per group: {teamsAdvancingPerGroup} (Bracket: {bracket.MaxTeams}, Groups: {groups.Count})");

            foreach (var group in groups)
            {
                // Get group entries using repository and sort in memory
                var allGroupEntries = await _groupRepository.FindAllAsync(ge => ge.StandingId == group.Id);
                var groupEntries = allGroupEntries
                    .OrderByDescending(g => g.Points)
                    .ThenByDescending(g => g.Wins)
                    .ThenBy(g => g.Losses)
                    .ToList();

                // Top X teams advance
                var advancing = groupEntries.Take(teamsAdvancingPerGroup).ToList();
                var eliminated = groupEntries.Skip(teamsAdvancingPerGroup).ToList();

                foreach (var groupEntry in advancing)
                {
                    groupEntry.Status = TeamStatus.Advanced;
                    await _groupRepository.UpdateAsync(groupEntry);

                    // Fetch Team entity using repository
                    var team = await _teamRepository.GetByIdAsync(groupEntry.TeamId);

                    if (team != null)
                    {
                        advancingTeams.Add(team);
                        _logger.LogInformation($"Team {team.Name} advanced from {group.Name}");
                    }
                }

                foreach (var groupEntry in eliminated)
                {
                    groupEntry.Status = TeamStatus.Eliminated;
                    groupEntry.Eliminated = true;
                    await _groupRepository.UpdateAsync(groupEntry);
                    _logger.LogInformation($"Team {groupEntry.TeamName} eliminated from {group.Name}");
                }
            }

            return advancingTeams;
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
                return new List<TeamPlacementDTO>();
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

        public async Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateFinalPlacements(long standingId)
        {
            _logger.LogInformation($"=== Calculating final placements for standing {standingId} ===");

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

                    await _tournamentTeamRepository.UpdateAsync(tournamentTeam);
                    _logger.LogInformation($"  Team {placement.TeamId}: Placement {placement}, Eliminated Round {placement.EliminatedInRound.ToString() ?? "N/A"}");
                }

                _logger.LogInformation($"✓ Stored {placements.Count} final results");
            }
        }

        public async Task<List<GetTeamWithStatsResponseDTO>> GetTeamsWithStatsAsync(long standingId)
        {
            var participants = await _groupRepository.FindAllAsync(p => p.StandingId == standingId);

            return _mapper.Map<List<GetTeamWithStatsResponseDTO>>(participants);
        }
    }
}
