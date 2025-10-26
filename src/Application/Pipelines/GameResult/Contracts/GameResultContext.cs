using Application.DTOs.Game;
using Application.DTOs.Tournament;
using Domain.AggregateRoots;
using Domain.Enums;

namespace Application.Pipelines.GameResult.Contracts
{
    /// <summary>
    /// Context object that holds all data passed between pipeline steps.
    /// Acts as a shared state for the entire game result processing pipeline.
    /// </summary>
    public class GameResultContext
    {
        // Input data
        public SetGameResultDTO GameResult { get; set; } = null!;

        // Loaded entities
        public Tournament Tournament { get; set; } = null!;

        // Match completion data
        public bool MatchFinished { get; set; }
        public long? MatchWinnerId { get; set; }
        public long? MatchLoserId { get; set; }

        // Standing data
        public StandingType? StandingType { get; set; }
        public bool StandingFinished { get; set; }
        public bool AllGroupsFinished { get; set; }

        // Tournament state
        public bool TournamentFinished { get; set; }
        public TournamentStatus? NewTournamentStatus { get; set; }

        // Final results
        public List<TeamPlacementDTO>? FinalStandings { get; set; }

        // Response message
        public string? Message { get; set; }

        // Flags to control pipeline flow
        public bool ShouldContinue { get; set; } = true;
        public bool Success { get; set; } = true;
    }
}
