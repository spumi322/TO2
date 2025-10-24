using Application.Contracts;
using Application.DTOs.Game;
using Application.DTOs.Match;
using Application.DTOs.Orchestration;
using Application.DTOs.Standing;
using Application.DTOs.Tournament;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.StateMachine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Group = Domain.Entities.Group;

namespace Application.Services
{
    /// <summary>
    /// Explicit state machine for tournament lifecycle management.
    /// Replaces domain event-based implicit state transitions with clear, testable methods.
    /// </summary>
    public class OrchestrationService : IOrchestrationService
    {
        private readonly ILogger<OrchestrationService> _logger;
        private readonly IStandingService _standingService;
        private readonly IMatchService _matchService;
        private readonly IGameService _gameService;
        private readonly IGenericRepository<Tournament> _tournamentRepository;
        private readonly IGenericRepository<Standing> _standingRepository;
        private readonly IGenericRepository<Bracket> _bracketRepository;
        private readonly IGenericRepository<Match> _matchRepository;
        private readonly IGenericRepository<TournamentTeam> _tournamentTeamRepository;
        private readonly IGenericRepository<Group> _groupRepository;
        private readonly IGenericRepository<Team> _teamRepository;
        private readonly ITournamentStateMachine _stateMachine;
        private readonly ITournamentService _tournamentService;

        public OrchestrationService(
            ILogger<OrchestrationService> logger,
            IStandingService standingService,
            IMatchService matchService,
            IGameService gameService,
            IGenericRepository<Tournament> tournamentRepository,
            IGenericRepository<Standing> standingRepository,
            IGenericRepository<Bracket> bracketRepository,
            IGenericRepository<Match> matchRepository,
            IGenericRepository<TournamentTeam> tournamentTeamRepository,
            IGenericRepository<Group> groupRepository,
            IGenericRepository<Team> teamRepository,
            ITournamentStateMachine stateMachine,
            ITournamentService tournamentService
            )
        {
            _logger = logger;
            _standingService = standingService;
            _matchService = matchService;
            _gameService = gameService;
            _tournamentRepository = tournamentRepository;
            _standingRepository = standingRepository;
            _bracketRepository = bracketRepository;
            _matchRepository = matchRepository;
            _tournamentTeamRepository = tournamentTeamRepository;
            _groupRepository = groupRepository;
            _teamRepository = teamRepository;
            _stateMachine = stateMachine;
            _tournamentService = tournamentService;
        }

