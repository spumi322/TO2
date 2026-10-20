using Application.Contracts;
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

        // DELETE: api/teams/{teamId}/{tournamentId}
        [HttpDelete("{teamId}/{tournamentId}")]
        public async Task<IActionResult> RemoveTeamFromTournament(long teamId, long tournamentId)
        {
            await _teamService.RemoveTeamFromTournamentAsync(teamId, tournamentId);

            return NoContent();
        }
    }
}
