using Domain.Common;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.AggregateRoots
{
    public class Tenant : AggregateRootBase
    {
        private readonly List<User> _users = new ();

        private Tenant() { }

        [Required]
        public string Name { get; set; }

        public bool IsActive { get; set; } = true;

        public IReadOnlyList<User> Users => _users;
    }
}
