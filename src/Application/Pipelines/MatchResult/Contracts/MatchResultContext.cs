using Application.DTOs.Tournament;
using Domain.AggregateRoots;
using Domain.Enums;

namespace Application.Pipelines.MatchResult.Contracts
{
    /// <summary>
    /// Shared state of the match result pipeline: the confirmed match (winner already set), then what each step changed.
    /// </summary>
    public class MatchResultContext
    {
        // Input: the confirmed match
        public Tournament Tournament { get; set; } = null!;
        public long StandingId { get; set; }
        public long MatchId { get; set; }
        public long WinnerId { get; set; }
        public long LoserId { get; set; }
        // The last written game: the one that decided the match (sent in the GameUpdated event)
        public long DecidingGameId { get; set; }

        // Standing data
        public StandingType? StandingType { get; set; }
        public bool StandingFinished { get; set; }
        public bool RoundFinished { get; set; }
        public bool AllGroupsFinished { get; set; }

        // Tournament state
        public bool TournamentFinished { get; set; }
        public TournamentStatus? NewTournamentStatus { get; set; }

        // Final results
        public List<TeamPlacementDTO>? FinalStandings { get; set; }

        // Response message
        public string? Message { get; set; }
    }
}
