using Application.Contracts;
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
        private readonly ILogger<StandingService> _logger;

        public StandingService(IGenericRepository<Standing> standingRepository, ILogger<StandingService> logger)
        {
            _standingRepository = standingRepository;
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
    }
}
