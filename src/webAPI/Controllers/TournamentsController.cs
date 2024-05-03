using Microsoft.AspNetCore.Mvc;

namespace TO2.Controllers
{
    [ApiController]
    [Route("api/tournaments")]
    public class TournamentsController : ControllerBase
    {
        private readonly ITournamentService _tournamentService;

        // GET: api/Tournaments
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok();
        }

        // GET: api/Tournament/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            return Ok();
        }

        // POST: api/Tournament
        [HttpPost]
        public IActionResult Post([FromBody] string value)
        {
            return Ok();
        }

        // PUT: api/Tournament/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] string value)
        {
            return Ok();
        }

        // DELETE: api/Tournament/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            return Ok();
        }
    }
}
