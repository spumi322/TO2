using Application.Contracts;
using Application.DTOs.Game;
using Microsoft.AspNetCore.Mvc;

namespace TO2.Controllers
{
    [ApiController]
    [Route("api/matches")]
    public class MatchesController : ControllerBase
    {
        private readonly IMatchService _matchService;
        private readonly IGameService _gameService;
        private readonly IOrchestrationService _orchestrationService;

        public MatchesController(IMatchService matchService, IGameService gameService, IOrchestrationService orchestrationService)
        {
            _matchService = matchService;
            _gameService = gameService;
            _orchestrationService = orchestrationService;
        }

        //// GET match by id
        //[HttpGet("match/{id}")]
        //public async Task<IActionResult> GetMatch(long id)
        //{
        //    return Ok(await _matchService.GetMatchAsync(id));
        //}

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
        [HttpGet("games/{matchId}")]
        public async Task<IActionResult> GetGames(long matchId)
        {
            return Ok(await _gameService.GetAllGamesByMatch(matchId));
        }

        // PUT game result
        [HttpPut("{gameId}/result")]
        public async Task<IActionResult> SetGameResult(SetGameResultDTO request)
        {
            var response = await _orchestrationService.ProcessGameResult(request);

            return response is not null ? Ok(response) : NoContent();
        }
    }
}
