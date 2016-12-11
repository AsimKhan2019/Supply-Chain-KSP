using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain
{
    public class VesselData : IConfigNode
    {
        private Vessel vesselRef;
        private Guid linkedID;
        public uint rootPartID;
        private bool resolved = false;

        /* Mod-specific ID.
         * This ID is general to the whole vessel, but does not change on docking events.
         */
        public Guid trackingID;

        public List<SupplyLink> links;
        public bool orbitalDockingEnabled = false;

        public VesselData()
        {
            this.links = new List<SupplyLink>();

            GameEvents.onPartUndock.Add(
                (Part p) => { handleDockingEvent(p, false); }
            );

            GameEvents.onPartCouple.Add(
                (GameEvents.FromToAction<Part, Part> evt) => { handleDockingEvent(evt.from, true); handleDockingEvent(evt.to, true); }
            );

            // Do we actually need to resolve vessel references on every scene switch?
            GameEvents.onGameSceneLoadRequested.Add((GameScenes s) => { this.resolved = false; });
        }

        public VesselData(Vessel v)
        {
            this.vesselRef = v;
            this.linkedID = v.id;
            this.rootPartID = v.rootPart.flightID;
            this.trackingID = Guid.NewGuid();
            this.resolved = true;

            GameEvents.onPartUndock.Add(
                (Part p) => { handleDockingEvent(p, false); }
            );

            GameEvents.onPartCouple.Add(
                (GameEvents.FromToAction<Part, Part> evt) => { handleDockingEvent(evt.from, true); handleDockingEvent(evt.to, true); }
            );

            this.links = new List<SupplyLink>();
        }

        public void Load(ConfigNode node)
        {
            linkedID = new Guid(node.GetValue("vesselID"));
            rootPartID = Convert.ToUInt32(node.GetValue("rootPartID"));
            trackingID = new Guid(node.GetValue("trackingID"));
            node.TryGetValue("orbitalDocking", ref orbitalDockingEnabled);
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("vesselID", linkedID.ToString());
            node.AddValue("rootPartID", rootPartID);
            node.AddValue("trackingID", trackingID.ToString());
            node.AddValue("orbitalDocking", orbitalDockingEnabled);
        }

        public void handleDockingEvent(Part p, bool dockEvent)
        {
            if(p.vessel == vessel)
            {
                if (dockEvent)
                {
                    if(vessel.situation == Vessel.Situations.ORBITING)
                    {
                        Debug.Log("[SupplyChain] Tracked vessel can now perform docking.");
                        this.orbitalDockingEnabled = true;
                    }
                }

                vesselRef = null;
                linkedID = new Guid();
                resolved = false;
            }
        }

        public void resolve()
        {
            resolved = false;
            Debug.Log("[SupplyChain] Resolving vessel reference for Tracking ID: " + this.trackingID.ToString());
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (v.id.Equals(linkedID))
                {
                    vesselRef = v;
                    rootPartID = v.protoVessel.protoPartSnapshots[v.protoVessel.rootIndex].flightID;
                    resolved = true;

                    Debug.Log("[SupplyChain] Resolved reference with Vessel ID: " + linkedID.ToString());

                    return;
                } else
                {
                    // try and match by root PartID.
                    if (v.loaded)
                    {
                        if (v.rootPart.flightID == this.rootPartID)
                        {
                            vesselRef = v;
                            linkedID = v.id;
                            resolved = true;

                            Debug.Log("[SupplyChain] Found vessel with Root Part ID: " + Convert.ToString(this.rootPartID));
                        }
                    } else
                    {
                        if (v.protoVessel.protoPartSnapshots[v.protoVessel.rootIndex].flightID == this.rootPartID)
                        {
                            vesselRef = v;
                            linkedID = v.id;
                            resolved = true;

                            Debug.Log("[SupplyChain] Found unloaded vessel with Root Part ID: " + Convert.ToString(this.rootPartID));
                        }
                    }
                    
                }

                if (resolved)
                    return;
            }


            if (!resolved)
            {
                // find by root part ID.
                Part rp = FlightGlobals.FindPartByID(this.rootPartID);
                if(rp != null)
                {
                    vesselRef = rp.vessel;
                    linkedID = rp.vessel.id;
                    resolved = true;

                    Debug.Log("[SupplyChain] Resolved reference by searching for Part ID: " + Convert.ToString(this.rootPartID));
                }
            }
        }

        public Vessel vessel
        {
            get
            {
                if(!resolved)
                {
                    resolve();
                }
                
                return vesselRef;
            }

            set
            {
                vesselRef = value;
                linkedID = value.id;
                rootPartID = value.rootPart.flightID;
                resolved = true;
            }
        }

        public Guid vesselID
        {
            get
            {
                return linkedID;
            }

            set
            {
                linkedID = value;
                resolved = false;
            }
        }

        public Dictionary<int, bool> checkResources(Dictionary<int, double> resources)
        {
            Dictionary<int, bool> ret = new Dictionary<int, bool>();

            foreach (int rsc in resources.Keys)
            {
                double current = 0;
                double max = 0;
                if (vessel.loaded)
                {
                    vessel.GetConnectedResourceTotals(rsc, out current, out max);
                }
                else
                {
                    foreach (ProtoPartSnapshot snap in vessel.protoVessel.protoPartSnapshots)
                    {
                        foreach (ProtoPartResourceSnapshot rsc_snap in snap.resources)
                        {
                            if(rsc_snap.definition.id == rsc)
                            {
                                current += rsc_snap.amount;
                                max += rsc_snap.maxAmount;
                            }
                        }
                    }
                }
                

                ret.Add(rsc, (current >= resources[rsc]));
            }

            return ret;
        }

        /**
         * Removes or adds resources to a craft. 
         * Pass in a dictionary mapping resource IDs to the amounts to drain/add for each.
         * Negative values will add resources, positive values will drain.
         */
        public void modifyResources(Dictionary<int, double> resources)
        {

            if (vessel.loaded)
            {
                Dictionary<int, List<PartResource>> partsByResource = new Dictionary<int, List<PartResource>>();

                foreach (Part p in vessel.parts)
                {
                    foreach (PartResource r in p.Resources)
                    {
                        if(!partsByResource.ContainsKey(r.info.id))
                        {
                            partsByResource.Add(r.info.id, new List<PartResource>());
                        }

                        partsByResource[r.info.id].Add(r);
                    }
                }

                foreach (int rsc in resources.Keys)
                {
                    if (partsByResource[rsc].Count == 0)
                        continue;

                    double changePerPart = resources[rsc] / partsByResource[rsc].Count;
                    double remaining = Math.Abs(resources[rsc]);

                    // Iterate over every part and drain/add as much as we can up to changePerPart.
                    // If we can't drain the full amount (drainPerPart) then empty the part and reiterate.
                    while (remaining > 0)
                    {
                        foreach (PartResource p in partsByResource[rsc])
                        {
                            if (remaining <= 0)
                                break;

                            if (remaining >= Math.Abs(changePerPart))
                            {
                                if (changePerPart > 0)
                                {
                                    if (p.amount >= changePerPart)
                                    {
                                        remaining -= changePerPart;
                                        p.amount -= changePerPart;
                                    }
                                    else
                                    {
                                        remaining -= p.amount;
                                        p.amount = 0;
                                    }
                                }
                                else
                                {
                                    if ((p.maxAmount - p.amount) <= changePerPart)
                                    {
                                        p.amount += Math.Abs(changePerPart);
                                        remaining -= Math.Abs(changePerPart);
                                    }
                                    else
                                    {
                                        remaining -= (p.maxAmount - p.amount);
                                        p.amount = p.maxAmount;
                                    }
                                }
                            }
                            else
                            {
                                p.amount -= remaining;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                /* A lot of this is duplicated from above. */
                Dictionary<int, List<ProtoPartResourceSnapshot>> partsByResource = new Dictionary<int, List<ProtoPartResourceSnapshot>>();

                foreach (ProtoPartSnapshot p in vessel.protoVessel.protoPartSnapshots)
                {
                    foreach (ProtoPartResourceSnapshot r in p.resources)
                    {
                        if (!partsByResource.ContainsKey(r.definition.id))
                        {
                            partsByResource.Add(r.definition.id, new List<ProtoPartResourceSnapshot>());
                        }

                        partsByResource[r.definition.id].Add(r);
                    }
                }

                foreach (int rsc in resources.Keys)
                {
                    if (partsByResource[rsc].Count == 0)
                        continue;

                    double changePerPart = resources[rsc] / partsByResource[rsc].Count;
                    double remaining = Math.Abs(resources[rsc]);
                    
                    while (remaining > 0)
                    {
                        foreach (ProtoPartResourceSnapshot p in partsByResource[rsc])
                        {
                            if (remaining <= 0)
                                break;

                            if (remaining >= Math.Abs(changePerPart))
                            {
                                if (changePerPart > 0)
                                {
                                    if (p.amount >= changePerPart)
                                    {
                                        remaining -= changePerPart;
                                        p.amount -= changePerPart;
                                    }
                                    else
                                    {
                                        remaining -= p.amount;
                                        p.amount = 0;
                                    }
                                }
                                else
                                {
                                    if ((p.maxAmount - p.amount) <= changePerPart)
                                    {
                                        p.amount += Math.Abs(changePerPart);
                                        remaining -= Math.Abs(changePerPart);
                                    }
                                    else
                                    {
                                        remaining -= (p.maxAmount - p.amount);
                                        p.amount = p.maxAmount;
                                    }
                                }
                            }
                            else
                            {
                                p.amount -= remaining;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
