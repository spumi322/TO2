using Application.Contracts;
using Domain.AggregateRoots;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TO2.Controllers
{
    [ApiController]
    [Route("api/standings")]
    public class StandingsController : ControllerBase
    {
        private readonly IStandingService _standingService;
        private readonly IMatchService _matchService;
        private readonly IGameService _gameService;

        public StandingsController(IStandingService standingService, IMatchService matchService, IGameService gameService)
        {
            _standingService = standingService;
            _matchService = matchService;
            _gameService = gameService;
        }

        [HttpGet("{tournamentId}")]
        public async Task<IActionResult> GetAll(long tournamentId)
        {
            return Ok(await _standingService.GetStandingsAsync(tournamentId));
        }

        [HttpGet("{standingId}/matches")]
        public async Task<IActionResult> GetMatches(long standingId)
        {
            return Ok(await _matchService.GetMatchesAsync(standingId));
        }

        [HttpGet("{standingId}/teams")]
        public async Task<IActionResult> GetTeams(long standingId)
        {
            return Ok(await _matchService.GetTeamsAsync(standingId));
        }

        [HttpPost("{tournamentId}/generate-groupmatches")]
        public async Task<IActionResult> GenerateGroupMatches(long tournamentId)
        {
            await _matchService.SeedGroups(tournamentId);

            return Ok();
        }

        [HttpPost("{tournamentId}/generate-bracketmatches")]
        public async Task<IActionResult> GenerateBracketMatches(long tournamentId)
        {
            List<Team> playOffTeams = [/* get teams from groups result */];

            await _matchService.SeedBracket(tournamentId, playOffTeams);

            return Ok();
        }

        [HttpPost("{standingId}/generate-games")]
        public async Task<IActionResult> GenerateGames(long standingId)
        {
            var matches = await _matchService.GetMatchesAsync(standingId);

            foreach (var match in matches)
            {
                await _gameService.GenerateGames(match.Id);
            }

            return Ok();
        }
    }
}
