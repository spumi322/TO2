using Application.Contracts;
using Application.DTOs.Tournament;
using Application.Services;
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
        private readonly ITeamService _teamService;

        public TournamentsController(ITournamentService tournamentService, ITeamService teamService)
        {
            _tournamentService = tournamentService;
            _teamService = teamService;
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

        // GET: api/Teams/tournament/5
        [HttpGet("{id}/teams")]
        public async Task<IActionResult> GetByTournament(long id)
        {
            return Ok(await _tournamentService.GetTeamsByTournamentAsync(id));
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

        // PUT: api/Tournament/5/status
        [HttpPut("{id}/{status}")]
        public async Task<IActionResult> Put(long id, [FromRoute][Required] TournamentStatus status)
        {
            await _tournamentService.SetTournamentStatusAsync(id, status);

            return Ok();
        }

        // DELETE: api/Tournament/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _tournamentService.SoftDeleteTournamentAsync(id);

            return NoContent();
        }


        // DELETE: api/Tournament/5/5
        [HttpDelete("{teamId}/{tournamentId}")]
        public async Task<IActionResult> RemoveTeam(long teamId, long tournamentId)
        {
            await _tournamentService.RemoveTeamFromTournamentAsync(teamId, tournamentId);

            return NoContent();
        }

        // PUT: api/tournaments/5/start
        [HttpPut("{id}/start")]
        public async Task<IActionResult> StartTournament(long id)
        {
            var result = await _tournamentService.StartTournament(id);

            return result.Success ? Ok() : BadRequest();
        }

        [HttpGet("/check-unique/{name}")]
        public async Task<IActionResult> CheckTournamentNameIsUnique(string name)
        {
            return Ok(await _tournamentService.CheckNameIsUniqueAsync(name));
        }

        // GET: api/tournaments/5/champion
        [HttpGet("{id}/champion")]
        public async Task<IActionResult> GetChampion(long id)
        {
            var champion = await _tournamentService.GetChampion(id);
            return champion != null ? Ok(champion) : NotFound();
        }

        // GET: api/tournaments/5/final-standings
        [HttpGet("{id}/final-standings")]
        public async Task<IActionResult> GetFinalStandings(long id)
        {
            return Ok(await _tournamentService.GetFinalStandings(id));
        }

        /// <summary>
        /// Starts the group stage (Setup -> GroupsInProgress).
        /// </summary>
        [HttpPost("{id}/start-groups")]
        public async Task<IActionResult> StartGroups(long id)
        {
            var result = await _tournamentService.StartGroups(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Gets the current state machine status.
        /// </summary>
        [HttpGet("{id}/state")]
        public async Task<IActionResult> GetTournamentState(long id)
        {
            return Ok(await _tournamentService.GetTournamentState(id));
        }
    }
}
