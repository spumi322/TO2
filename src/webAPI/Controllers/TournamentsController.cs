using Application.Contracts;
using Application.DTOs.Tournament;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace TO2.Controllers
{
    [ApiController]
    [Route("api/tournaments")]
    public class TournamentsController : ControllerBase
    {
        private readonly ITournamentService _tournamentService;

        public TournamentsController(ITournamentService tournamentService)
        {
            _tournamentService = tournamentService;
        }

        // GET: api/Tournaments
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _tournamentService.GetAllTournamentsAsync());
        }

        // GET: api/Tournament/5
        [HttpGet("id")]
        public async Task<IActionResult> Get([FromQuery][Required][Range(1, long.MaxValue)] long id)
        {
            return Ok(await _tournamentService.GetTournamentAsync(id));
        }

        // POST: api/Tournament
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateTournamentRequestDTO request)
        {
            return Ok(await _tournamentService.CreateTournamentAsync(request));
        }

        // PUT: api/Tournament/5
        [HttpPut("id")]
        public async Task<IActionResult> Put([FromQuery][Required][Range(1, long.MaxValue)] long id, [FromBody] UpdateTournamentRequestDTO request)
        {
            return Ok(await _tournamentService.UpdateTournamentAsync(id, request));
        }

        // DELETE: api/Tournament/5
        [HttpDelete("id")]
        public async Task<IActionResult> Delete(long id)
        {
            await _tournamentService.SoftDeleteTournamentAsync(id);

            return NoContent();
        }
    }
}
