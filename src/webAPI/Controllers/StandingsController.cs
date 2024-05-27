using Application.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TO2.Controllers
{
    [ApiController]
    [Route("api/standings")]
    public class StandingsController : ControllerBase
    {
        private readonly IStandingService _standingService;
        private readonly IMatchService _matchService;

        public StandingsController(IStandingService standingService, IMatchService matchService)
        {
            _standingService = standingService;
            _matchService = matchService;
        }

        [HttpGet("{tournamentId}")]
        public async Task<IActionResult> GetAll(long tournamentId)
        {
            return Ok(await _standingService.GetStandingsAsync(tournamentId));
        }

        [HttpGet("generate/{tournamentId}")]
        public async Task<IActionResult> Generate(long tournamentId)
        {
            await _matchService.SeedGroups(tournamentId);

            return Ok();
        }
    }
}
