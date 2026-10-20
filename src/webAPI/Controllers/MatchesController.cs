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
        private readonly IWorkFlowService _workFlowService;

        public MatchesController(IWorkFlowService workFlowService)
        {
            _workFlowService = workFlowService;
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
