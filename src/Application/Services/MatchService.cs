using Application.Contracts;
using Domain.AggregateRoots;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class MatchService : IMatchService
    {
        private readonly IRepository<Match> _matchRepository;
        private readonly IFormatService _formatService;

        public MatchService(IRepository<Match> matchRepository,
                            IFormatService formatService)
        {
            _matchRepository = matchRepository;
            _formatService = formatService;
        }

        public async Task<Match> GenerateMatch(Team? teamA, Team? teamB, int round, int seed, long standingId)
        {
            Match match;

            var defaultBestOf = _formatService.GetDefaultBestOf();

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
                    TeamAId = teamA?.Id,
                    TeamBId = teamB?.Id,
                    BestOf = defaultBestOf
                };
            }

            await _matchRepository.AddAsync(match);

            return match;

        }
    }
}
