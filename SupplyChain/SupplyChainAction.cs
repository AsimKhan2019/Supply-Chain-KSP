using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SupplyChain
{
    public abstract class SupplyChainAction : IConfigNode
    {
        public double timeRequired;
        public SupplyPoint location;
        public VesselData linkVessel;

        public bool active;
        public double timeComplete; // UT for action completion

        public abstract bool startAction();
        public abstract void finishAction();
        
        public abstract bool canExecute();
        public abstract bool canFinish();

        public abstract void Load(ConfigNode node);
        public abstract void Save(ConfigNode node);
    }
}
