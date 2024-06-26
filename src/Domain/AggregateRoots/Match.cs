﻿using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.AggregateRoots
{
    public class Match : AggregateRootBase
    {
        private readonly List<Game> _games = new();

        public long StandingId { get; set; }

        public Team TeamA { get; set; }

        public Team TeamB { get; set; }

        public BestOf BestOf { get; set; }

        public IReadOnlyList<Game> Games => _games;
    }
}
