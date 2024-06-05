using Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace TO2.Controllers
{
    [ApiController]
    [Route("api/matches")]
    public class MatchesController : ControllerBase
    {
        private readonly IMatchService _matchService;
        private readonly IGameService _gameService;

        public MatchesController(IMatchService matchService, IGameService gameService)
        {
            _matchService = matchService;
            _gameService = gameService;
        }

        // GET match by id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMatch(long id)
        {
            return Ok(await _matchService.GetMatchAsync(id));
        }

        // GET matches by standingId
        [HttpGet("all/{standingId}")]
        public async Task<IActionResult> GetMatches(long standingId)
        {
            return Ok(await _matchService.GetMatchesAsync(standingId));
        }

        // GET game by id
        [HttpGet("game/{id}")]
        public async Task<IActionResult> GetGame(long id)
        {
            return Ok(await _gameService.GetGameAsync(id));
        }

        // GET games by matchId
        [HttpGet("{matchId}/games")]
        public async Task<IActionResult> GetGames(long matchId)
        {
            return Ok(await _gameService.GetAllGamesByMatch(matchId));
        }

        // PUT game result
        [HttpPut("{gameId}/result")]
        public async Task<IActionResult> SetGameResult(long gameId, int? TeamAScore, int? TeamBScore, TimeSpan? duration, long winnerId)
        {
            await _gameService.SetGameResult(gameId, TeamAScore, TeamBScore, duration, winnerId);

            return Ok();
        }
    }
}
