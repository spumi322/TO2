namespace Application.DTOs.Tournament
{
    public record TeamPlacementDTO(
        long TeamId,
        string TeamName,
        int Placement,
        int EliminatedInRound,
        // Set when the placement is a group position (GroupsOnly, or knocked out in the groups)
        string? GroupName
    );
}
