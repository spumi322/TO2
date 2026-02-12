namespace Application.DTOs.Team
{
    public record UpdateTeamRequestDTO(string Name, byte[]? RowVersion);
}
