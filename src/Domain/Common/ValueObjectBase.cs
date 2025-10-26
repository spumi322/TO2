namespace Domain.Common
{
    public abstract class ValueObjectBase
    {
        protected abstract IEnumerable<object> GetEqualityComponents();

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            var valueObject = (ValueObjectBase)obj;

            return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x != null ? x.GetHashCode() : 0)
                .Aggregate((x, y) => x ^ y);
        }

        public static bool operator ==(ValueObjectBase left, ValueObjectBase right)
        {
            return left?.Equals(right) ?? right is null;
        }

        public static bool operator !=(ValueObjectBase left, ValueObjectBase right)
        {
            return !(left == right);
        }
    }
}
