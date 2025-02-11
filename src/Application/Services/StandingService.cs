using Application.Contracts;
using Domain.AggregateRoots;
using Domain.DomainEvents;
using Domain.Entities;
using Domain.Enums;
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
        private readonly ILogger<StandingService> _logger;

        public StandingService(IGenericRepository<Standing> standingRepository,
                               IGenericRepository<Match> matchRepository,
                               ILogger<StandingService> logger)
        {
            _standingRepository = standingRepository;
            _matchRepository = matchRepository;
            _logger = logger;   
        }

        public async Task GenerateStanding(long tournamentId, string name, StandingType type, int? teamsPerStanding)
        {
            try
            {
                var standing  = new Standing(name, type, DateTime.UtcNow, DateTime.UtcNow, teamsPerStanding);
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
            var taskStandings = GetStandingsAsync(tournamentId);
            List<Standing> standings = await taskStandings;

            foreach (var standing in standings)
            {
                var taskMatches = _matchRepository.GetAllByFK("StandingId", standing.Id);
                IReadOnlyList<Match> matches = await taskMatches;

                foreach (var match in matches)
                {
                    if (matches.All(m => m.WinnerId != null && m.LoserId != null))
                    {
                        standing.AddDomainEvent(new StandingFinishedEvent(standing.Id));
                    }
                }

                await _standingRepository.Save();
            }

        }
    }
}
