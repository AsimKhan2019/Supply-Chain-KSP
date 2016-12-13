using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain
{
    public class SupplyLink : SupplyChainAction, IConfigNode
    {
        public Dictionary<int, double> resourcesRequired;
        public double maxMass;
        
        public SupplyPoint to;

        public Guid id;

        private Guid linkVesselID;
        public Guid linkedID
        {
            get
            {
                return linkVesselID;
            }
        }

        public SupplyLink() {}
        public SupplyLink(VesselData v, SupplyPoint from, SupplyPoint to)
        {
            this.id = Guid.NewGuid();
            this.location = from;
            this.to = to;

            this.linkVessel = v;
            this.linkVesselID = v.trackingID;
            v.links.Add(this);

            this.timeRequired = 0;
            this.maxMass = 0;
            this.resourcesRequired = new Dictionary<int, double>();
        }

        public bool checkVesselPosition()
        {
            return location.isVesselAtPoint(linkVessel.vessel);
        }

        public bool checkVesselMass()
        {
            return (linkVessel.vessel.totalMass < maxMass);
        }

        public Dictionary<int, bool> checkVesselResources()
        {
            return linkVessel.checkResources(this.resourcesRequired);
        }

        public bool checkVesselResourceStatus()
        {
            Dictionary<int, bool> resourceStatus = this.checkVesselResources();
            foreach(bool s in resourceStatus.Values)
            {
                if (!s)
                    return false;
            }

            return true;
        }


        public override bool canExecute()
        {
            return (
                this.checkVesselPosition() &&
                this.checkVesselMass() &&
                this.checkVesselResourceStatus()
            );
        }

        public override bool canFinish()
        {
            return true;
        }

        public override void finishAction()
        {
            Debug.Log("[SupplyChain] Moving vessel.");
            to.moveVesselToPoint(linkVessel.vessel);

            this.active = false;
        }

        public override bool startAction()
        {
            if (!canExecute())
                return false;

            if (this.active)
                return false;

            Debug.Log("[SupplyChain] Draining resources.");
            linkVessel.modifyResources(this.resourcesRequired);

            this.active = true;
            this.timeComplete = Planetarium.GetUniversalTime() + this.timeRequired;

            return true;
        }

        public void Load(ConfigNode node)
        {
            id = new Guid(node.GetValue("id"));
            location = SupplyChainController.getPointByGuid(new Guid(node.GetValue("from")));
            to = SupplyChainController.getPointByGuid(new Guid(node.GetValue("to")));
            node.TryGetValue("timeRequired", ref timeRequired);
            node.TryGetValue("maxMass", ref maxMass);

            /* Load linked vessel. */
            linkVesselID = new Guid(node.GetValue("linkVessel"));
            foreach(VesselData vd in SupplyChainController.instance.trackedVessels)
            {
                if(vd.trackingID.Equals(linkVesselID))
                {
                    this.linkVessel = vd;
                    vd.links.Add(this);
                    break;
                }
            }

            /* Load resources. */
            ConfigNode[] required = node.GetNodes("RequiredResource");
            resourcesRequired = new Dictionary<int, double>();
            foreach (ConfigNode rscNode in required)
            {
                String name = rscNode.GetValue("name");
                double amount = Convert.ToDouble(rscNode.GetValue("amount"));
                resourcesRequired.Add(PartResourceLibrary.Instance.GetDefinition(name).id, amount);
            }

            node.TryGetValue("active", ref active);
            if(this.active)
            {
                node.TryGetValue("timeAtComplete", ref timeComplete);
            }
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("id", id.ToString());
            node.AddValue("from", location.id.ToString());
            node.AddValue("to", to.id.ToString());
            
            node.AddValue("linkVessel", linkVessel.trackingID);
            
            node.AddValue("timeRequired", timeRequired);
            node.AddValue("maxMass", maxMass);
            if (resourcesRequired != null)
            {
                foreach (int rsc in resourcesRequired.Keys)
                {
                    ConfigNode rscNode = node.AddNode("RequiredResource");
                    rscNode.AddValue("name", PartResourceLibrary.Instance.GetDefinition(rsc).name);
                    rscNode.AddValue("amount", resourcesRequired[rsc]);
                }
            }

            node.AddValue("active", this.active);
            if(this.active)
            {
                node.AddValue("timeAtComplete", this.timeComplete);
            }
        }
    }
}
