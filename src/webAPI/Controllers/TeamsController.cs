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

        public TeamsController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        // GET: api/teams/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            return Ok(await _teamService.GetTeamAsync(id));
        }

        // GET: api/teams
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _teamService.GetAllTeamsAsync());
        }

        // POST: api/teams
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateTeamRequestDTO request)
        {
            var result = await _teamService.CreateTeamAsync(request);

            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }

        // POST: api/teams/{tournamentId}/teams/{teamId}
        [HttpPost("{tournamentId}/teams/{teamId}")]
        public async Task<IActionResult> AddTeamToTournament(long tournamentId, long teamId)
        {
            var request = new AddTeamToTournamentRequestDTO(tournamentId, teamId);

            return Ok(await _teamService.AddTeamToTournamentAsync(request));
        }

        // DELETE: api/teams/{teamId}/{tournamentId}
        [HttpDelete("{teamId}/{tournamentId}")]
        public async Task<IActionResult> RemoveTeamFromTournament(long teamId, long tournamentId)
        {
            await _teamService.RemoveTeamFromTournamentAsync(teamId, tournamentId);

            return NoContent();
        }
    }
}
