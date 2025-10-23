using Application.DTOs.Tournament;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Orchestration
{
    public record GameProcessResultDTO(
        bool Success,
        bool MatchFinished,
        long? MatchWinnerId = null,
        long? MatchLoserId = null,
        bool StandingFinished = false,
        bool AllGroupsFinished = false,
        bool TournamentFinished = false,
        TournamentStatus? NewTournamentStatus = null,
        List<TeamPlacementDTO>? FinalStandings = null,
        string? Message = null
    );
}
