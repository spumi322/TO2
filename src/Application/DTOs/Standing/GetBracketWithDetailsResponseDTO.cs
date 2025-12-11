using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Standing
{
    public record GetBracketWithDetailsResponseDTO
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsFinished { get; init; }
        public bool IsSeeded { get; init; }
        public List<StandingMatchDTO> Matches { get; init; } = new();
    }
}
