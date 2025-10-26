using Domain.Common;

namespace Domain.ValueObjects
{
    public class Prize : ValueObjectBase
    {
        private Prize() { }

        public int Place { get; private set; }

        public string Amount { get; private set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Place;
            yield return Amount;
        }
    }
}
