using Application.DTOs.Standing;
using Domain.AggregateRoots;
using Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Application.Mappings
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target, EnumMappingStrategy = EnumMappingStrategy.ByValue)]
    public static partial class StandingMappings
    {
        [MapProperty(nameof(GroupEntry.TeamId), nameof(GroupTeamDTO.Id))]
        [MapProperty(nameof(GroupEntry.TeamName), nameof(GroupTeamDTO.Name))]
        public static partial GroupTeamDTO ToGroupTeamDTO(this GroupEntry entry);

        public static partial StandingGameDTO ToStandingGameDTO(this Game game);

        // StandingService calculates the wins
        [MapperIgnoreTarget(nameof(StandingMatchDTO.TeamAWins))]
        [MapperIgnoreTarget(nameof(StandingMatchDTO.TeamBWins))]
        public static partial StandingMatchDTO ToStandingMatchDTO(this Match match);
    }
}
