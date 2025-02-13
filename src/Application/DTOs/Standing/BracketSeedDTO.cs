using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Standing
{
    public class BracketSeedDTO
    {
        public long TeamId { get; set; }
        public long GroupId { get; set; }
        public int Placement { get; set; }
        public string? TeamName { get; set; }
    }
}
