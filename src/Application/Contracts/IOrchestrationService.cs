using Application.DTOs.Standing;
using Application.DTOs.Tournament;
using Domain.AggregateRoots;

namespace Application.Contracts
{
    /// <summary>
    /// Manages explicit tournament lifecycle state transitions.
    /// Replaces domain events with synchronous, testable state machine pattern.
    /// </summary>
    public interface IOrchestrationService
    {
        Task<StartGroupsResponseDTO> StartGroups(long tournamentId);
        Task<SeedGroupsResponseDTO> SeedGroups(long tournamentId);
        Task<StartBracketResponseDTO> StartBracket(long tournamentId);
        Task<SeedBracketResponseDTO> SeedBracket(long tournamentId, List<Team> teams);
    }
}
