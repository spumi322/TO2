using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Standing
{
    public record GroupMatchDTO
    {
        public long Id { get; init; }
        public long StandingId { get; init; }  // Required by match component for game result
        public int? Round { get; init; }
        public int? Seed { get; init; }
        public long? TeamAId { get; init; }
        public long? TeamBId { get; init; }
        public long? WinnerId { get; init; }
        public long? LoserId { get; init; }
        public int BestOf { get; init; }
    }
}