        public async Task<GameProcessResultDTO> LEGACY_ProcessGameResult(SetGameResultDTO gameResult)
        {
            _logger.LogInformation("Processing game result for GameId: {GameId}, WinnerId: {WinnerId}",
                gameResult.gameId, gameResult.WinnerId);

            try
            {
                var tournament = await _tournamentRepository.Get(gameResult.TournamentId)
                    ?? throw new Exception($"Tournament with id: {gameResult.TournamentId} was not found!");
                var matchId = gameResult.MatchId;
                // 1. Set game result (writes score into Game table)
                await _gameService.SetGameResult(
                    gameResult.gameId,
                    gameResult.WinnerId,
                    gameResult.TeamAScore,
                    gameResult.TeamBScore
                );

                _logger.LogInformation("Game result set for GameId: {GameId}, MatchId: {MatchId}",
                    gameResult.gameId, matchId);
                // 2. Check if match has a winner (counts wins per team)
                var matchWinner = await _gameService.SetMatchWinner(matchId);
                // If no winner yet, game was scored but match not finished
                if (matchWinner is null)
                {
                    _logger.LogInformation("Match {MatchId} still in progress (no winner yet)", matchId);
                    return new GameProcessResultDTO(
                        Success: true,
                        MatchFinished: false,
                        Message: "Game result recorded. Match still in progress."
                    );
                }

                _logger.LogInformation("Match {MatchId} finished. Winner: {WinnerId}, Loser: {LoserId}",
                    matchId, matchWinner.WinnerId, matchWinner.LoserId);

                // 3. Match finished - update standing entries (Group or Bracket tables)
                // Note: UpdateStandingEntries now handles its own Save() via repository pattern
                var standingType = await _gameService.UpdateStandingEntries(
                    gameResult.StandingId,
                    matchWinner.WinnerId,
                    matchWinner.LoserId
                );

                // 3a. BRACKET-SPECIFIC: Handle bracket progression
                if (standingType == StandingType.Bracket)
                {

                    // Check if this was the final match
                    bool isFinal = await IsFinalMatch(matchId, gameResult.StandingId);

                    if (isFinal)
                    {
                        // Validate state transition
                        _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.Finished);

                        // Transition to Finished
                        tournament.Status = TournamentStatus.Finished;

                        await _tournamentRepository.Update(tournament);
                        await _tournamentRepository.Save();

                        _logger.LogInformation($"✓ Tournament {tournament.Name} FINISHED.");

                        var placements = await _tournamentService.CalculateFinalPlacements(gameResult.StandingId);
                        await _tournamentService.SetFinalResults(tournament.Id, placements);
                        var finalResult = await _tournamentService.GetFinalResults(tournament.Id);

                        return new GameProcessResultDTO(
                            Success: true,
                            MatchFinished: true,
                            MatchWinnerId: matchWinner.WinnerId,
                            MatchLoserId: matchWinner.LoserId,
                            TournamentFinished: true,
                            NewTournamentStatus: TournamentStatus.Finished,
                            FinalStandings: finalResult,
                            Message: $"TOURNAMENT FINISHED! Champion: Team {matchWinner.WinnerId}"
                        );
                    }
                    else
                    {
                        // Not the final match - winner advanced to next round
                        await AdvanceWinnerToNextRound(matchId, matchWinner.WinnerId, gameResult.StandingId);

                        return new GameProcessResultDTO(
                            Success: true,
                            MatchFinished: true,
                            MatchWinnerId: matchWinner.WinnerId,
                            MatchLoserId: matchWinner.LoserId,
                            Message: $"Match completed. Winner advanced to next round."
                        );
                    }
                }

                // 4. Check if this match finishing caused the standing to finish (GROUP-SPECIFIC)
                bool standingJustFinished = await _standingService.CheckAndMarkStandingAsFinished(gameResult.StandingId);

                if (!standingJustFinished) 
                    return new GameProcessResultDTO(
                        Success: true,
                        MatchFinished: true,
                        MatchWinnerId: matchWinner.WinnerId,
                        MatchLoserId: matchWinner.LoserId,
                        Message: "Match completed."
                    );

                _logger.LogInformation("Standing finished for TournamentId: {TournamentId}",
                    gameResult.TournamentId);

                // 5. Check if ALL groups are finished (triggers transition to GroupsCompleted)
                bool allGroupsFinished = await _standingService.CheckAllGroupsAreFinished(gameResult.TournamentId);

                if (!allGroupsFinished)
                {
                    return new GameProcessResultDTO(
                        Success: true,
                        MatchFinished: true,
                        MatchWinnerId: matchWinner.WinnerId,
                        MatchLoserId: matchWinner.LoserId,
                        StandingFinished: true,
                        Message: "Match completed and standing finished. Other groups still in progress."
                    );
                }

                _logger.LogInformation("All groups finished for TournamentId: {TournamentId}. Transitioning to GroupsCompleted.",
                    gameResult.TournamentId);

                // 6. All groups finished - validate and transition tournament status

                _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.GroupsCompleted);
                tournament.Status = TournamentStatus.GroupsCompleted;

                await _tournamentRepository.Update(tournament);
                await _tournamentRepository.Save();

                _logger.LogInformation("Tournament {TournamentId} transitioned to GroupsCompleted status",
                    gameResult.TournamentId);

                // 7. Return full result with state transition information
                return new GameProcessResultDTO(
                    Success: true,
                    MatchFinished: true,
                    MatchWinnerId: matchWinner.WinnerId,
                    MatchLoserId: matchWinner.LoserId,
                    StandingFinished: true,
                    AllGroupsFinished: true,
                    NewTournamentStatus: TournamentStatus.GroupsCompleted,
                    Message: "All groups finished! Tournament transitioned to GroupsCompleted status. Admin can now seed bracket."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing game result for GameId: {GameId}. Error: {Message}",
                    gameResult.gameId, ex.Message);

