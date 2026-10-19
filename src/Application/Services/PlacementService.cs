using Application.Contracts;
using Application.Contracts.Repositories;
using Application.DTOs.Tournament;
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

            // Champion = winner of the final
            var finalMatch = matches.FirstOrDefault(m => m.Round == maxRound && m.Seed == 1);
            if (finalMatch?.WinnerId != null)
            {
                placements.Add((finalMatch.WinnerId.Value, 1, null));
                _logger.LogInformation($"  Team {finalMatch.WinnerId} = 1st place (Champion)");
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
                    _logger.LogInformation($"  Team {match.LoserId} = {rank} place (eliminated R{round})");
                }
            }

            _logger.LogInformation($"Calculated {placements.Count} final placements");
            return placements;
        }

        public async Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateGroupOnlyPlacements(long tournamentId)
        {
            _logger.LogInformation($"=== Calculating group-only placements for tournament {tournamentId} ===");

            var placements = new List<(long TeamId, int Placement, int? EliminatedInRound)>();

            var standings = await _standingRepository.FindAllAsync(s => s.TournamentId == tournamentId);
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
    }
}
