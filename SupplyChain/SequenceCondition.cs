using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SupplyChain
{
    public abstract class SequenceCondition
    {
        public abstract bool check();
    }

    public class PeriodicSequenceCondition : SequenceCondition
    {
        public override bool check()
        {
            throw new NotImplementedException();
        }
    }
}
