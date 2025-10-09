using Application.Contracts;
using Application.DTOs.Standing;
using Domain.AggregateRoots;
using Domain.DomainEvents;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.EventHandlers
{
    public class AllGroupsFinishedEventHandler : IDomainEventHandler<AllGroupsFinishedEvent>
    {
        private readonly ILogger<AllGroupsFinishedEventHandler> _logger;
        private readonly IStandingService _standingService;
        private readonly IMatchService _matchService;

        public AllGroupsFinishedEventHandler(
            ILogger<AllGroupsFinishedEventHandler> logger,
            IStandingService standingService,
            IMatchService matchService)
        {
            _logger = logger;
            _standingService = standingService;
            _matchService = matchService;
        }

        public async Task HandleAsync(AllGroupsFinishedEvent domainEvent)
        {
            var tournamentId = domainEvent.TournamentId;

            _logger.LogInformation($"All groups finished for tournament {tournamentId}. Preparing bracket...");

            // Determine which teams advance and which are eliminated
            var advancingTeams = await _standingService.PrepareTeamsForBracket(tournamentId);

            // Seed the bracket with advancing teams
            await _matchService.SeedBracket(tournamentId, advancingTeams);

            _logger.LogInformation($"Bracket seeded with {advancingTeams.Count} teams");
        }
    }
}
