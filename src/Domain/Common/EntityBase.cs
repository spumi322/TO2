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
