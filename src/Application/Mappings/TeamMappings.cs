using Application.DTOs.Team;
using Domain.AggregateRoots;
using Riok.Mapperly.Abstractions;

namespace Application.Mappings
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public static partial class TeamMappings
    {
        public static partial GetTeamResponseDTO ToGetTeamResponseDTO(this Team team);

        public static partial UpdateTeamResponseDTO ToUpdateTeamResponseDTO(this Team team);
    }
}
