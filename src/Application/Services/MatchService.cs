using Application.Contracts;
using Domain.AggregateRoots;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class MatchService : IMatchService
    {
        private readonly IGenericRepository<Match> _matchRepository;
        private readonly ILogger<MatchService> _logger;

        private readonly IUnitOfWork _unitOfWork;

        public MatchService(IGenericRepository<Match> matchRepository,
                            ILogger<MatchService> logger,
                                 IUnitOfWork unitOfWork)
        {
            _matchRepository = matchRepository;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<Match>> GetMatchesAsync(long standingId)
        {
            try
            {
                var matches = await _matchRepository.GetAllByFK("StandingId", standingId);

                return matches.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting matches: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<Match> GenerateMatch(Team? teamA, Team? teamB, int round, int seed, long standingId)
        {
            Match match;

            if (teamA != null && teamB != null)
            {
                match = new Match(teamA, teamB, BestOf.Bo3);
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
                    BestOf = BestOf.Bo3
                };
            }

            await _matchRepository.Add(match);

            return match;

        }
    }
}
