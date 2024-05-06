using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Tournament
{
    public record UpdateTournamentRequestDTO(string Name, string Description, DateTime StartDate, DateTime EndDate);
}
