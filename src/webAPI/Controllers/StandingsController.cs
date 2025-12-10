using Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TO2.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/standings")]
    public class StandingsController : ControllerBase
    {
        private readonly IStandingService _standingService;

        public StandingsController(
            IStandingService standingService
            )
        {
            _standingService = standingService;
        }

        [HttpGet("{tournamentId}")]
        public async Task<IActionResult> GetStandings(long tournamentId)
        {
            var result = await _standingService.GetStandingsAsync(tournamentId);
            return Ok(result);
        }

        [HttpGet("{tournamentId}/groups")]
        public async Task<IActionResult> GetGroupsWithDetails(long tournamentId)
        {
            var result = await _standingService.GetGroupsWithDetailsAsync(tournamentId);
            return Ok(result);
        }

        [HttpGet("{tournamentId}/bracket")]
        public async Task<IActionResult> GetBracketWithDetails(long tournamentId)
        {
            var result = await _standingService.GetBracketWithDetailsAsync(tournamentId);
            return Ok(result);
        }
    }
}
