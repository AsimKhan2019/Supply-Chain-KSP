using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain.UI
{
    public class ResourceTransferEditor : ActionEditorView
    {
        private Dictionary<int, ResourceTransferAction.ResourceTransfer> selectedXfers = null;
        private Dictionary<int, bool> xferDirections = null; // false = to target, true = to origin

        private Dictionary<int, double> orgCurResources;
        private Dictionary<int, double> orgMaxResources;

        private Dictionary<int, double> tgtCurResources;
        private Dictionary<int, double> tgtMaxResources;

        public new ResourceTransferAction action;

        private static string[] resourceTransferTypeStrings = { "Transfer At Most", "Transfer All", "Transfer At Least" };

        private Vector2 viewScrollPoint;

        private VesselData target = null;

        public ResourceTransferEditor(VesselData selected)
        {
            this.action = new ResourceTransferAction(selected);
            commonInit();
        }

        public ResourceTransferEditor(ResourceTransferAction action)
        {
            this.action = action;
            this.target = action.targetVessel;
            commonInit();
        }

        private void commonInit()
        {
            onUpdate();
        }



        public override void onUpdate()
        {
            action.linkVessel.getResourcesCount(out orgCurResources, out orgMaxResources);

            if(target != null)
            {
                target.getResourcesCount(out tgtCurResources, out tgtMaxResources);
            } else
            {
                tgtCurResources = null;
                tgtMaxResources = null;
            }
        }

        public override bool drawEditorWindow()
        {
            GUILayout.BeginVertical();

            /* Resource Transfer view */
            viewScrollPoint = GUILayout.BeginScrollView(viewScrollPoint);

            GUILayout.Label("Vessel: " + action.linkVessel.vessel.name);
            GUILayout.Label("At: " + action.linkVessel.currentLocation.name);

            if (SupplyChainController.instance.vesselsAtPoint.ContainsKey(action.linkVessel.currentLocation))
            {
                /* Vessel selector */
                foreach (Vessel v in SupplyChainController.instance.vesselsAtPoint[action.linkVessel.currentLocation])
                {
                    if(v.Equals(action.linkVessel.vessel))
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

                        onUpdate();

                        selectedXfers = new Dictionary<int, ResourceTransferAction.ResourceTransfer>();
                        xferDirections = new Dictionary<int, bool>();
                    }
                }

                if (target != null)
                {
                    /* Transfer details editor */
                    HashSet<int> targetRscTypes = target.getResourceTypesOnVessel();
                    HashSet<int> originRscTypes = action.linkVessel.getResourceTypesOnVessel();
                    
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

                            xferDirections.Add(rsc, false);
                        }
                        
                        GUILayout.Label(PartResourceLibrary.Instance.GetDefinition(rsc).name + ": ");
                        
                        selectedXfers[rsc].type = (ResourceTransferAction.TransferType)GUILayout.SelectionGrid((int)selectedXfers[rsc].type, ResourceTransferEditor.resourceTransferTypeStrings, 3);
                        if (GUILayout.Button(xferDirections[rsc] ? "To Origin" : "To Target"))
                        {
                            xferDirections[rsc] = !xferDirections[rsc];
                        }

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
                }
            }
            else
            {
                GUILayout.Label("No vessels available to transfer with.");
            }

            if (GUILayout.Button("Apply"))
            {
                List<ResourceTransferAction.ResourceTransfer> toOrigin = new List<ResourceTransferAction.ResourceTransfer>();
                List<ResourceTransferAction.ResourceTransfer> toTarget = new List<ResourceTransferAction.ResourceTransfer>();

                foreach (ResourceTransferAction.ResourceTransfer xfer in selectedXfers.Values)
                {
                    if(xferDirections[xfer.resourceID])
                    {
                        toOrigin.Add(xfer);
                    } else
                    {
                        toTarget.Add(xfer);
                    }
                }

                this.action.toTarget = toTarget;
                this.action.toOrigin = toOrigin;
                this.action.targetVessel = target;
            }

            if (GUILayout.Button("Back"))
            {
                return true;
            }


            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            return false;
        }

        public override bool drawSelectorButton()
        {
            string resourceNames = "";

            foreach(ResourceTransferAction.ResourceTransfer xfer in action.toOrigin)
            {
                if (resourceNames != "")
                    resourceNames += ", ";
                resourceNames += PartResourceLibrary.Instance.GetDefinition(xfer.resourceID).name;
            }

            foreach (ResourceTransferAction.ResourceTransfer xfer in action.toTarget)
            {
                if (resourceNames != "")
                    resourceNames += ", ";
                resourceNames += PartResourceLibrary.Instance.GetDefinition(xfer.resourceID).name;
            }

            return GUILayout.Button("Resource Transfer: " +
                action.linkVessel.vessel.name +
                " -> " +
                ((action.targetVessel != null && action.targetVessel.vessel != null) ? action.targetVessel.vessel.name : "[Unknown]") +
                "\n(" + resourceNames + ")"
            );
        }
    }
}
