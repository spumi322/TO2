using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Standing
{
    // Consolidated match DTO for both groups and bracket
    public record StandingMatchDTO
    {
        public long Id { get; init; }
        public long StandingId { get; init; }
        public int? Round { get; init; }
        public int? Seed { get; init; }
        public long? TeamAId { get; init; }
        public long? TeamBId { get; init; }
        public long? WinnerId { get; init; }
        public long? LoserId { get; init; }
        public int BestOf { get; init; }

        // Backend-calculated results
        public int TeamAWins { get; init; }
        public int TeamBWins { get; init; }

        public List<StandingGameDTO> Games { get; init; } = new();
    }
}
