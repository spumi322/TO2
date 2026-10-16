namespace Application.DTOs.Standing
{
    /// <summary>
    /// A team entering the bracket.
    /// GroupId/GroupPlacement (1 = group winner) are set for teams advancing from groups, null otherwise.
    /// </summary>
    public record BracketEntrant(Domain.AggregateRoots.Team Team, long? GroupId, int? GroupPlacement);
}
