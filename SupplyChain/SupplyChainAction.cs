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

        public List<Action<SupplyChainAction>> onComplete;
        
        /* Set to true if this action is not owned by any object in particular.
         * (i.e. if it's a oneshot non-Supply Link action.)
         */
        public bool freestanding = true;

        public bool active;
        public double timeComplete; // UT for action completion

        public abstract bool startAction();
        public abstract void finishAction();
        
        public abstract bool canExecute();
        public abstract bool canFinish();

        public void Load(ConfigNode node)
        {
            this.loadCommonData(node);
            this.LoadCustom(node);
        }

        public void Save(ConfigNode node)
        {
            this.saveCommonData(node);
            this.SaveCustom(node);
        }

        public abstract void LoadCustom(ConfigNode node);
        public abstract void SaveCustom(ConfigNode node);

        private void saveCommonData(ConfigNode node)
        {
            node.AddValue("linkVessel", linkVessel.trackingID.ToString());
            if(this.location != null)
                node.AddValue("location", location.id.ToString());
            node.AddValue("timeRequired", this.timeRequired);

            node.AddValue("active", this.active);
            if (this.active)
            {
                node.AddValue("timeAtComplete", this.timeComplete);
            }

            node.AddValue("freestanding", this.freestanding);
        }

        private void loadCommonData(ConfigNode node)
        {
            location = SupplyChainController.getPointByGuid(new Guid(node.GetValue("location")));
            node.TryGetValue("freestanding", ref this.freestanding);
            node.TryGetValue("timeRequired", ref timeRequired);

            node.TryGetValue("active", ref active);
            if (this.active)
            {
                node.TryGetValue("timeAtComplete", ref timeComplete);
            }

            /* Load linked vessel. */
            Guid linkVesselID = new Guid(node.GetValue("linkVessel"));
            foreach (VesselData vd in SupplyChainController.instance.trackedVessels)
            {
                if (vd.trackingID.Equals(linkVesselID))
                {
                    this.linkVessel = vd;
                    break;
                }
            }
        }
    }
}
