namespace Application.DTOs.Match
{
    /// <summary>
    /// Enhanced DTO that includes tournament lifecycle state information.
    /// Allows frontend to know immediately when state transitions occur (e.g., bracket seeding).
    /// </summary>
    public record MatchResultDTO(
        long WinnerId,
        long LoserId,
        bool AllGroupsFinished = false,
        bool BracketSeeded = false,
        string? BracketSeedMessage = null
    );
}
