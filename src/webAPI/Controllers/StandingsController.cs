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

        public StandingsController(IStandingService standingService)
        {
            _standingService = standingService;
        }

        [HttpGet("{tournamentId}")]
        public async Task<IActionResult> GetAll(long tournamentId)
        {
            return Ok(await _standingService.GetStandingsAsync(tournamentId));
        }
    }
}
