using Application.Contracts;
using Domain.AggregateRoots;
using Domain.Configuration;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class MatchService : IMatchService
    {
        private readonly IRepository<Match> _matchRepository;
        private readonly ITournamentFormatConfiguration _formatConfig;
        private readonly ILogger<MatchService> _logger;

        public MatchService(IRepository<Match> matchRepository,
                            ITournamentFormatConfiguration formatConfig,
                            ILogger<MatchService> logger)
        {
            _matchRepository = matchRepository;
            _formatConfig = formatConfig;
            _logger = logger;
        }

        public async Task<List<Match>> GetMatchesAsync(long standingId)
        {
            var matches = await _matchRepository.FindAllAsync(m => m.StandingId == standingId);

            return matches.ToList();
        }

        public async Task<Match> GenerateMatch(Team? teamA, Team? teamB, int round, int seed, long standingId)
        {
            Match match;

            var defaultBestOf = _formatConfig.GetDefaultBestOf();

            if (teamA != null && teamB != null)
            {
                match = new Match(teamA, teamB, defaultBestOf);
                match.Round = round;
                match.Seed = seed;
                match.StandingId = standingId;
            }
            else
            {
                match = new Match
                {
                    StandingId = standingId,
                    Round = round,
                    Seed = seed,
                    TeamAId = teamA?.Id ?? 0,
                    TeamBId = teamB?.Id ?? 0,
                    BestOf = defaultBestOf
                };
            }

            await _matchRepository.AddAsync(match);

            return match;

        }
    }
}
