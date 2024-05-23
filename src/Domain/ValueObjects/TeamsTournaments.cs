using Domain.AggregateRoots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ValueObjects
{
    public class TeamsTournaments
    {
        public long TeamId { get; set; }
        public Team Team { get; set; }

        public long TournamentId { get; set; }
        public Tournament Tournament { get; set; }
    }
}
