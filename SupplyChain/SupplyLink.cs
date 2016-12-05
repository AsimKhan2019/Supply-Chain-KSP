using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain
{
    public class SupplyLink : IConfigNode
    {
        public Dictionary<int, double> resourcesRequired;
        public double timeRequired;
        public double maxMass;

        public Vessel linkVessel;

        public SupplyPoint from;
        public SupplyPoint to;

        public Guid id;

        private Guid linkVesselID;

        public SupplyLink() {}
        public SupplyLink(Vessel v, SupplyPoint from, SupplyPoint to)
        {
            this.id = Guid.NewGuid();
            this.from = from;
            this.to = to;
            this.linkVessel = v;
            this.linkVesselID = v.id;
            this.timeRequired = 0;
            this.maxMass = 0;
            this.resourcesRequired = new Dictionary<int, double>();
        }

        public bool canTraverseLink()
        {
            if (linkVessel == null)
                this.resolveVessel(true);

            if(from.isVesselAtPoint(linkVessel))
            {
                if (!linkVessel.loaded)
                    linkVessel.Load();

                if (linkVessel.totalMass > maxMass)
                {
                    Debug.Log("[SupplyChain] Vessel too massive for link!");
                    return false;
                }

                /* Check resources. */
                foreach (int rsc in resourcesRequired.Keys)
                {
                    double current = 0;
                    double max = 0;

                    linkVessel.GetConnectedResourceTotals(rsc, out current, out max, true);
                    
                    if(current < resourcesRequired[rsc])
                    {
                        Debug.Log("[SupplyChain] Vessel " + linkVessel.name + " does not have enough " + PartResourceLibrary.Instance.GetDefinition(rsc).name);
                        return false;
                    }
                }
            } else {
                Debug.Log("[SupplyChain] Vessel not at supply point: " + from.name);
                return false;
            }

            return true;
        }

        public bool traverseLink()
        {
            if (!canTraverseLink())
                return false;

            /* Drain resources from the ship.
             * I have no idea how to do this "properly", so for now I'm just going to reduce the resource amounts on every part of the ship evenly.
             */

            Dictionary<int, List<PartResource>> partsByResource = new Dictionary<int, List<PartResource>>();

            Debug.Log("[SupplyChain] Attempting to enumerate PartResources.");

            foreach (Part p in linkVessel.Parts)
            {
                foreach (PartResource r in p.Resources)
                {
                    if(resourcesRequired.ContainsKey(r.info.id))
                    {
                        if (!partsByResource.ContainsKey(r.info.id))
                        {
                            partsByResource.Add(r.info.id, new List<PartResource>());
                        }

                        partsByResource[r.info.id].Add(r);
                    }
                }
            }

            Debug.Log("[SupplyChain] Now draining resources.");
            foreach (int rsc in resourcesRequired.Keys)
            {
                double drainPerPart = resourcesRequired[rsc] / partsByResource.Count;
                double needToDrain = resourcesRequired[rsc];

                // Iterate over every part and drain as much as we can up to drainPerPart.
                // If we can't drain the full amount (drainPerPart) then empty the part and reiterate.
                while(needToDrain > 0)
                {
                    foreach (PartResource p in partsByResource[rsc])
                    {
                        if (needToDrain <= 0)
                            break;

                        if (needToDrain >= drainPerPart)
                        {
                            if (p.amount >= drainPerPart)
                            {
                                needToDrain -= drainPerPart;
                                p.amount -= drainPerPart;
                            } else {
                                needToDrain -= p.amount;
                                p.amount = 0;
                            }
                        } else {
                            p.amount -= needToDrain;
                            break;
                        }
                    }
                }
            }

            Debug.Log("[SupplyChain] Moving vessel.");
            to.moveVesselToPoint(linkVessel);

            return true;
        }

        public void Load(ConfigNode node)
        {
            id = new Guid(node.GetValue("id"));
            from = SupplyChainController.getPointByGuid(new Guid(node.GetValue("from")));
            to = SupplyChainController.getPointByGuid(new Guid(node.GetValue("to")));
            node.TryGetValue("timeRequired", ref timeRequired);
            node.TryGetValue("maxMass", ref maxMass);

            /* Load linked vessel. */
            linkVesselID = new Guid(node.GetValue("linkVessel"));
            GameEvents.OnFlightGlobalsReady.Add(resolveVessel);

            /* Load resources. */
            ConfigNode[] required = node.GetNodes("RequiredResource");
            resourcesRequired = new Dictionary<int, double>();
            foreach (ConfigNode rscNode in required)
            {
                String name = rscNode.GetValue("name");
                double amount = Convert.ToDouble(rscNode.GetValue("amount"));
                resourcesRequired.Add(PartResourceLibrary.Instance.GetDefinition(name).id, amount);
            }
        }

        public void Save(ConfigNode node)
        {
            Debug.Log("[SupplyChain] Saving Link ID.");
            node.AddValue("id", id.ToString());

            Debug.Log("[SupplyChain] Saving point IDs...");
            node.AddValue("from", from.id.ToString());
            node.AddValue("to", to.id.ToString());

            Debug.Log("[SupplyChain] Saving vessel ID...");
            if (linkVessel != null)
            {
                Debug.Log("[SupplyChain] Found vessel reference.");
                node.AddValue("linkVessel", linkVessel.id.ToString());
            } else if(linkVesselID != null)
            {
                Debug.Log("[SupplyChain] Could not find vessel reference-- saving previously loaded ID...");
                node.AddValue("linkVessel", linkVesselID.ToString());
            }

            Debug.Log("[SupplyChain] Saving requirements.");
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
        }

        /* Link GUID -> vessel reference.
         * FlightGlobals.Vessels isn't populated when Load() is run, so we defer this step to later. */
        private void resolveVessel(bool something)
        {
            linkVessel = null;

            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (v.id.Equals(linkVesselID))
                {
                    linkVessel = v;
                    Debug.Log("[SupplyChain] Successfully linked supply link " + from.name + " -> " + to.name + " to vessel " + linkVesselID.ToString());
                    break;
                }
            }

            if (linkVessel == null)
            {
                Debug.LogError("[SupplyChain] Failed to find vessel with GUID " + linkVesselID.ToString());
            }
        }
    }
}
