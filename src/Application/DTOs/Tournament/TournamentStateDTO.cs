using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Tournament
{
    public record TournamentStateDTO(
        TournamentStatus CurrentStatus,
        bool IsTransitionState,
        bool IsActiveState,
        bool CanScoreMatches,
        bool CanModifyTeams,
        string StatusDisplayName,
        string StatusDescription
    );
}
