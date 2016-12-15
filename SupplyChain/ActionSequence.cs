using SupplyChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SupplyChain
{
    public class ActionSequence : IConfigNode
    {
        public List<SupplyChainAction> sequence;
        public int currentIndex;
        public bool active;

        /* each List<SequenceCondition> represents a set of conditions that must
         * all be met for that condition set to be true.
         * 
         * If any condition set is true then the whole sequence will fire.
         */ 
        public List<List<SequenceCondition>> conditions;

        public bool canExecute()
        {
            throw new NotImplementedException();
        }

        public bool checkConditions()
        {
            foreach(List<SequenceCondition> conditionSet in conditions)
            {
                bool status = true;
                foreach(SequenceCondition condition in conditionSet)
                {
                    status = status && condition.check();
                }

                if(status)
                    return true;
            }

            return false;
        }

        public void startSequence()
        {
            throw new NotImplementedException();
        }

        public void Load(ConfigNode node)
        {
            /* Load action ConfigNodes in order.
             * I'm pretty sure node.GetNodes() won't return the nodes in sequence,
             * so we'll have to sort it first.
             */

            List<ConfigNode> nodes = new List<ConfigNode>(node.GetNodes("Action"));
            nodes.Sort(
                (ConfigNode comp1, ConfigNode comp2) =>
                {
                    int idx1 = 0;
                    int idx2 = 0;
                    comp1.TryGetValue("idx", ref idx1);
                    comp2.TryGetValue("idx", ref idx2);

                    return idx1.CompareTo(idx2);
                }
            );

            foreach(ConfigNode actNode in nodes)
            {
                int idx = 0;
                actNode.TryGetValue("idx", ref idx);
                string type = actNode.GetValue("type");
                if(type == "SupplyLink")
                {
                    SupplyLink act = SupplyChainController.getLinkByGuid(new Guid(actNode.GetValue("id")));
                } else if (type == "ResourceTransfer")
                {
                    ResourceTransferAction act = new ResourceTransferAction();
                    act.Load(actNode);
                }
            }
        }

        public void Save(ConfigNode node)
        {
            for (int i = 0; i < this.sequence.Count; i++)
            {
                ConfigNode act = node.AddNode("Action");
                act.AddValue("idx", i);

                if(this.sequence[i].GetType() != typeof(SupplyLink))
                {
                    this.sequence[i].Save(act);
                } else
                {
                    act.AddValue("type", "SupplyLink");
                    act.AddValue("id", ((SupplyLink)this.sequence[i]).id.ToString());
                }
            }
        }

        
    }
}
