﻿using Domain.AggregateRoots;
using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Player : EntityBase
    {
        public string Name { get; private set; }

        public Team? Team { get; private set; }
    }
}
