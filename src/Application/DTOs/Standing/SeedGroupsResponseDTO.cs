using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Standing
{
    public record SeedGroupsResponseDTO (string Response, bool Success, List<long> StandingId);
}
