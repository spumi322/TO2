using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Game
{
    public record SetGameResultResponseDTO(bool Success, long? matchId);
}
