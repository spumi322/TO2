using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Game
{
    public record SetGameResultDTO(long WinnerId, int? TeamAScore, int? TeamBScore);
}
