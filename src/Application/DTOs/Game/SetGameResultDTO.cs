using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Game
{
    public record SetGameResultDTO(long gameId, long WinnerId, int? TeamAScore, int? TeamBScore, long MatchId, long StandingId, long TournamentId);
}
