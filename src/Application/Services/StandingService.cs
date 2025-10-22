using Application.Contracts;
using Application.DTOs.Standing;
using Application.DTOs.Team;
using Domain.AggregateRoots;
//using Domain.DomainEvents; // STEP 1 FIX: No longer needed
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
        private readonly IGenericRepository<Group> _groupRepository;
        private readonly IGenericRepository<Team> _teamRepository;
        private readonly ILogger<StandingService> _logger;

        public StandingService(IGenericRepository<Standing> standingRepository,
                               IGenericRepository<Match> matchRepository,
                               IGenericRepository<Tournament> tournamentRepository,
                               IGenericRepository<Group> groupRepository,
                               IGenericRepository<Team> teamRepository,
                               ILogger<StandingService> logger)
        {
            _standingRepository = standingRepository;
            _matchRepository = matchRepository;
            _tournamentRepository = tournamentRepository;
            _groupRepository = groupRepository;
            _teamRepository = teamRepository;
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

        public async Task<bool> CheckAndMarkStandingAsFinished(long standingId)
        {
            var standing = await _standingRepository.Get(standingId)
                ?? throw new Exception($"Standing with Id: {standingId} was not found!");
            var matches = await _matchRepository.GetAllByFK("StandingId", standingId)
                ?? throw new Exception($"Matches for the standing Id : {standingId} were not found!");
            bool standingFinished = false;

            if (matches.All(m => m.WinnerId is not null && m.LoserId is not null))
            {
                standingFinished = true;
                standing.IsFinished = true;
                await _standingRepository.Update(standing);
                await _standingRepository.Save();
            }

            return standingFinished;
        }

        public async Task<bool> CheckAllGroupsAreFinished(long tournamentId)
        {
            var allStandings = await _standingRepository.GetAllByFK("TournamentId", tournamentId)
                ?? throw new Exception($"Standings with tournamentId: {tournamentId} was not found!");
            var allGroups = allStandings.Where(s => s.StandingType == StandingType.Group);

            bool allGroupsFinished = allGroups.Any() && allGroups.All(ag => ag.IsFinished);

            if (allGroupsFinished)
            {
                _logger.LogInformation($"✓ ALL GROUPS FINISHED for tournament {tournamentId}!");
            }
            else
            {
                _logger.LogInformation($"Groups not all finished yet for tournament {tournamentId}.");
            }

            return allGroupsFinished;
        }

        public async Task<List<Team>> GetTeamsForBracket(long tournamentId)
        {
            var advancingTeams = new List<Team>();
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

            foreach (var group in groups)
            {
                // Get group entries using repository and sort in memory
                var allGroupEntries = await _groupRepository.GetAllByFK("StandingId", group.Id);
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
                    await _groupRepository.Update(groupEntry);

                    // Fetch Team entity using repository
                    var team = await _teamRepository.Get(groupEntry.TeamId);

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
                    await _groupRepository.Update(groupEntry);
                    _logger.LogInformation($"Team {groupEntry.TeamName} eliminated from {group.Name}");
                }
            }

            // Save GroupEntry status changes using repository
            await _groupRepository.Save();

            return advancingTeams;
        }
    }
}