                return new GameProcessResultDTO(
                    Success: false,
                    MatchFinished: false,
                    Message: $"Failed to process game result: {ex.Message}"
                );
            }
        }

        public async Task<StartGroupsResponseDTO> StartGroups(long tournamentId)
        {
            var tournament = await _tournamentRepository.Get(tournamentId) ?? throw new Exception("Tournament not found");

            try
            {
                // 1. Validate and transition to SeedingGroups
                _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.SeedingGroups);
                tournament.Status = TournamentStatus.SeedingGroups;
                tournament.IsRegistrationOpen = false; // Close registration when starting groups
                await _tournamentRepository.Update(tournament);
                await _tournamentRepository.Save();

                // 2. Seed groups
                var result = await SeedGroups(tournamentId);
                if (!result.Success)
                {
                    throw new Exception(result.Response);
                }

                // 3. Validate and transition to GroupsInProgress
                _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.GroupsInProgress);
                tournament.Status = TournamentStatus.GroupsInProgress;
                await _tournamentRepository.Update(tournament);
                await _tournamentRepository.Save();

                _logger.LogInformation($"Tournament {tournament.Id} group stage started successfully.");

                return new StartGroupsResponseDTO(true, "Group stage started successfully", tournament.Status);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Invalid state transition: {ex.Message}");
                return new StartGroupsResponseDTO(false, ex.Message, tournament.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting groups: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<SeedGroupsResponseDTO> SeedGroups(long tournamentId)
        {
            _logger.LogInformation($"=== Starting group seeding for tournament {tournamentId} ===");

            try
            {
                // 1. Validate tournament and check if standings are already seeded
                var tournament = await _tournamentRepository.Get(tournamentId);
                if (tournament == null)
                {
                    _logger.LogWarning($"Tournament {tournamentId} not found");
                    return new SeedGroupsResponseDTO(false, "Tournament not found");
                }

                var standings = await _standingService.GetStandingsAsync(tournamentId);
                if (standings.Any(standing => standing.IsSeeded))
                {
                    _logger.LogWarning("Standings are already seeded");
                    return new SeedGroupsResponseDTO(false, "Standings are already seeded!");
                }

                // 2. Split teams evenly into groups
                var groupStandings = standings.Where(s => s.StandingType == StandingType.Group).ToList();
                if (groupStandings.Count == 0)
                {
                    _logger.LogWarning("No group standings found");
                    return new SeedGroupsResponseDTO(false, "No group standings found for this tournament");
                }

                // Get all TournamentTeam records using repository
                var tournamentTeams = await _tournamentTeamRepository.GetAllByFK("TournamentId", tournamentId);

                // Get Team entities for each TournamentTeam
                var teams = new List<Team>();
                foreach (var tt in tournamentTeams)
                {
                    var team = await _teamRepository.Get(tt.TeamId);
                    if (team != null)
                    {
                        teams.Add(team);
                    }
                }

                if (teams.Count < groupStandings.Count)
                {
                    _logger.LogWarning($"Not enough teams ({teams.Count}) for {groupStandings.Count} groups");
                    return new SeedGroupsResponseDTO(false, "Not enough teams to seed the groups!");
                }

                var groupAssignments = ShuffleTeamsIntoEqualGroups(teams, groupStandings);
                _logger.LogInformation($"Distributed {teams.Count} teams across {groupStandings.Count} groups");

                // 3. Create GroupEntry records for each team
                foreach (var (standing, teamsInGroup) in groupAssignments)
                {
                    foreach (var team in teamsInGroup)
                    {
                        try
                        {
                            // Get all group entries for this tournament and filter in memory
                            var allGroupEntries = await _groupRepository.GetAllByFK("TournamentId", tournamentId);
                            var existingEntry = allGroupEntries.FirstOrDefault(ge => ge.TeamId == team.Id);

                            if (existingEntry != null)
                            {
                                existingEntry.StandingId = standing.Id;
                                existingEntry.Status = TeamStatus.Competing;
                                await _groupRepository.Update(existingEntry);
                                _logger.LogInformation($"Updated GroupEntry for team {team.Name} in {standing.Name}");
                            }
                            else
                            {
                                var groupEntry = new Group(tournamentId, standing.Id, team.Id, team.Name);

                                await _groupRepository.Add(groupEntry);
                                _logger.LogInformation($"Created GroupEntry for team {team.Name} in {standing.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to create/update GroupEntry for team {team.Id}: {ex.Message}");
                            return new SeedGroupsResponseDTO(false, $"Failed to create group entry for team {team.Name}");
                        }
                    }
                }

                // 4. Generate matches and games for each group (round-robin)
                bool allMatchesGenerated = true;
                foreach (var (standing, teamsInGroup) in groupAssignments)
                {
                    _logger.LogInformation($"Generating round-robin matches for {standing.Name} with {teamsInGroup.Count} teams");

                    for (int i = 0; i < teamsInGroup.Count; i++)
                    {
                        for (int j = i + 1; j < teamsInGroup.Count; j++)
                        {
                            try
                            {
                                var result = await _matchService.GenerateMatch(
                                    teamsInGroup[i],
                                    teamsInGroup[j],
                                    round: i + 1,
                                    seed: j,
                                    standingId: standing.Id
                                );

                                if (!result.Success)
                                {
                                    _logger.LogError($"Failed to generate match: {result.Message}");
                                    allMatchesGenerated = false;
                                }
                                else
                                {
                                    _logger.LogInformation($"Generated match: {teamsInGroup[i].Name} vs {teamsInGroup[j].Name}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error generating match between teams {teamsInGroup[i].Id} and {teamsInGroup[j].Id}: {ex.Message}");
                                allMatchesGenerated = false;
                            }
                        }
                    }

                    // 5. Mark standing as seeded if all matches were generated
                    if (allMatchesGenerated)
                    {
                        standing.IsSeeded = true;
                        await _standingRepository.Update(standing);
                        _logger.LogInformation($"Marked {standing.Name} as seeded");
                    }
                }

                if (!allMatchesGenerated)
                {
                    _logger.LogWarning("Some matches failed to generate");
                    return new SeedGroupsResponseDTO(false, "Some matches failed to generate");
                }

                // 6. Save all changes using repositories
                await _groupRepository.Save();
                await _standingRepository.Save();
                _logger.LogInformation("✓ Group seeding completed successfully");

                return new SeedGroupsResponseDTO(true, "Groups seeded successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error seeding groups for tournament {tournamentId}: {ex.Message}");
                return new SeedGroupsResponseDTO(false, $"Group seeding failed: {ex.Message}");
            }
        }

        private Dictionary<Standing, List<Team>> ShuffleTeamsIntoEqualGroups(List<Team> teams, List<Standing> groupStandings)
        {
            // Shuffle teams randomly
            var shuffledTeams = teams.OrderBy(t => Guid.NewGuid()).ToList();

            int teamsPerGroup = shuffledTeams.Count / groupStandings.Count;
            int remainingTeams = shuffledTeams.Count % groupStandings.Count;
            int teamIndex = 0;

            var assignments = new Dictionary<Standing, List<Team>>();

            for (int i = 0; i < groupStandings.Count; i++)
            {
                int groupSize = teamsPerGroup + (i < remainingTeams ? 1 : 0);
                var teamsForGroup = shuffledTeams.GetRange(teamIndex, groupSize);
                assignments[groupStandings[i]] = teamsForGroup;
                teamIndex += groupSize;

                _logger.LogInformation($"Assigned {groupSize} teams to {groupStandings[i].Name}");
            }

            return assignments;
        }

        public async Task<StartBracketResponseDTO> StartBracket(long tournamentId)
        {
            var tournament = await _tournamentRepository.Get(tournamentId) ?? throw new Exception("Tournament not found!");

            try
            {
                //1. Validate and transition from GroupsCompleted to SeedingBracket
                var standings = await _standingRepository.GetAllByFK("TournamentId", tournamentId);
                var bracket = standings.Where(s => s.StandingType == StandingType.Bracket).ToList();

                if (bracket.Any(b => b.IsSeeded))
                {
                    _logger.LogWarning("Bracket is already seeded!");
                    return new StartBracketResponseDTO(false, "Bracket is already seeded!", tournament.Status);
                }

                _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.SeedingBracket);
                tournament.Status = TournamentStatus.SeedingBracket;
                
                await _tournamentRepository.Update(tournament);
                await _tournamentRepository.Save();

                _logger.LogInformation("The tournament is ready to seed the bracket!");
                //2. Seed Bracket
                var teams = await _standingService.GetTeamsForBracket(tournamentId);

                var result = await SeedBracket(tournamentId, teams);
                if (!result.Success)
                {
                    throw new Exception(result.Response);
                }
                //3. Validate and transition from SeedingBracket to BracketInProgress
                _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.BracketInProgress);
                tournament.Status = TournamentStatus.BracketInProgress;

                await _tournamentRepository.Update(tournament);
                await _tournamentRepository.Save();

                _logger.LogInformation($"Tournament {tournament.Id} bracket started successfully.");

                return new StartBracketResponseDTO(true, "Bracket stage started successfully", tournament.Status);

            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Invalid state transition: {ex.Message}");
                return new StartBracketResponseDTO(false, ex.Message, tournament.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting bracket: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<SeedBracketResponseDTO> SeedBracket(long tournamentId, List<Team> teams)
        {
            _logger.LogInformation($"=== Starting bracket seeding for tournament {tournamentId} with {teams?.Count ?? 0} teams ===");

            try
            {
                // 1. Validate inputs
                if (teams == null || teams.Count == 0)
                {
                    return new SeedBracketResponseDTO(false, "No teams provided for bracket");
                }

                if (!IsPowerOfTwo(teams.Count))
                {
                    return new SeedBracketResponseDTO(false,
                        $"Team count must be power of 2. Got {teams.Count} teams");
                }

                // 2. Get bracket standing
                var standings = await _standingRepository.GetAllByFK("TournamentId", tournamentId);
                var bracket = standings.FirstOrDefault(s => s.StandingType == StandingType.Bracket);

                if (bracket == null)
                {
                    return new SeedBracketResponseDTO(false, "Bracket standing not found");
                }

                if (bracket.IsSeeded)
                {
                    return new SeedBracketResponseDTO(false, "Bracket is already seeded");
                }

                // 3. Apply single elimination seeding (top vs bottom)
                var seededPairs = CreateSingleEliminationPairs(teams);
                _logger.LogInformation($"Created {seededPairs.Count} first-round pairings");

                // 4. Calculate bracket structure
                int teamCount = teams.Count;
                int totalRounds = (int)Math.Log2(teamCount);
                _logger.LogInformation($"Creating bracket with {totalRounds} rounds for {teamCount} teams");

                // 5. Create ALL matches (Round 1 with teams, Round 2+ with TBD) using MatchService
                _logger.LogInformation($"Generating matches across {totalRounds} rounds");

                for (int round = 1; round <= totalRounds; round++)
                {
                    int matchesInRound = (int)Math.Pow(2, totalRounds - round);

                    for (int seed = 1; seed <= matchesInRound; seed++)
                    {
                        if (round == 1)
                        {
                            // Round 1: Use actual teams from seeding (already fetched)
                            var pairIndex = seed - 1;
                            var (teamA, teamB) = seededPairs[pairIndex];

                            var result = await _matchService.GenerateMatch(teamA, teamB, round, seed, bracket.Id);
                            if (!result.Success)
                            {
                                throw new Exception($"Failed to generate match: {result.Message}");
                            }

                            _logger.LogInformation($"R{round} Match {seed}: {teamA.Name} vs {teamB.Name}");
                        }
                        else
                        {
                            // Round 2+: TBD teams (null teams)
                            var result = await _matchService.GenerateMatch(null, null, round, seed, bracket.Id);
                            if (!result.Success)
                            {
                                throw new Exception($"Failed to generate TBD match: {result.Message}");
                            }

                            _logger.LogInformation($"R{round} Match {seed}: TBD vs TBD");
                        }
                    }
                }

                _logger.LogInformation($"Generated all matches with games across {totalRounds} rounds");

                // 6. Create BracketEntry records for participation tracking
                var bracketEntries = new List<Bracket>();

                foreach (var team in teams)
                {
                    var bracketEntry = new Bracket(tournamentId, bracket.Id, team);
                    bracketEntry.CurrentRound = 1;
                    bracketEntry.Status = TeamStatus.Competing;

                    bracketEntries.Add(bracketEntry);
                }

                await _bracketRepository.AddRange(bracketEntries);
                _logger.LogInformation($"Created {bracketEntries.Count} bracket entries");

                // 7. Mark bracket as seeded
                bracket.IsSeeded = true;
                await _standingRepository.Update(bracket);

                // 8. Save all changes
                await _bracketRepository.Save();
                await _standingRepository.Save();

                _logger.LogInformation($"✓ Bracket seeded successfully with {teams.Count} teams across {totalRounds} rounds");
                return new SeedBracketResponseDTO(true,
                    $"Bracket seeded with {teams.Count} teams across {totalRounds} rounds");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error seeding bracket for tournament {tournamentId}: {ex.Message}");
                return new SeedBracketResponseDTO(false, $"Bracket seeding failed: {ex.Message}");
            }
        }

        // Helper method: Check if number is power of 2
        private bool IsPowerOfTwo(int n)
        {
            return n > 0 && (n & (n - 1)) == 0;
        }

        // Helper method: Generate seeding order for single elimination
        private int[] GenerateSeedingOrder(int teamCount)
        {
            // Standard single elimination seeding
            // Creates bracket where higher seeds face lower seeds
            var rounds = (int)Math.Log2(teamCount);
            var order = new int[teamCount];

            order[0] = 0;  // Best team
            order[1] = teamCount - 1;  // Worst team

            int filled = 2;
            for (int round = 1; round < rounds; round++)
            {
                int step = teamCount / (int)Math.Pow(2, round + 1);
                int currentFilled = filled; // Capture value before loop
                for (int i = 0; i < currentFilled; i += 2)
                {
                    order[filled++] = order[i] + step;
                    order[filled++] = order[i + 1] - step;
                }
            }

            return order;
        }

        // Helper method: Create single elimination pairs
        private List<(Team teamA, Team teamB)> CreateSingleEliminationPairs(
            List<Team> teams)
        {
            var pairs = new List<(Team, Team)>();
            int teamCount = teams.Count;

            // Get seeding order (ensures #1 vs #8, #4 vs #5, etc.)
            var seedingOrder = GenerateSeedingOrder(teamCount);

            for (int i = 0; i < seedingOrder.Length; i += 2)
            {
                var teamA = teams[seedingOrder[i]];
                var teamB = teams[seedingOrder[i + 1]];
                pairs.Add((teamA, teamB));

                _logger.LogInformation($"Pair created: {teamA.Name} (rank {seedingOrder[i] + 1}) vs {teamB.Name} (rank {seedingOrder[i + 1] + 1})");
            }

            return pairs;
        }

        /// <summary>
        /// Advances the match winner to the next round by populating the appropriate team slot.
        /// </summary>
        private async Task AdvanceWinnerToNextRound(long finishedMatchId, long winnerId, long standingId)
        {
            // 1. Get the finished match
            var finishedMatch = await _matchRepository.Get(finishedMatchId)
                ?? throw new Exception($"Match {finishedMatchId} not found");

            if (!finishedMatch.Round.HasValue || !finishedMatch.Seed.HasValue)
            {
                throw new Exception($"Match {finishedMatchId} missing Round or Seed information");
            }

            int currentRound = finishedMatch.Round.Value;
            int currentSeed = finishedMatch.Seed.Value;

            // 2. Calculate next round match position
            int nextRound = currentRound + 1;
            int nextSeed = (int)Math.Ceiling(currentSeed / 2.0);

            _logger.LogInformation($"Match R{currentRound}S{currentSeed} finished. Winner {winnerId} advances to R{nextRound}S{nextSeed}");

            // 3. Find the next round match
            var allMatches = await _matchRepository.GetAllByFK("StandingId", standingId);
            var nextMatch = allMatches.FirstOrDefault(m =>
                m.Round == nextRound &&
                m.Seed == nextSeed);

            if (nextMatch == null)
            {
                _logger.LogInformation($"No next round match found (final match completed)");
                return; // This was the final match
            }

            // 4. Determine which team slot (A or B) based on seed parity
            // Odd seeds (1, 3, 5...) → TeamA
            // Even seeds (2, 4, 6...) → TeamB
            if (currentSeed % 2 == 1)
            {
                nextMatch.TeamAId = winnerId;
                _logger.LogInformation($"Set R{nextRound}S{nextSeed} TeamA = {winnerId}");
            }
            else
            {
                nextMatch.TeamBId = winnerId;
                _logger.LogInformation($"Set R{nextRound}S{nextSeed} TeamB = {winnerId}");
            }

            // 5. Update all games in the next match with the new team IDs
            await _gameService.UpdateGamesTeamIds(nextMatch.Id, nextMatch.TeamAId, nextMatch.TeamBId);

            // 6. Save the updated match
            await _matchRepository.Update(nextMatch);
            await _matchRepository.Save();
        }

        /// <summary>
        /// Checks if the given match is the final match of the bracket.
        /// </summary>
        private async Task<bool> IsFinalMatch(long matchId, long standingId)
        {
            var match = await _matchRepository.Get(matchId)
                ?? throw new Exception($"Match {matchId} not found");

            if (!match.Round.HasValue || !match.Seed.HasValue)
            {
                return false;
            }

            // Get all matches for this standing to determine total rounds
            var allMatches = await _matchRepository.GetAllByFK("StandingId", standingId);
            int totalRounds = allMatches.Max(m => m.Round ?? 0);

            // Final match is: last round, seed 1
            bool isFinal = match.Round.Value == totalRounds && match.Seed.Value == 1;

            if (isFinal)
            {
                _logger.LogInformation($"Match {matchId} is the FINAL match (R{match.Round}S{match.Seed})");
            }

            return isFinal;
        }
    }
}
