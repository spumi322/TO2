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

        // POST: api/Tournament/5/5
        [HttpPost("{teamId}/{tournamentId}")]
        public async Task<IActionResult> AddTeam(long teamId, long tournamentId)
        {
            return Ok(await _tournamentService.AddTeamToTournamentAsync(teamId, tournamentId));
        }

        // DELETE: api/Tournament/5/5
        [HttpDelete("{tournamentId}/{teamId}")]
        public async Task<IActionResult> RemoveTeam(long tournamentId, long teamId)
        {
            await _tournamentService.RemoveTeamFromTournamentAsync(tournamentId, teamId);

            return NoContent();
        }
    }
}
