using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Tournament
{
    public record GetAllTournamentsResponseDTO(long Id, string Name, string Description, int MaxTeams, TournamentStatus Status);
}
