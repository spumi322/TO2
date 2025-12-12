namespace Application.DTOs.Game
{
    public record SetGameResultResponseDTO(bool Success, long? matchId, byte[]? RowVersion);
}
