using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Standing
{
    public record GetGroupsWithDetailsResponseDTO
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsFinished { get; init; }
        public bool IsSeeded { get; init; }
        public List<GroupTeamDTO> Teams { get; init; } = new();
        public List<StandingMatchDTO> Matches { get; init; } = new();
    }
}
