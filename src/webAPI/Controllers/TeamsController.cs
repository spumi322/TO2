using Application.Contracts;
using Application.DTOs.Team;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace TO2.Controllers
{
    [ApiController]
    [Route("api/teams")]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamsController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        // GET: api/Teams
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _teamService.GetAllTeamsAsync());
        }

        // GET: api/Teams/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            return Ok(await _teamService.GetTeamAsync(id));
        }

        // POST: api/Teams
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateTeamRequestDTO request)
        {
            return Ok(await _teamService.CreateTeamAsync(request));
        }

        // POST: api/Teams/5/5
        [HttpPost("tournamentId/teamId")]
        public async Task<IActionResult> AddTeamToTournament(AddTeamToTournamentRequestDTO request)
        {
            return Ok(await _teamService.AddTeamToTournamentAsync(request));
        }

        // PUT: api/Teams/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] UpdateTeamRequestDTO request)
        {
            return Ok(await _teamService.UpdateTeamAsync(id, request));
        }

        // DELETE: api/Teams/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _teamService.DeleteTeamAsync(id);

            return NoContent();
        }

        // GET: api/5/teams-with-stats
        [HttpGet("{standingId}/teams-with-stats")]
        public async Task<IActionResult> GetTeamsWithStats(long standingId)
        {
            return Ok(await _teamService.GetTeamsWithStatsAsync(standingId));
        }
    }
}
