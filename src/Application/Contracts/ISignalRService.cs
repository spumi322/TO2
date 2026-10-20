using Application.DTOs.SignalR;

namespace Application.Contracts
{
    public interface ISignalRService
    {
        Task BroadcastTournamentCreated(long tournamentId, string createdBy);
        Task BroadcastTournamentUpdated(long tournamentId, string updatedBy);
        Task BroadcastGameUpdated(GameUpdatedEvent eventPayload);
        Task BroadcastTeamAdded(long tournamentId, IReadOnlyList<long> teamIds, string updatedBy);
        Task BroadcastTeamRemoved(long tournamentId, long teamId, string updatedBy);
        Task BroadcastGroupsStarted(long tournamentId, string updatedBy);
        Task BroadcastBracketStarted(long tournamentId, string updatedBy);
        Task BroadcastTournamentDeleted(long tournamentId, string deletedBy);
        Task BroadcastTournamentCancelled(long tournamentId, string cancelledBy);
    }
}
