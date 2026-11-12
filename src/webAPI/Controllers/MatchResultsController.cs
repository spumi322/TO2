using Application.DTOs.Match;
using Application.Pipelines.MatchResult.Contracts;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TO2.Controllers
{
    // Confirmed match results (draft scores are shared through the hub, never saved before this)
    [Authorize]
    [ApiController]
    [Route("api/matches")]
    public class MatchResultsController : ControllerBase
    {
        private readonly IMatchResultPipeline _matchResultPipeline;

        public MatchResultsController(IMatchResultPipeline matchResultPipeline)
        {
            _matchResultPipeline = matchResultPipeline;
        }

        // POST /api/matches/{matchId}/result
        [HttpPost("{matchId}/result")]
        public async Task<IActionResult> SetMatchResult(long matchId, [FromBody] SetMatchResultDTO request)
        {
            if (request.MatchId != matchId)
                throw new ValidationException("Match id in the path and the body differ.");

            return Ok(await _matchResultPipeline.ExecuteAsync(request));
        }
    }
}
