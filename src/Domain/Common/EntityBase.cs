using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Common
{
    public abstract class EntityBase : IEquatable<EntityBase?>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }

        private readonly List<DomainEvent> _domainEvents = new();
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected EntityBase()
        {
            CreatedDate = DateTime.UtcNow;
            LastModifiedDate = CreatedDate;
        }

        public void AddDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void RemoveDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        public void ClearEvents()
        {
            _domainEvents.Clear();
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as EntityBase);
        }

        public bool Equals(EntityBase? other)
        {
            if (other is null)
                return false;

            // If both entities are transient (not yet persisted), use reference equality
            if (Id == 0 && other.Id == 0)
                return ReferenceEquals(this, other);

            return Id == other.Id;
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
