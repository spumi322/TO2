using Application.Contracts;
using Domain.AggregateRoots;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class TournamentRollbackService : ITournamentRollbackService
    {
        private readonly IGenericRepository<Tournament> _tournamentRepository;
        private readonly IGenericRepository<Standing> _standingRepository;
        private readonly IGenericRepository<Match> _matchRepository;
        private readonly ITO2DbContext _dbContext;
        private readonly ILogger<TournamentRollbackService> _logger;

        public TournamentRollbackService(
            IGenericRepository<Tournament> tournamentRepository,
            IGenericRepository<Standing> standingRepository,
            IGenericRepository<Match> matchRepository,
            ITO2DbContext dbContext,
            ILogger<TournamentRollbackService> logger)
        {
            _tournamentRepository = tournamentRepository;
            _standingRepository = standingRepository;
            _matchRepository = matchRepository;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task RollbackTournamentStartAsync(long tournamentId, List<long> createdStandingIds)
        {
            _logger.LogWarning(
                "Starting rollback for tournament {TournamentId}. Standings to clean: [{StandingIds}]",
                tournamentId,
                string.Join(", ", createdStandingIds)
            );

            // Step 1: Delete Games associated with matches in affected standings
            await DeleteGamesAsync(createdStandingIds);

            // Step 2: Delete Matches in affected standings
            await DeleteMatchesAsync(createdStandingIds);

            // Step 3: Delete Group entries referencing affected standings
            await DeleteGroupEntriesAsync(createdStandingIds);

            // Step 4: Delete Bracket entries referencing affected standings
            await DeleteBracketEntriesAsync(createdStandingIds);

            // Step 5: Delete Standings themselves
            await DeleteStandingsAsync(createdStandingIds);

            // Step 6: Reset Tournament state
            await ResetTournamentStateAsync(tournamentId);

            _logger.LogInformation(
                "Rollback completed for tournament {TournamentId}",
                tournamentId
            );
        }

        private async Task DeleteGamesAsync(List<long> standingIds)
        {
            try
            {
                _logger.LogInformation("Deleting games for standings: [{StandingIds}]", string.Join(", ", standingIds));

                // Find all games in matches belonging to these standings
                var gamesToDelete = await _dbContext.Games
                    .Where(g => _dbContext.Matches
                        .Where(m => standingIds.Contains(m.StandingId))
                        .Select(m => m.Id)
                        .Contains(g.MatchId))
                    .ToListAsync();

                _logger.LogInformation("Found {Count} games to delete", gamesToDelete.Count);

                _dbContext.Games.RemoveRange(gamesToDelete);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted {Count} games", gamesToDelete.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete games for standings: [{StandingIds}]", string.Join(", ", standingIds));
                // Continue with rollback even if this step fails
            }
        }

        private async Task DeleteMatchesAsync(List<long> standingIds)
        {
            try
            {
                _logger.LogInformation("Deleting matches for standings: [{StandingIds}]", string.Join(", ", standingIds));

                var matchesToDelete = await _dbContext.Matches
                    .Where(m => standingIds.Contains(m.StandingId))
                    .ToListAsync();

                _logger.LogInformation("Found {Count} matches to delete", matchesToDelete.Count);

                _dbContext.Matches.RemoveRange(matchesToDelete);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted {Count} matches", matchesToDelete.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete matches for standings: [{StandingIds}]", string.Join(", ", standingIds));
                // Continue with rollback even if this step fails
            }
        }

        private async Task DeleteGroupEntriesAsync(List<long> standingIds)
        {
            try
            {
                _logger.LogInformation("Deleting group entries for standings: [{StandingIds}]", string.Join(", ", standingIds));

                var groupEntriesToDelete = await _dbContext.GroupEntries
                    .Where(g => standingIds.Contains(g.StandingId))
                    .ToListAsync();

                _logger.LogInformation("Found {Count} group entries to delete", groupEntriesToDelete.Count);

                _dbContext.GroupEntries.RemoveRange(groupEntriesToDelete);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted {Count} group entries", groupEntriesToDelete.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete group entries for standings: [{StandingIds}]", string.Join(", ", standingIds));
                // Continue with rollback even if this step fails
            }
        }

        private async Task DeleteBracketEntriesAsync(List<long> standingIds)
        {
            try
            {
                _logger.LogInformation("Deleting bracket entries for standings: [{StandingIds}]", string.Join(", ", standingIds));

                var bracketEntriesToDelete = await _dbContext.BracketEntries
                    .Where(b => standingIds.Contains(b.StandingId))
                    .ToListAsync();

                _logger.LogInformation("Found {Count} bracket entries to delete", bracketEntriesToDelete.Count);

                _dbContext.BracketEntries.RemoveRange(bracketEntriesToDelete);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted {Count} bracket entries", bracketEntriesToDelete.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete bracket entries for standings: [{StandingIds}]", string.Join(", ", standingIds));
                // Continue with rollback even if this step fails
            }
        }

        private async Task DeleteStandingsAsync(List<long> standingIds)
        {
            try
            {
                _logger.LogInformation("Deleting standings: [{StandingIds}]", string.Join(", ", standingIds));

                foreach (var standingId in standingIds)
                {
                    try
                    {
                        await _standingRepository.Delete(standingId);
                        _logger.LogDebug("Deleted standing {StandingId}", standingId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete standing {StandingId}", standingId);
                        // Continue with other standings
                    }
                }

                await _standingRepository.Save();
                _logger.LogInformation("Successfully deleted {Count} standings", standingIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete standings: [{StandingIds}]", string.Join(", ", standingIds));
                // Continue with rollback even if this step fails
            }
        }

        private async Task ResetTournamentStateAsync(long tournamentId)
        {
            try
            {
                _logger.LogInformation("Resetting tournament state for tournament {TournamentId}", tournamentId);

                var tournament = await _tournamentRepository.Get(tournamentId);
                if (tournament == null)
                {
                    _logger.LogWarning("Tournament {TournamentId} not found during rollback", tournamentId);
                    return;
                }

                // Reset processing flag
                tournament.StopProcessing();

                // Keep Status and IsRegistrationOpen as-is (don't modify)
                _logger.LogInformation(
                    "Tournament {TournamentId} state: IsProcessing={IsProcessing}, Status={Status}, IsRegistrationOpen={IsRegistrationOpen}",
                    tournamentId,
                    tournament.IsProcessing,
                    tournament.Status,
                    tournament.IsRegistrationOpen
                );

                await _tournamentRepository.Update(tournament);
                await _tournamentRepository.Save();

                _logger.LogInformation("Successfully reset tournament {TournamentId} state", tournamentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset tournament state for tournament {TournamentId}", tournamentId);
                // This is the last step, but log the failure
            }
        }
    }
}
