using Application.Contracts;
using Application.DTOs.Team;
using Application.DTOs.Tournament;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TO2.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/tournaments")]
    public class TournamentsController : ControllerBase
    {
        private readonly ITournamentService _tournamentService;
        private readonly ITeamService _teamService;
        private readonly IPlacementService _placementService;
        private readonly IWorkFlowService _workFlowService;

        public TournamentsController(
            ITournamentService tournamentService,
            ITeamService teamService,
            IPlacementService placementService,
            IWorkFlowService workFlowService)
        {
            _tournamentService = tournamentService;
            _teamService = teamService;
            _placementService = placementService;
            _workFlowService = workFlowService;
        }


        // GET: api/Tournament/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            return Ok(await _tournamentService.GetTournamentAsync(id));
        }

        // GET: api/tournaments
        [HttpGet]
        public async Task<IActionResult> GetTournamentList()
        {
            return Ok(await _tournamentService.GetTournamentListAsync());
        }

        // GET: api/Tournament/5/teams
        [HttpGet("{id}/teams")]
        public async Task<IActionResult> GetTeamsByTournament(long id)
        {
            return Ok(await _tournamentService.GetTeamsByTournamentAsync(id));
        }

        // POST: api/tournaments/5/teams
        [HttpPost("{id}/teams")]
        public async Task<IActionResult> AddTeams(long id, [FromBody] AddTeamsRequestDTO request)
        {
            return Ok(await _teamService.AddTeamsToTournamentAsync(id, request));
        }

        // GET: api/Tournament/5/state
        [HttpGet("{id}/state")]
        public async Task<IActionResult> GetTournamentState(long id)
        {
            return Ok(await _tournamentService.GetTournamentStateAsync(id));
        }

        // GET: api/tournaments/5/final-standings
        [HttpGet("{id}/final-standings")]
        public async Task<IActionResult> GetFinalStandings(long id)
        {
            return Ok(await _placementService.GetFinalResultsAsync(id));
        }

        // POST: api/Tournament
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateTournamentRequestDTO request)
        {
            var result = await _tournamentService.CreateTournamentAsync(request);

            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }

        // PUT: api/Tournament/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] UpdateTournamentRequestDTO request)
        {
            return Ok(await _tournamentService.UpdateTournamentAsync(id, request));
        }

        // POST: api/Tournament/5/start-groups
        [HttpPost("{id}/start-groups")]
        public async Task<IActionResult> StartGroups(long id)
        {
            return Ok(await _workFlowService.StartGroups(id));
        }

        // POST: api/Tournament/5/start-bracket
        [HttpPost("{id}/start-bracket")]
        public async Task<IActionResult> StartBracket(long id)
        {
            return Ok(await _workFlowService.StartBracket(id));
        }

        // POST: api/tournaments/5/cancel
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(long id, [FromBody] CancelTournamentRequestDTO request)
        {
            await _tournamentService.CancelTournamentAsync(id, request);
            return NoContent();
        }

        // DELETE: api/tournaments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _tournamentService.DeleteTournamentAsync(id);
            return NoContent();
        }

    }
}
