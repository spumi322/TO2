namespace Application.DTOs.Match
{
    public record CheckRoundResultDTO(
        bool IsTournamentComplete,
        long? ChampionTeamId,
        string Message
    );
}
