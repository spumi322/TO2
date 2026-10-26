
using Application.Contracts;
using Application.DTOs.Game;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class GameService : IGameService
    {
        private readonly IRepository<Game> _gameRepository;
        private readonly IFormatService _formatService;
        private readonly ILogger<GameService> _logger;

        public GameService(IRepository<Game> gameRepository,
                           IFormatService formatService,
                           ILogger<GameService> logger)
        {
            _gameRepository = gameRepository;
            _formatService = formatService;
            _logger = logger;
        }

        public async Task<GenerateGamesDTO> GenerateGames(Match match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            if (match.Id != 0)
            {
                var gamesAlreadyGenerated = await _gameRepository.FindAllAsync(g => g.MatchId == match.Id);
                if (gamesAlreadyGenerated.Count > 0)
                {
                    _logger.LogWarning("Attempted to generate games for match {MatchId}, but games already exist.", match.Id);
                    throw new ConflictException("This match already has its games.");
                }
            }

            var games = new List<Game>();
            var matchesToPlay = _formatService.GetTotalGames(match.BestOf);

            for (int i = 0; i < matchesToPlay; i++)
            {
                var game = new Game(match, match.TeamAId, match.TeamBId);
                games.Add(game);
            }

            await _gameRepository.AddRangeAsync(games);

            return new GenerateGamesDTO(true, $"{games.Count} games generated for match {match.Id}");
        }
    }
}
