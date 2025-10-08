using Application.Contracts;
using Application.Services.EventHandling;
using Domain.AggregateRoots;
using Domain.DomainEvents;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.EventHandlers
{
    public class StartTournamentEventHandler : IDomainEventHandler<StartTournamentEvent>
    {
        private readonly ILogger<StartTournamentEventHandler> _logger;
        private readonly IStartTournamentService _startTournamentService;
        private readonly ITournamentRollbackService _rollbackService;
        private readonly IGenericRepository<Tournament> _tournamentRepository;

        public StartTournamentEventHandler(
            ILogger<StartTournamentEventHandler> logger,
            IStartTournamentService startTournamentService,
            ITournamentRollbackService rollbackService,
            IGenericRepository<Tournament> tournamentRepository)
        {
            _logger = logger;
            _startTournamentService = startTournamentService;
            _rollbackService = rollbackService;
            _tournamentRepository = tournamentRepository;
        }

        public async Task HandleAsync(StartTournamentEvent domainEvent)
        {
            var tournamentId = domainEvent.TournamentId;
            var format = domainEvent.Format;

            _logger.LogInformation(
                "Handling StartTournamentEvent for tournament {TournamentId} with format {Format}",
                tournamentId,
                format
            );

            try
            {
                // Initialize tournament (seed matches and generate games)
                var seedResult = await _startTournamentService.InitializeTournamentAsync(tournamentId);

                if (!seedResult.Success)
                {
                    _logger.LogError(
                        "Tournament initialization failed for tournament {TournamentId}. Failed step: {FailedStep}. Error: {ErrorMessage}",
                        tournamentId,
                        seedResult.FailedStep,
                        seedResult.ErrorMessage
                    );

                    // Rollback any created standings
                    if (seedResult.CreatedStandingsId.Any())
                    {
                        _logger.LogWarning(
                            "Initiating rollback for tournament {TournamentId}. Created standings: [{StandingIds}]",
                            tournamentId,
                            string.Join(", ", seedResult.CreatedStandingsId)
                        );

                        await _rollbackService.RollbackTournamentStartAsync(tournamentId, seedResult.CreatedStandingsId);
                    }
                    else
                    {
                        // No standings were created, just reset tournament state
                        var tournament = await _tournamentRepository.Get(tournamentId);
                        if (tournament != null)
                        {
                            tournament.StopProcessing();
                            await _tournamentRepository.Update(tournament);
                            await _tournamentRepository.Save();

                            _logger.LogInformation("Reset IsProcessing flag for tournament {TournamentId}", tournamentId);
                        }
                    }

                    return;
                }

                // Success: Update tournament state
                var completedTournament = await _tournamentRepository.Get(tournamentId);
                if (completedTournament == null)
                {
                    _logger.LogWarning("Tournament {TournamentId} not found after successful initialization", tournamentId);
                    return;
                }

                // Close registration and set status to Ongoing
                completedTournament.IsRegistrationOpen = false;
                completedTournament.Status = TournamentStatus.Ongoing;
                completedTournament.StopProcessing();

                await _tournamentRepository.Update(completedTournament);
                await _tournamentRepository.Save();

                _logger.LogInformation(
                    "Tournament {TournamentId} started successfully. Status: {Status}, IsRegistrationOpen: {IsRegistrationOpen}, IsProcessing: {IsProcessing}",
                    tournamentId,
                    completedTournament.Status,
                    completedTournament.IsRegistrationOpen,
                    completedTournament.IsProcessing
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error handling StartTournamentEvent for tournament {TournamentId}",
                    tournamentId
                );

                // Attempt to reset tournament state
                try
                {
                    var tournament = await _tournamentRepository.Get(tournamentId);
                    if (tournament != null)
                    {
                        tournament.StopProcessing();
                        await _tournamentRepository.Update(tournament);
                        await _tournamentRepository.Save();

                        _logger.LogInformation("Reset IsProcessing flag for tournament {TournamentId} after exception", tournamentId);
                    }
                }
                catch (Exception resetEx)
                {
                    _logger.LogError(resetEx, "Failed to reset tournament {TournamentId} state after exception", tournamentId);
                }
            }
        }
    }
}
