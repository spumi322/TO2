using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Team
{
    public record AddTeamToTournamentRequestDTO(long TournamentId, long TeamId); 
}
