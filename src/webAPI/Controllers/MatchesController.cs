using Application.Contracts;
using Application.DTOs.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TO2.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/matches")]
    public class MatchesController : ControllerBase
    {
        private readonly IMatchService _matchService;
        private readonly IGameService _gameService;
        private readonly IWorkFlowService _workFlowService;

        public MatchesController(IMatchService matchService, IGameService gameService, IWorkFlowService workFlowService)
        {
            _matchService = matchService;
            _gameService = gameService;
            _workFlowService = workFlowService;
        }

        // GET /api/matches?standingId={id}
        [HttpGet]
        public async Task<IActionResult> GetMatches([FromQuery] long standingId)
        {
            return Ok(await _matchService.GetMatchesAsync(standingId));
        }

        // GET /api/matches/{matchId}/games
        [HttpGet("{matchId}/games")]
        public async Task<IActionResult> GetGames(long matchId)
        {
            return Ok(await _gameService.GetAllGamesByMatch(matchId));
        }

        // PUT /api/matches/{matchId}/games/{gameId}/result
        [HttpPut("{matchId}/games/{gameId}/result")]
        public async Task<IActionResult> SetGameResult(long matchId, long gameId, [FromBody] SetGameResultDTO request)
        {
            var response = await _workFlowService.ProcessGameResult(request);

            return response is not null ? Ok(response) : NoContent();
        }
    }
}
