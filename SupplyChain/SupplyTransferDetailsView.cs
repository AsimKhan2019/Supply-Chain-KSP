using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain
{
    public class SupplyTransferDetailsView
    {
        public static GUIStyle passableStyle;
        public static GUIStyle impassableStyle;
        public static GUIStyle activeStyle;

        public static GUIStyle passableLabelStyle;
        public static GUIStyle impassableLabelStyle;
        public static GUIStyle activeLabelStyle;
        
        private VesselData origin = null;
        private VesselData target = null;
        private Dictionary<int, ResourceTransferAction.ResourceTransfer> selectedXfers = null;
        public Dictionary<int, double> orgCurResources;
        public Dictionary<int, double> orgMaxResources;

        public Dictionary<int, double> tgtCurResources;
        public Dictionary<int, double> tgtMaxResources;

        private static string[] resourceTransferTypeStrings = { "Transfer At Most", "Transfer All", "Transfer At Least" };

        private Vector2 viewScrollPoint;

        private ResourceTransferAction action = null;
        private bool activated = false;

        public SupplyTransferDetailsView(VesselData selected)
        {
            this.origin = selected;
            updateVesselRscLevels();

            GameEvents.onTimeWarpRateChanged.Add(updateVesselRscLevels);
        }

        public void updateVesselRscLevels()
        {
            origin.getResourcesCount(out orgCurResources, out orgMaxResources);

            if(target != null)
            {
                target.getResourcesCount(out tgtCurResources, out tgtMaxResources);
            } else
            {
                tgtCurResources = null;
                tgtMaxResources = null;
            }
        }

        public void OnGUI()
        {
            setupStyling();

            GUILayout.BeginVertical();

            /* Resource Transfer / Docking Event view */

            viewScrollPoint = GUILayout.BeginScrollView(viewScrollPoint);

            GUILayout.Label("Vessel: " + origin.vessel.name);
            GUILayout.Label("At: " + origin.currentLocation.name);

            if (SupplyChainController.instance.vesselsAtPoint.ContainsKey(origin.currentLocation))
            {
                /* Vessel select view. */
                foreach (Vessel v in SupplyChainController.instance.vesselsAtPoint[origin.currentLocation])
                {
                    if(v.Equals(origin.vessel))
                    {
                        continue;
                    }

                    if (GUILayout.Button(v.name))
                    {
                        if (SupplyChainController.isVesselTracked(v))
                        {
                            target = SupplyChainController.getVesselTrackingInfo(v);
                        }
                        else
                        {
                            target = new VesselData(v);
                        }

                        updateVesselRscLevels();

                        selectedXfers = new Dictionary<int, ResourceTransferAction.ResourceTransfer>();
                    }
                }

                if (target != null)
                {
                    /* Transfer details view */

                    HashSet<int> targetRscTypes = target.getResourceTypesOnVessel();
                    HashSet<int> originRscTypes = origin.getResourceTypesOnVessel();
                    
                    List<int> combinedRscTypes = new List<int>();
                    foreach(int r in targetRscTypes)
                    {
                        if (originRscTypes.Contains(r))
                            combinedRscTypes.Add(r);
                    }
                    

                    foreach (int rsc in combinedRscTypes)
                    {
                        GUILayout.BeginHorizontal();
                        if (!selectedXfers.ContainsKey(rsc))
                        {
                            selectedXfers.Add(rsc, new ResourceTransferAction.ResourceTransfer());
                            selectedXfers[rsc].resourceID = rsc;
                        }
                        
                        GUILayout.Label(PartResourceLibrary.Instance.GetDefinition(rsc).name + ": ");
                        
                        selectedXfers[rsc].type = (ResourceTransferAction.TransferType)GUILayout.SelectionGrid((int)selectedXfers[rsc].type, SupplyTransferDetailsView.resourceTransferTypeStrings, 3);
                        GUILayout.EndHorizontal();

                        try
                        {
                            selectedXfers[rsc].amount = Convert.ToDouble(GUILayout.TextField(selectedXfers[rsc].amount.ToString()));
                        } catch(FormatException e)
                        {
                            selectedXfers[rsc].amount = 0.0;
                        }

                        selectedXfers[rsc].amount = GUILayout.HorizontalSlider(
                            (float)selectedXfers[rsc].amount, 0, (orgCurResources[rsc] > tgtMaxResources[rsc]) ? (float)tgtMaxResources[rsc] : (float)orgCurResources[rsc]);

                        
                    }

                    if (action != null && action.active)
                    {
                        GUILayout.Label("Currently Transferring Resources\nT-" +
                            SupplyActionView.formatTimespan(action.timeComplete - Planetarium.GetUniversalTime(), true),
                            activeStyle
                        );
                    }
                    else
                    {
                        if (GUILayout.Button("Transfer Resources", passableStyle))
                        {
                            List<ResourceTransferAction.ResourceTransfer> transfers = new List<ResourceTransferAction.ResourceTransfer>();
                            foreach(ResourceTransferAction.ResourceTransfer xfer in selectedXfers.Values)
                            {
                                transfers.Add(xfer);
                            }

                            action = new ResourceTransferAction(this.origin, this.target, new List<ResourceTransferAction.ResourceTransfer>(), transfers);

                            action.startAction();
                        }
                    }
                }
            }
            else
            {
                GUILayout.Label("No vessels available to transfer with.");
            }


            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void setupStyling()
        {
            /* Setup styling. */
            if (passableStyle == null)
            {
                passableStyle = new GUIStyle("button");
                passableStyle.normal.textColor = Color.green;
            }

            if (activeStyle == null)
            {
                activeStyle = new GUIStyle("button");
                activeStyle.normal.textColor = Color.yellow;
            }

            if (impassableStyle == null)
            {
                impassableStyle = new GUIStyle("button");
                impassableStyle.normal.textColor = Color.red;
            }

            if (passableLabelStyle == null)
            {
                passableLabelStyle = new GUIStyle("label");
                passableLabelStyle.normal.textColor = Color.green;
            }

            if (activeLabelStyle == null)
            {
                activeLabelStyle = new GUIStyle("label");
                activeLabelStyle.normal.textColor = Color.yellow;
            }

            if (impassableLabelStyle == null)
            {
                impassableLabelStyle = new GUIStyle("label");
                impassableLabelStyle.normal.textColor = Color.red;
            }

        }
    }
}
