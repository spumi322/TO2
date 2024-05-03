using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ValueObjects
{
    public class Prize
    {
        public int Place { get; private set; }
        public string Amount { get; private set; }
    }
}
