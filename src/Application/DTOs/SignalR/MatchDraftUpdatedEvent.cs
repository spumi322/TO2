namespace Application.DTOs.SignalR;

// A match's draft score, relayed by the hub to the others on the tournament page; never saved
public record MatchDraftUpdatedEvent(long TournamentId, long MatchId, int TeamAWins, int TeamBWins, string UpdatedBy);
