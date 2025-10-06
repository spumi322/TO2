using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Standing
{
    public class BracketSeedResponseDTO
    {
        public string Message { get; }
        public bool Success { get; }

        public BracketSeedResponseDTO(string message, bool success)
        {
            Message = message;
            Success = success;
        }
    }
}
