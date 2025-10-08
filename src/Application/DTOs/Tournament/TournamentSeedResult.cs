using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Tournament
{
    public record TournamentSeedResult(bool Success, List<long> CreatedStandingsId, string ErrorMessage, string? FailedStep);
}
