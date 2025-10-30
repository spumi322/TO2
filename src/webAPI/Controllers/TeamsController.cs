using Application.Contracts;
using Application.DTOs.Team;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TO2.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/teams")]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly IStandingService _standingService;

        public TeamsController(ITeamService teamService,
                               IStandingService standingService)
        {
            _teamService = teamService;
            _standingService = standingService;
        }

        // GET: api/Teams
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _teamService.GetAllTeamsAsync());
        }

        // GET: api/5/teams-with-stats
        [HttpGet("{standingId}/teams-with-stats")]
        public async Task<IActionResult> GetTeamsWithStats(long standingId)
        {
            return Ok(await _standingService.GetTeamsWithStatsAsync(standingId));
        }

        // POST: api/Teams
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateTeamRequestDTO request)
        {
            return Ok(await _teamService.CreateTeamAsync(request));
        }

        // POST: api/Teams/5/5
        [HttpPost("tournamentId/teamId")]
        public async Task<IActionResult> AddTeamToTournament([FromBody] AddTeamToTournamentRequestDTO request)
        {
            return Ok(await _teamService.AddTeamToTournamentAsync(request));
        }

        // DELETE: api/Tournament/5/5
        [HttpDelete("{teamId}/{tournamentId}")]
        public async Task<IActionResult> RemoveTeamFromTournament(long teamId, long tournamentId)
        {
            await _teamService.RemoveTeamFromTournamentAsync(teamId, tournamentId);

            return NoContent();
        }
    }
}
