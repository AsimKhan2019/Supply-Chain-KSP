using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SupplyChain.Sequencing
{
    public abstract class SequenceCondition : IConfigNode
    {
        public abstract bool check();
        public abstract void Load(ConfigNode node);
        public abstract void Save(ConfigNode node);

        public abstract void onProcessStart(SupplyChainProcess proc);

        /* Loads and constructs a specific subclass of SequenceCondition based on a config node. */
        public static SequenceCondition LoadFactory(ConfigNode node)
        {
            string type = node.GetValue("type");

            switch(type)
            {
                case "periodic":
                    PeriodicSequenceCondition newCond = new PeriodicSequenceCondition();
                    newCond.Load(node);
                    return newCond;
                default:
                    return null;
            }
        }
    }

    /* Configures a node to run every N seconds. */
    public class PeriodicSequenceCondition : SequenceCondition
    {
        public int period;
        public double lastFired;

        public override bool check()
        {
            if (Planetarium.GetUniversalTime() >= lastFired + period)
                return true;
            return false;
        }

        public override void Load(ConfigNode node)
        {
            node.TryGetValue("period", ref period);
            node.TryGetValue("lastFired", ref lastFired);
        }

        public override void Save(ConfigNode node)
        {
            node.AddValue("type", "periodic");

            node.AddValue("period", period);
            node.AddValue("lastFired", lastFired);
        }

        public override void onProcessStart(SupplyChainProcess proc)
        {
            lastFired = Planetarium.GetUniversalTime();
        }
    }
}
