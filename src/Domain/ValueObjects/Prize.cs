using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ValueObjects
{
    public class Prize : ValueObjectBase
    {
        private Prize()
        {
        }

        public int Place { get; private set; }

        public string Amount { get; private set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Place;
            yield return Amount;
        }
    }
}
