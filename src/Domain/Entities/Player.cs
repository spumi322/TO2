using Domain.AggregateRoots;
using Domain.Common;

namespace Domain.Entities
{
    public class Player : EntityBase
    {
        private Player() { }

        public string Name { get; private set; }

        public Team? Team { get; private set; }
    }
}
