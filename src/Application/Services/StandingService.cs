using Application.Contracts;
using Application.DTOs.Team;
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

namespace Application.Services
{
    public class StandingService : IStandingService
    {
        private readonly IGenericRepository<Standing> _standingRepository;
        private readonly IGenericRepository<Match> _matchRepository;
        private readonly IGenericRepository<Tournament> _tournamentRepository;
        private readonly ITO2DbContext _dbContext;
        private readonly ILogger<StandingService> _logger;

        public StandingService(IGenericRepository<Standing> standingRepository,
                               IGenericRepository<Match> matchRepository,
                               IGenericRepository<Tournament> tournamentRepository,
                               ITO2DbContext tO2DbContext,
                               ILogger<StandingService> logger)
        {
            _standingRepository = standingRepository;
            _matchRepository = matchRepository;
            _tournamentRepository = tournamentRepository;
            _dbContext = tO2DbContext;
            _logger = logger;
        }

        public async Task GenerateStanding(long tournamentId, string name, StandingType type, int? teamsPerStanding)
        {
            try
            {
                var standing = new Standing(name, teamsPerStanding, type);
                standing.TournamentId = tournamentId;

                await _standingRepository.Add(standing);
                await _standingRepository.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving standing: {Message}", ex.Message);

                throw;
            }
        }

        public async Task<List<Standing>> GetStandingsAsync(long tournamentId)
        {
            try
            {
                var standings = await _standingRepository.GetAllByFK("TournamentId", tournamentId);

                return standings.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting standings: {Message}", ex.Message);
                throw;
            }
        }

        public async Task CheckAndMarkStandingAsFinishedAsync(long tournamentId)
        {
            var standings = (await GetStandingsAsync(tournamentId))
                .Where(s => s.IsSeeded)
                .ToList();
            var standingsToUpdate = new List<Standing>();

            foreach (var standing in standings)
            {
                var matches = await _matchRepository.GetAllByFK("StandingId", standing.Id);
                if (matches.All(m => m.WinnerId != null && m.LoserId != null))
                {
                    standingsToUpdate.Add(standing);
                }
            }

            foreach (var standing in standingsToUpdate)
            {
                if (!standing.DomainEvents.Any(e => e is StandingFinishedEvent))
                {
                    standing.AddDomainEvent(new StandingFinishedEvent(standing.Id));
                }
            }

            await _standingRepository.Save();
        }

        public async Task CheckAndMarkAllGroupsAreFinishedAsync(long tournamentId)
        {
            var tournament = await _tournamentRepository.Get(tournamentId);
            var allGroups = (await _standingRepository.GetAllByFK("TournamentId", tournamentId))
                                .Where(s => s.StandingType == StandingType.Group)
                                .ToList();

            if (allGroups.All(ag => ag.IsFinished) &&
                !tournament.DomainEvents.Any(e => e is AllGroupsFinishedEvent))
            {
                tournament.AddDomainEvent(new AllGroupsFinishedEvent(tournamentId, allGroups));
            }

            // Removed Save() call - DbContext automatically tracks changes and saves after event handlers complete
        }

        public async Task<List<Application.DTOs.Standing.BracketSeedDTO>> PrepareTeamsForBracket(long tournamentId)
        {
            var tournament = await _tournamentRepository.Get(tournamentId);
            var standings = await GetStandingsAsync(tournamentId);
            var groups = standings.Where(s => s.StandingType == StandingType.Group).ToList();
            var bracket = standings.FirstOrDefault(s => s.StandingType == StandingType.Bracket);

            if (bracket == null)
                throw new Exception("Bracket standing not found");

            if (groups.Count == 0)
                throw new Exception("No groups found");

            // Calculate how many teams advance per group
            int teamsAdvancingPerGroup = bracket.MaxTeams / groups.Count;

            _logger.LogInformation($"Teams advancing per group: {teamsAdvancingPerGroup} (Bracket: {bracket.MaxTeams}, Groups: {groups.Count})");

            var advancingTeams = new List<Application.DTOs.Standing.BracketSeedDTO>();

            foreach (var group in groups)
            {
                // Get group entries sorted by Points DESC, then Wins DESC
                var groupEntries = await _dbContext.GroupEntries
                    .Where(g => g.StandingId == group.Id)
                    .OrderByDescending(g => g.Points)
                    .ThenByDescending(g => g.Wins)
                    .ThenBy(g => g.Losses)
                    .ToListAsync();

                // Top X teams advance
                var advancing = groupEntries.Take(teamsAdvancingPerGroup).ToList();
                var eliminated = groupEntries.Skip(teamsAdvancingPerGroup).ToList();

                int placement = 1;
                foreach (var team in advancing)
                {
                    team.Status = TeamStatus.Advanced;
                    advancingTeams.Add(new Application.DTOs.Standing.BracketSeedDTO
                    {
                        TeamId = team.TeamId,
                        GroupId = group.Id,
                        Placement = placement++
                    });

                    _logger.LogInformation($"Team {team.TeamName} advanced from {group.Name} (Placement: {placement - 1})");
                }

                foreach (var team in eliminated)
                {
                    team.Status = TeamStatus.Eliminated;
                    team.Eliminated = true;

                    _logger.LogInformation($"Team {team.TeamName} eliminated from {group.Name}");
                }
            }

            await _dbContext.SaveChangesAsync();

            return advancingTeams;
        }
    }
}
