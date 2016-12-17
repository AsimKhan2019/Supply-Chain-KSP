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

        public SupplyPoint currentLocation;

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

            GameEvents.onFlightReady.Add(this.periodicUpdate);
            GameEvents.onTimeWarpRateChanged.Add(this.periodicUpdate);
        }

        public VesselData(Vessel v)
        {
            this.vesselRef = v;
            this.linkedID = v.id;
            if (v.loaded)
            {
                this.rootPartID = v.rootPart.flightID;
            } else
            {
                this.rootPartID = v.protoVessel.protoPartSnapshots[v.protoVessel.rootIndex].flightID;
            }
            this.trackingID = Guid.NewGuid();
            this.resolved = true;

            GameEvents.onPartUndock.Add(
                (Part p) => { handleDockingEvent(p, false); }
            );

            GameEvents.onPartCouple.Add(
                (GameEvents.FromToAction<Part, Part> evt) => { handleDockingEvent(evt.from, true); handleDockingEvent(evt.to, true); }
            );

            this.links = new List<SupplyLink>();

            GameEvents.onFlightReady.Add(this.periodicUpdate);
            GameEvents.onTimeWarpRateChanged.Add(this.periodicUpdate);
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

        public void getResourceCount(int resource, out double current, out double max)
        {
            if (vessel.loaded)
            {
                vessel.GetConnectedResourceTotals(resource, out current, out max);
            }
            else
            {
                double c = 0;
                double m = 0;

                foreach (ProtoPartSnapshot snap in vessel.protoVessel.protoPartSnapshots)
                {
                    foreach (ProtoPartResourceSnapshot rsc_snap in snap.resources)
                    {
                        if (rsc_snap.definition.id == resource)
                        {
                            c += rsc_snap.amount;
                            m += rsc_snap.maxAmount;
                        }
                    }
                }

                current = c;
                max = m;
            }
        }

        public void getResourcesCount(out Dictionary<int, double> amounts, out Dictionary<int, double> maximums)
        {
            amounts = new Dictionary<int, double>();
            maximums = new Dictionary<int, double>();

            if (vessel.loaded)
            {
                foreach (Part p in this.vessel.parts)
                {
                    foreach (PartResource r in p.Resources)
                    {
                        if(!amounts.ContainsKey(r.info.id))
                        {
                            amounts.Add(r.info.id, 0);
                            maximums.Add(r.info.id, 0);
                        }

                        amounts[r.info.id] += r.amount;
                        maximums[r.info.id] += r.maxAmount;
                    }
                }
            }
            else
            {
                foreach (ProtoPartSnapshot snap in vessel.protoVessel.protoPartSnapshots)
                {
                    foreach (ProtoPartResourceSnapshot rsc_snap in snap.resources)
                    {
                        if (!amounts.ContainsKey(rsc_snap.definition.id))
                        {
                            amounts.Add(rsc_snap.definition.id, 0);
                            maximums.Add(rsc_snap.definition.id, 0);
                        }

                        amounts[rsc_snap.definition.id] += rsc_snap.amount;
                        maximums[rsc_snap.definition.id] += rsc_snap.maxAmount;
                    }
                }
            }
        }

        public HashSet<int> getResourceTypesOnVessel()
        {
            HashSet<int> rscTypes = new HashSet<int>();

            if (vessel.loaded)
            {
                foreach(Part p in this.vessel.parts)
                {
                    foreach(PartResource r in p.Resources)
                    {
                        if (!rscTypes.Contains(r.info.id))
                            rscTypes.Add(r.info.id);
                    }
                }
            }
            else
            {
                foreach (ProtoPartSnapshot snap in vessel.protoVessel.protoPartSnapshots)
                {
                    foreach (ProtoPartResourceSnapshot rsc_snap in snap.resources)
                    {
                        if (!rscTypes.Contains(rsc_snap.definition.id))
                            rscTypes.Add(rsc_snap.definition.id);
                    }
                }
            }

            return rscTypes;
        }

        public void getAllResourceCounts(out Dictionary<int, double> currentCounts, out Dictionary<int, double> maxCounts)
        {
            currentCounts = new Dictionary<int, double>();
            maxCounts = new Dictionary<int, double>();

            if (vessel.loaded)
            {
                foreach (Part p in this.vessel.parts)
                {
                    foreach (PartResource r in p.Resources)
                    {
                        if(!currentCounts.ContainsKey(r.info.id))
                        {
                            currentCounts.Add(r.info.id, 0);
                            maxCounts.Add(r.info.id, 0);
                        }

                        currentCounts[r.info.id] += r.amount;
                        maxCounts[r.info.id] += r.maxAmount;
                    }
                }
            }
            else
            {
                foreach (ProtoPartSnapshot snap in vessel.protoVessel.protoPartSnapshots)
                {
                    foreach (ProtoPartResourceSnapshot rsc_snap in snap.resources)
                    {
                        if (!currentCounts.ContainsKey(rsc_snap.definition.id))
                        {
                            currentCounts.Add(rsc_snap.definition.id, 0);
                            maxCounts.Add(rsc_snap.definition.id, 0);
                        }

                        currentCounts[rsc_snap.definition.id] += rsc_snap.amount;
                        maxCounts[rsc_snap.definition.id] += rsc_snap.maxAmount;
                    }
                }
            }
        }

        public Dictionary<int, bool> checkResources(Dictionary<int, double> resources, bool checkMax=false)
        {
            Dictionary<int, bool> ret = new Dictionary<int, bool>();

            foreach (int rsc in resources.Keys)
            {
                double current = 0;
                double max = 0;
                getResourceCount(rsc, out current, out max);

                if (checkMax)
                {
                    ret.Add(rsc, (max <= resources[rsc]));
                }
                else
                {
                    ret.Add(rsc, (current >= resources[rsc]));
                }
            }

            return ret;
        }

        /**
         * Sets a resource on a craft to 0 or max amount that can be stored.
         * True = set resource to max
         * False = set resource to empty
         */
        public void setResourceToExtreme(int resource, bool setToMax)
        {
            if (vessel.loaded)
            {
                foreach (Part p in vessel.parts)
                {
                    foreach (PartResource r in p.Resources)
                    {
                        if (r.info.id == resource)
                        {
                            if (setToMax)
                            {
                                r.amount = r.maxAmount;
                            }
                            else
                            {
                                r.amount = 0;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (ProtoPartSnapshot p in vessel.protoVessel.protoPartSnapshots)
                {
                    foreach (ProtoPartResourceSnapshot r in p.resources)
                    {
                        if (r.definition.id == resource)
                        {
                            if (setToMax)
                            {
                                r.amount = r.maxAmount;
                            }
                            else
                            {
                                r.amount = 0;
                            }
                        }
                    }
                }
            }
        }

        public void setResourcesToExtreme(Dictionary<int, bool> resources)
        {
            foreach(int rsc in resources.Keys)
            {
                setResourceToExtreme(rsc, resources[rsc]);
            }
        }

        /**
         * Removes or adds resources to a craft. 
         * Pass in a dictionary mapping resource IDs to the amounts to drain/add for each.
         * Negative values will add resources, positive values will drain.
         */
        public void modifyResource(int rsc, double amount)
        {
            if (vessel.loaded)
            {
                List<PartResource> resourceHolders = new List<PartResource>();

                foreach (Part p in vessel.parts)
                {
                    foreach (PartResource r in p.Resources)
                    {
                        if (r.info.id == rsc)
                            resourceHolders.Add(r);
                    }
                }

                if (resourceHolders.Count == 0)
                    return;

                double changePerPart = amount / resourceHolders.Count;
                double remaining = Math.Abs(amount);

                bool changed = false;

                while (remaining > 0)
                {
                    foreach (PartResource p in resourceHolders)
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
                                    changed = true;
                                }
                                else
                                {
                                    remaining -= p.amount;
                                    p.amount = 0;
                                    changed = true;
                                }
                            }
                            else
                            {
                                if ((p.maxAmount - p.amount) >= Math.Abs(changePerPart))
                                {
                                    p.amount += Math.Abs(changePerPart);
                                    remaining -= Math.Abs(changePerPart);
                                    changed = true;
                                }
                                else
                                {
                                    remaining -= (p.maxAmount - p.amount);
                                    p.amount = p.maxAmount;
                                    changed = true;
                                }
                            }
                        }
                        else
                        {
                            if(changePerPart > 0)
                            {
                                p.amount -= remaining;
                            } else
                            {
                                p.amount += remaining;
                            }

                            remaining = 0;
                            
                            changed = true;
                            break;
                        }
                    }

                    if (!changed)
                    {
                        break;
                    }
                    else
                    {
                        changed = false;
                    }
                }
            }
            else
            {
                /* There HAS to be a better way to do this. */
                List<ProtoPartResourceSnapshot> resourceHolders = new List<ProtoPartResourceSnapshot>();

                foreach (ProtoPartSnapshot p in vessel.protoVessel.protoPartSnapshots)
                {
                    foreach (ProtoPartResourceSnapshot r in p.resources)
                    {
                        if (r.definition.id == rsc)
                            resourceHolders.Add(r);
                    }
                }

                if (resourceHolders.Count == 0)
                    return;

                double changePerPart = amount / resourceHolders.Count;
                double remaining = Math.Abs(amount);

                bool changed = false;

                while (remaining > 0)
                {
                    foreach (ProtoPartResourceSnapshot p in resourceHolders)
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
                                    changed = true;
                                }
                                else
                                {
                                    remaining -= p.amount;
                                    p.amount = 0;
                                    changed = true;
                                }
                            }
                            else
                            {
                                if ((p.maxAmount - p.amount) >= Math.Abs(changePerPart))
                                {
                                    p.amount += Math.Abs(changePerPart);
                                    remaining -= Math.Abs(changePerPart);
                                    changed = true;
                                }
                                else
                                {
                                    remaining -= (p.maxAmount - p.amount);
                                    p.amount = p.maxAmount;
                                    changed = true;
                                }
                            }
                        }
                        else
                        {
                            if (changePerPart > 0)
                            {
                                p.amount -= remaining;
                            }
                            else
                            {
                                p.amount += remaining;
                            }

                            remaining = 0;

                            changed = true;
                            break;
                        }
                    }

                    if (!changed)
                    {
                        break;
                    }
                    else
                    {
                        changed = false;
                    }
                }
            }
        } // end of modifyResource

        public void modifyResources(Dictionary<int, double> resources)
        {
            foreach(int rsc in resources.Keys)
            {
                modifyResource(rsc, resources[rsc]);
            }
        }

        public void periodicUpdate()
        {
            this.currentLocation = null;

            foreach (SupplyPoint pt in SupplyChainController.instance.points)
            {
                if (pt.isVesselAtPoint(this.vessel))
                {
                    this.currentLocation = pt;
                    break;
                }
            }
        }

    }
}
