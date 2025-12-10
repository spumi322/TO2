using Domain.Common;
using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Domain.AggregateRoots
{
    public class Tenant : AggregateRootBase
    {
        private readonly List<User> _users = new();

        public Tenant() { }

        [Required]
        public string Name { get; set; }

        public bool IsActive { get; set; } = true;

        public IReadOnlyList<User> Users => _users;
    }
}
