using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Standing
{
    // Shared game DTO (used by both groups and brackets)
    public record StandingGameDTO
    {
        public long Id { get; init; }
        public long MatchId { get; init; }
        public long? TeamAId { get; init; }
        public int? TeamAScore { get; init; }
        public long? TeamBId { get; init; }
        public int? TeamBScore { get; init; }
        public long? WinnerId { get; init; }
        public byte[]? RowVersion { get; init; }
    }
}
