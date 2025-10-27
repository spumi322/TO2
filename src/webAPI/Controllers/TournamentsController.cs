using Application.Contracts;
using Application.DTOs.Tournament;
using Application.Pipelines.StartGroups.Contracts;
using Application.Pipelines.StartBracket.Contracts;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace TO2.Controllers
{
    [ApiController]
    [Route("api/tournaments")]
    public class TournamentsController : ControllerBase
    {
        private readonly ITournamentService _tournamentService;
        private readonly IStandingService _standingService;
        private readonly IWorkFlowService _workFlowService;

        public TournamentsController(
            ITournamentService tournamentService,
            IStandingService standingService,
            IWorkFlowService workFlowService)
        {
            _tournamentService = tournamentService;
            _standingService = standingService;
            _workFlowService = workFlowService;
        }

        // GET: api/Tournaments
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _tournamentService.GetAllTournamentsAsync());
        }

        // GET: api/Tournament/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            return Ok(await _tournamentService.GetTournamentAsync(id));
        }

        // GET: api/Tournament/5/teams
        [HttpGet("{id}/teams")]
        public async Task<IActionResult> GetTeamsByTournament(long id)
        {
            return Ok(await _tournamentService.GetTeamsByTournamentAsync(id));
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
            return Ok(await _standingService.GetFinalResultsAsync(id));
        }

        // POST: api/Tournament
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateTournamentRequestDTO request)
        {
            return Ok(await _tournamentService.CreateTournamentAsync(request));
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
            var result = await _workFlowService.StartGroups(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // POST: api/Tournament/5/start-bracket
        [HttpPost("{id}/start-bracket")]
        public async Task<IActionResult> StartBracket(long id)
        {
            var result = await _workFlowService.StartBracket(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PUT: api/Tournament/5/status
        [HttpPut("{id}/{status}")]
        public async Task<IActionResult> Put(long id, [FromRoute][Required] TournamentStatus status)
        {
            await _tournamentService.SetTournamentStatusAsync(id, status);

            return Ok();
        }

        // PUT: api/Tournament/5/start
        [HttpPut("{id}/start")]
        public async Task<IActionResult> StartTournament(long id)
        {
            var result = await _tournamentService.StartTournament(id);

            return result.Success ? Ok() : BadRequest();
        }

    }
}
