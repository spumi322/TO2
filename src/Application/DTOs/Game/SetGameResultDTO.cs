namespace Application.DTOs.Game
{
    public record SetGameResultDTO(long gameId, long WinnerId, int? TeamAScore, int? TeamBScore, long MatchId, long StandingId, long TournamentId, byte[]? RowVersion);
}
