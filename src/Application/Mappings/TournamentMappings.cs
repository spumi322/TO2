using Application.DTOs.Tournament;
using Domain.AggregateRoots;
using Riok.Mapperly.Abstractions;

namespace Application.Mappings
{
    // Mapperly generates the method bodies at compile time
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public static partial class TournamentMappings
    {
        // NumberOfGroups is not a tournament field; StandingService uses it to create the groups
        [MapperRequiredMapping(RequiredMappingStrategy.Source)]
        [MapperIgnoreSource(nameof(CreateTournamentRequestDTO.NumberOfGroups))]
        public static partial Tournament ToTournament(this CreateTournamentRequestDTO request);

        public static partial GetTournamentResponseDTO ToGetTournamentResponseDTO(this Tournament tournament);

        public static partial GetTournamentListResponseDTO ToGetTournamentListResponseDTO(this Tournament tournament, int currentTeams);

        public static partial UpdateTournamentResponseDTO ToUpdateTournamentResponseDTO(this Tournament tournament);
    }
}
