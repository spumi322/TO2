using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Common
{
    public abstract class EntityBase : IEquatable<EntityBase?>
    {
        public long Id { get; protected set; }
        public DateTime CreatedDate { get; protected set; }
        public DateTime? LastModifiedDate { get; protected set; }
        public string? CreatedBy { get; protected set; }
        public string? LastModifiedBy { get; protected set; }

        protected EntityBase()
        {
            CreatedDate = DateTime.UtcNow;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as EntityBase);
        }

        public bool Equals(EntityBase? other)
        {
            return other is not null &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public static bool operator ==(EntityBase? left, EntityBase? right)
        {
            return EqualityComparer<EntityBase>.Default.Equals(left, right);
        }

        public static bool operator !=(EntityBase? left, EntityBase? right)
        {
            return !(left == right);
        }
    }
}
