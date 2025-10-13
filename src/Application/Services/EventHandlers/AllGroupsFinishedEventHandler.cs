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

            _logger.LogInformation($"========== AllGroupsFinishedEvent Handler Started ==========");
            _logger.LogInformation($"Tournament ID: {tournamentId}");
            _logger.LogInformation($"Groups completed: {domainEvent.Groups.Count}");

            // Determine which teams advance and which are eliminated
            var advancingTeams = await _standingService.PrepareTeamsForBracket(tournamentId);

            _logger.LogInformation($"Teams advancing to bracket: {advancingTeams.Count}");
            foreach (var team in advancingTeams)
            {
                _logger.LogInformation($"  - Team ID {team.TeamId} from Group {team.GroupId} (Placement: {team.Placement})");
            }

            // Seed the bracket with advancing teams
            var result = await _matchService.SeedBracket(tournamentId, advancingTeams);

            _logger.LogInformation($"Bracket seeding result: {result.Message}, Success: {result.Success}");
            _logger.LogInformation($"========== AllGroupsFinishedEvent Handler Completed ==========");
        }
    }
}
