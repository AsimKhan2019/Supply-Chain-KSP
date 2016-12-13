using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain
{
    public class StateTracker : PartModule
    {
        Dictionary<int, double> vesselResourceCapacity; // Max resources
        Dictionary<int, double> vesselResourceAmounts;  // Current resources
        HashSet<int> resourcesOnVessel;


        bool currentlyTrackingFlight = false;
        Dictionary<int, double> flightStartingResources;
        double flightStartingMET;
        double flightStartingMass;
        SupplyPoint flightStartPoint;

        /***
         * Enumerates all resources on the vessel and gets the  maximum resources onboard.
         */
        private void updateResourceCapacity()
        {
            Debug.Log("[SupplyChain] Entering UpdateResourceCapacity");
            if(resourcesOnVessel == null)
            {
                resourcesOnVessel = new HashSet<int>();
            } else
            {
                resourcesOnVessel.Clear();
            }

            

            foreach (Part p in vessel.Parts)
            {
                foreach(PartResource r in p.Resources)
                {
                    resourcesOnVessel.Add(r.info.id);
                }
            }

            vesselResourceCapacity = new Dictionary<int, double>();

            foreach(int r in resourcesOnVessel)
            {
                double current, max;

                vessel.GetConnectedResourceTotals(r, out current, out max, true);

                if(vesselResourceCapacity.ContainsKey(r))
                {
                    vesselResourceCapacity.Remove(r);
                }

                vesselResourceCapacity.Add(r, max);
            }
        }

        /***
         * Updates resource amounts using a previously enumerated resource list.
         */
        private void updateResourceAmounts()
        {
            Debug.Log("[SupplyChain] Entering UpdateResourceAmounts");
            vesselResourceAmounts = new Dictionary<int, double>();

            foreach (int r in resourcesOnVessel)
            {
                double current, max;

                vessel.GetConnectedResourceTotals(r, out current, out max, true);

                if (vesselResourceAmounts.ContainsKey(r))
                {
                    vesselResourceAmounts.Remove(r);
                }

                vesselResourceAmounts.Add(r, current);
            }
        }

        public override void OnStart(StartState state)
        {
            if(state != StartState.Editor)
            {
                Events["registerVessel"].active = !SupplyChainController.isVesselTracked(vessel);
            }
        }

        [KSPEvent(guiActive = true, active = true, guiName = "Register Vessel")]
        public void registerVessel()
        {
            if(!SupplyChainController.isVesselTracked(vessel))
            {
                VesselData nvd = new VesselData(vessel);
                SupplyChainController.registerNewTrackedVessel(nvd);
            }

            Events["registerVessel"].active = false;
        }

        [KSPEvent(guiActive = true, active = true, guiName = "Create New Supply Point")]
        public void createNewSupplyPoint()
        {
            foreach (SupplyPoint point in SupplyChainController.instance.points)
            {
                if (point.isVesselAtPoint(vessel))
                {
                    return;
                }
            }

            Debug.Log("[SupplyPoint] Creating new supply point.");
            // Create a new flight point here.
            if (vessel.situation == Vessel.Situations.ORBITING &&
            vessel.orbit.eccentricity > 0 && vessel.orbit.eccentricity < 1)
            {
                flightStartPoint = new OrbitalSupplyPoint(vessel);
                SupplyChainController.registerNewSupplyPoint(flightStartPoint);
            }
            else
            {
                // Can't create a new flight point; unstable situation.
                Debug.LogError("[SupplyPoint] Cannot create new supply point: in unstable orbit!");
                return;
            }
        }

        [KSPEvent(guiActive = true, active = true, guiName = "Begin Flight Tracking")]
        public void beginFlightTracking()
        {
            updateResourceCapacity();
            updateResourceAmounts();

            Debug.Log("[SupplyChain] Entering rest of BeginFlightTracking");

            flightStartPoint = null;
            foreach (SupplyPoint point in SupplyChainController.instance.points)
            {
                Debug.Log("[SupplyPoint] Inspecting point: " + point.name);
                if (point.isVesselAtPoint(vessel))
                {
                    Debug.Log("[SupplyChain] Found matching supply point: " + point.name);
                    flightStartPoint = point;
                    break;
                }
            }

            if (flightStartPoint == null)
            {
                Debug.Log("[SupplyPoint] Creating new supply point.");
                // Create a new flight point here.
                if (vessel.situation == Vessel.Situations.ORBITING &&
                vessel.orbit.eccentricity > 0 && vessel.orbit.eccentricity < 1)
                {
                    flightStartPoint = new OrbitalSupplyPoint(vessel);
                    SupplyChainController.registerNewSupplyPoint(flightStartPoint);
                } else {
                    // Can't create a new flight point; unstable situation.
                    Debug.LogError("[SupplyPoint] Cannot create new supply point: in unstable orbit!");
                    return;
                }
            }

            Debug.Log("[SupplyPoint] Setting up resources.");

            flightStartingResources = new Dictionary<int, double>();
            foreach (int r in vesselResourceAmounts.Keys)
            {
                Debug.Log("[SupplyChain] Tracking resource: " + PartResourceLibrary.Instance.GetDefinition(r).name + " (have" + Convert.ToString(vesselResourceAmounts[r]) + ")");
                flightStartingResources.Add(r, vesselResourceAmounts[r]);
            }
            flightStartingMET = vessel.missionTime;
            flightStartingMass = vessel.totalMass;
            Debug.Log("[SupplyPoint] Vessel mass: " + Convert.ToString(vessel.totalMass));
            currentlyTrackingFlight = true;

            Debug.Log("[SupplyPoint] Changing event states.");

            Events["endFlightTracking"].guiActive = true;
            Events["endFlightTracking"].active = true;
            Events["beginFlightTracking"].guiActive = false;
            Events["beginFlightTracking"].active = false;
        }

        [KSPEvent(guiActive = false, active = false, guiName = "End Flight Tracking")]
        public void endFlightTracking()
        {
            if(!currentlyTrackingFlight)
            {
                Debug.LogError("[SupplyChain] Attempted to end flight tracking without starting!");
                return;
            }

            updateResourceAmounts();

            // are we in a stable non-escape orbit?
            if(vessel.situation == Vessel.Situations.ORBITING &&
               vessel.orbit.eccentricity > 0 && vessel.orbit.eccentricity < 1)
            {
                SupplyPoint to = null;

                foreach(SupplyPoint point in SupplyChainController.instance.points)
                {
                    if(point.isVesselAtPoint(vessel))
                    {
                        Debug.Log("[SupplyChain] Found existing supply point.");
                        to = point;
                        break;
                    }
                }

                if(to == null)
                {
                    Debug.Log("[SupplyChain] Creating new supply point.");
                    to = new OrbitalSupplyPoint(vessel);
                    SupplyChainController.registerNewSupplyPoint(to);
                }

                if(!SupplyChainController.isVesselTracked(vessel))
                {
                    VesselData nv = new VesselData(vessel);
                    SupplyChainController.registerNewTrackedVessel(nv);
                }

                VesselData vd = SupplyChainController.getVesselTrackingInfo(vessel);

                SupplyLink result = new SupplyLink(vd, flightStartPoint, to);
                result.timeRequired = (vessel.missionTime - flightStartingMET);
                result.maxMass = flightStartingMass;

                Debug.Log("[SupplyChain] Creating new supply link.");
                Debug.Log("[SupplyChain] From: " + result.location.name);
                Debug.Log("[SupplyChain] To: " + result.to.name);
                Debug.Log("[SupplyChain] Total Elapsed MET: " + Convert.ToString(result.timeRequired));
                Debug.Log("[SupplyChain] Maximum mass: " + Convert.ToString(result.maxMass));

                foreach (int rsc in flightStartingResources.Keys)
                {
                    if(vesselResourceAmounts[rsc] < flightStartingResources[rsc])
                    {
                        result.resourcesRequired.Add(rsc, flightStartingResources[rsc] - vesselResourceAmounts[rsc]);
                        Debug.Log("[SupplyChain] Detected resource deficit: " +
                            Convert.ToString(flightStartingResources[rsc] - vesselResourceAmounts[rsc]) +
                            " of " +
                            PartResourceLibrary.Instance.GetDefinition(rsc).name);
                    }
                }

                SupplyChainController.registerNewSupplyLink(result);
            } else {
                Debug.Log("Canceled flight tracking: not in stable orbit.");
            }

            currentlyTrackingFlight = false;

            Events["endFlightTracking"].guiActive = false;
            Events["endFlightTracking"].active = false;
            Events["beginFlightTracking"].guiActive = true;
            Events["beginFlightTracking"].active = true;
        }
    }
}
