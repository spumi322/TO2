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
        private readonly ITO2DbContext _dbContext;
        private readonly ILogger<StandingService> _logger;

        public StandingService(IGenericRepository<Standing> standingRepository,
                               IGenericRepository<Match> matchRepository,
                               ITO2DbContext tO2DbContext,
                               ILogger<StandingService> logger)
        {
            _standingRepository = standingRepository;
            _matchRepository = matchRepository;
            _dbContext = tO2DbContext;
            _logger = logger;
        }

        public async Task GenerateStanding(long tournamentId, string name, StandingType type, int? teamsPerStanding)
        {
            try
            {
                var standing = new Standing(name, type, DateTime.UtcNow, DateTime.UtcNow, teamsPerStanding);
                standing.TournamentId = tournamentId;

                await _standingRepository.Add(standing);
                await _standingRepository.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error saving standing: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
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
                _logger.LogError("Error getting standings: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
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

        public async Task<int> TopX(long tournamentId)
        {
            var standings = await _standingRepository.GetAllByFK("tournamentId", tournamentId);
            var groups = standings.Where(s => s.Type == StandingType.Group).ToList();
            var bracket = standings.FirstOrDefault(s => s.Type == StandingType.Bracket);

            if (bracket == null || groups.Count == 0) return 0;

            int teamsToAdvancePerGroup = bracket.MaxTeams / groups.Count;

            return teamsToAdvancePerGroup;
        }
    }
}
