using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SupplyChain.UI;
using SupplyChain.Sequencing;

namespace SupplyChain
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class SupplyChainController : ScenarioModule
    {
        public List<SupplyPoint> points;
        public List<SupplyLink> links;
        public List<VesselData> trackedVessels;

        public List<SupplyChainAction> activeActions;
        public List<SupplyChainProcess> allProcesses;

        public static SupplyChainController instance;

        public static double updateInterval = 1; // check every second by default.
        private double lastUpdated;

        public Dictionary<SupplyPoint, List<Vessel>> vesselsAtPoint;

        public override void OnAwake()
        {
            instance = this;

            points = new List<SupplyPoint>();
            links = new List<SupplyLink>();
            trackedVessels = new List<VesselData>();
            activeActions = new List<SupplyChainAction>();
            vesselsAtPoint = new Dictionary<SupplyPoint, List<Vessel>>();

            lastUpdated = Planetarium.GetUniversalTime();

            GameEvents.onFlightReady.Add(doPeriodicUpdate);
            GameEvents.OnFlightGlobalsReady.Add( (bool data) => { doPeriodicUpdate(); } );
            GameEvents.onTimeWarpRateChanged.Add(doPeriodicUpdate);

            SupplyBaseWindow.initAllWindows();
        }

        public void OnGUI()
        {
            UI.UIStyle.init();

            SupplyPointWindow.instance.drawWindow();
            OneshotEditorWindow.instance.drawWindow();
            SupplyStatusWindow.instance.drawWindow();
        }

        public void FixedUpdate()
        {
            if((Planetarium.GetUniversalTime() - lastUpdated) > SupplyChainController.updateInterval)
            {
                List<SupplyChainAction> finished = new List<SupplyChainAction>(); // can't modify the active actions list while iterating through it
                foreach (SupplyChainAction act in activeActions)
                {
                    if (Planetarium.GetUniversalTime() >= act.timeComplete && act.canFinish())
                    {
                        act.finishAction();
                        finished.Add(act);

                        if(act.onComplete != null)
                        {
                            foreach (Action<SupplyChainAction> callback in act.onComplete)
                            {
                                callback(act);
                            }
                        }
                    }
                }

                foreach(SupplyChainAction act in finished)
                {
                    activeActions.Remove(act);
                }

                doPeriodicUpdate();
                SupplyBaseWindow.updateAllWindows();

                foreach(SupplyChainProcess proc in allProcesses)
                {
                    proc.schedulerUpdate();
                }

                lastUpdated = Planetarium.GetUniversalTime();
            }
        }

        private void doPeriodicUpdate()
        {
            /* Update the vesselsAtPoint dictionary. */
            vesselsAtPoint.Clear();

            foreach (Vessel v in FlightGlobals.Vessels)
            {
                foreach (SupplyPoint p in SupplyChainController.instance.points)
                {
                    if (p.isVesselAtPoint(v))
                    {
                        if (!vesselsAtPoint.ContainsKey(p))
                        {
                            vesselsAtPoint.Add(p, new List<Vessel>());
                        }
                        vesselsAtPoint[p].Add(v);
                        break;
                    }
                }
            }

            /* Update all tracked vessels. */
            foreach(VesselData vd in this.trackedVessels)
            {
                vd.periodicUpdate();
            }
        }

        public override void OnSave(ConfigNode node)
        {
            if (points == null)
                points = new List<SupplyPoint>();

            if (links == null)
                links = new List<SupplyLink>();

            if (trackedVessels == null)
                trackedVessels = new List<VesselData>();

            Debug.Log("[SupplyChain] Saving SupplyPoints...");
            /* Save all SupplyPoints. */
            foreach (SupplyPoint point in points)
            {
                Debug.Log("[SupplyChain] Saved supply point: " + point.name);
                ConfigNode pointNode = node.AddNode("SupplyPoint");
                point.Save(pointNode);
            }

            Debug.Log("[SupplyChain] Saving SupplyLinks...");
            /* Save all SupplyLinks. */
            foreach (SupplyLink link in links)
            {
                Debug.Log("[SupplyChain] Saved supply link: " + link.location.name + " -> " + link.to.name);
                ConfigNode linkNode = node.AddNode("SupplyLink");
                link.Save(linkNode);
            }

            Debug.Log("[SupplyChain] Saving Vessel Data...");
            foreach(VesselData v in trackedVessels)
            {
                ConfigNode vessNode = node.AddNode("TrackedVessel");
                v.Save(vessNode);
            }

            if(activeActions.Exists( (SupplyChainAction act) => { return act.freestanding; } ))
            {
                Debug.Log("[SupplyChain] Saving active actions...");
                foreach(SupplyChainAction act in activeActions)
                {
                    if(act.freestanding)
                    {
                        ConfigNode actNode = node.AddNode("ActiveAction");
                        act.Save(actNode);
                    }
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            if (points == null)
                points = new List<SupplyPoint>();

            if (links == null)
                links = new List<SupplyLink>();

            if (trackedVessels == null)
                trackedVessels = new List<VesselData>();

            /* Load all SupplyPoints. */
            Debug.Log("[SupplyChain] Loading SupplyPoints...");
            ConfigNode[] pointNodes = node.GetNodes("SupplyPoint");
            foreach (ConfigNode pointNode in pointNodes)
            {
                if (!points.Exists( (SupplyPoint p) => { return p.id.Equals(new Guid(pointNode.GetValue("id"))); } ))
                {
                    String type = pointNode.GetValue("type");
                    switch (type)
                    {
                        case "orbit":
                            OrbitalSupplyPoint point = new OrbitalSupplyPoint();
                            point.Load(pointNode);
                            points.Add(point);
                            Debug.Log("[SupplyChain] Loaded SupplyPoint: " + point.name);
                            break;
                        default:
                            Debug.LogError("[SupplyChain] unrecognized supply point type: " + type);
                            break;
                    }
                }
            }

            Debug.Log("[SupplyChain] Loading tracked vessels...");
            ConfigNode[] vessNodes = node.GetNodes("TrackedVessel");
            foreach (ConfigNode vessNode in vessNodes)
            {
                VesselData v = new VesselData();
                v.Load(vessNode);
                trackedVessels.Add(v);
            }

            /* Load all SupplyLinks. */
            Debug.Log("[SupplyChain] Loading SupplyLinks...");
            ConfigNode[] linkNodes = node.GetNodes("SupplyLink");
            foreach (ConfigNode linkNode in linkNodes)
            {
                if (!links.Exists((SupplyLink l) => { return l.id.Equals(new Guid(linkNode.GetValue("id"))); }))
                {
                    SupplyLink link = new SupplyLink();
                    link.Load(linkNode);
                    links.Add(link);

                    if(link.active)
                    {
                        activeActions.Add(link);
                    }
                    Debug.Log("[SupplyChain] Loaded Supply Link: " + link.location.name + " -> " + link.to.name);
                }
            }

            ConfigNode[] actNodes = node.GetNodes("ActiveAction");
            if(actNodes.Count() > 0)
            {
                Debug.Log("[SupplyChain] Loading active actions...");
                foreach (ConfigNode actNode in actNodes)
                {
                    string type = actNode.GetValue("type");
                    if (type == "ResourceTransfer")
                    {
                        ResourceTransferAction act = new ResourceTransferAction();
                        act.Load(actNode);
                        this.activeActions.Add(act);
                    }
                }
            }
        }

        public static bool registerNewSupplyPoint(SupplyPoint point)
        {
            SupplyChainController.instance.points.Add(point);
            Debug.Log("[SupplyChain] Registered new supply point: " + point.name);

            return true; // TODO: maybe check for redundant points?
        }

        public static bool deregisterNewSupplyPoint(SupplyPoint point)
        {
            Debug.Log("[SupplyChain] Unregistered supply point: " + point.name);

            if(SupplyChainController.instance.points.Contains(point))
            {
                SupplyChainController.instance.points.Remove(point);
                return true;
            }
            return false;
        }

        public static SupplyPoint getPointByGuid(Guid id)
        {
            return SupplyChainController.instance.points.Find((SupplyPoint p) => { return id.Equals(p.id); });
        }

        public static SupplyLink getLinkByGuid(Guid id)
        {
            return SupplyChainController.instance.links.Find((SupplyLink l) => { return id.Equals(l.id); });
        }

        public static bool registerNewSupplyLink(SupplyLink link)
        {
            SupplyChainController.instance.links.Add(link);

            Debug.Log("[SupplyChain] Added new supply link from " + link.location.name + " -> " + link.to.name);

            return true; // TODO: maybe check for redundant points?
        }

        public static bool deregisterNewSupplyLink(SupplyLink link)
        {
            Debug.Log("[SupplyChain] Removed supply link from " + link.location.name + " -> " + link.to.name);

            if (SupplyChainController.instance.links.Contains(link))
            {
                return true;
            }
            return false;
        }

        public static bool registerNewTrackedVessel(VesselData v)
        {
            if(!SupplyChainController.instance.trackedVessels.Contains(v))
            {
                SupplyChainController.instance.trackedVessels.Add(v);
                Debug.Log("[SupplyChain] Registered new tracked vessel:");
                Debug.Log("[SupplyChain]     Name: " + v.vessel.name);
                Debug.Log("[SupplyChain]     VesselID = " + v.vesselID.ToString());
                Debug.Log("[SupplyChain]     Root PartID = " + Convert.ToString(v.rootPartID));
                Debug.Log("[SupplyChain]     Tracking ID = " + v.trackingID.ToString());
                return true;
            }
            return false;
        }

        public static bool deregisterNewTrackedVessel(VesselData v)
        {
            if (SupplyChainController.instance.trackedVessels.Contains(v))
            {
                SupplyChainController.instance.trackedVessels.Remove(v);
                return true;
            }
            return false;
        }

        public static VesselData getVesselTrackingInfo(Vessel v)
        {
            return SupplyChainController.instance.trackedVessels.Find((VesselData vd) => { return (vd.vesselID.Equals(v.id)); });
        }

        public static VesselData getVesselTrackingInfo(Guid trackingID)
        {
            return SupplyChainController.instance.trackedVessels.Find((VesselData vd) => { return (vd.trackingID.Equals(trackingID)); });
        }

        public static bool isVesselTracked(Vessel v)
        {
            return SupplyChainController.instance.trackedVessels.Exists((VesselData vd) => { return (vd.vesselID.Equals(v.id)); });
        }
    }
}
