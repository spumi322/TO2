namespace Application.DTOs.Match
{
    // A confirmed match result: game wins per team; Version = the match version the client loaded
    public record SetMatchResultDTO(long MatchId, long StandingId, long TournamentId, int TeamAWins, int TeamBWins, long Version);
}
