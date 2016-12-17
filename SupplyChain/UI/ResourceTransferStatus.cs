using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain.UI
{
    public class ResourceTransferStatus : ActionStatusView
    {
        private Vector2 scrollPt;

        public new ResourceTransferAction action;

        public ResourceTransferStatus(ResourceTransferAction act)
        {
            action = act;
        }

        public override bool drawSelectorButton()
        {
            string resourceNames = "";

            foreach (ResourceTransferAction.ResourceTransfer xfer in action.toOrigin)
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

            // indicates resource transfer direction
            string arrow;

            if(action.toOrigin.Count == 0 && action.toTarget.Count > 0)
            {
                arrow = " -> ";
            } else if(action.toOrigin.Count > 0 && action.toTarget.Count == 0)
            {
                arrow = " <- ";
            } else
            {
                arrow = " <-> ";
            }

            if (action.active)
            {
                return GUILayout.Button("Resource Transfer: " +
                    action.linkVessel.vessel.name +
                    arrow +
                    ((action.targetVessel != null && action.targetVessel.vessel != null) ? action.targetVessel.vessel.name : "[Unknown]") +
                    "\n(" + resourceNames + ")" +
                    "\nT-" + UIStyle.formatTimespan(action.timeComplete - Planetarium.GetUniversalTime(), true) + " to completion"
                );
            } else
            {
                return GUILayout.Button("Resource Transfer: " +
                    action.linkVessel.vessel.name +
                    arrow +
                    ((action.targetVessel != null && action.targetVessel.vessel != null) ? action.targetVessel.vessel.name : "[Unknown]") +
                    "\n(" + resourceNames + ")"
                );
            }
        }

        private void displayResourceTransferType(ResourceTransferAction.ResourceTransfer xfer)
        {
            if (xfer.type == ResourceTransferAction.TransferType.TRANSFER_AMOUNT)
            {
                GUILayout.Label(
                    PartResourceLibrary.Instance.GetDefinition(xfer.resourceID).name +
                    ": Transferring at most " + xfer.amount.ToString() + " units."
                );
            }
            else if (xfer.type == ResourceTransferAction.TransferType.TRANSFER_ALL)
            {
                GUILayout.Label(
                    PartResourceLibrary.Instance.GetDefinition(xfer.resourceID).name +
                    ": Transferring any available units of resource."
                );
            }
            else if (xfer.type == ResourceTransferAction.TransferType.TRANSFER_WAIT)
            {
                GUILayout.Label(
                    PartResourceLibrary.Instance.GetDefinition(xfer.resourceID).name +
                    ": Transferring at least " + xfer.amount.ToString() + " units."
                );
            }
        }

        public override bool drawStatusWindow()
        {
            /* Basic data. */
            GUILayout.BeginVertical();
            GUILayout.Label("Origin Vessel: " + action.linkVessel.vessel.name);
            GUILayout.Label(
                "Target Vessel:" +
                ((action.targetVessel != null && action.targetVessel.vessel != null) ? action.targetVessel.vessel.name : "[Unknown]"));

            if (action.active)
            {
                GUILayout.Label("Currently Active: T-" + UIStyle.formatTimespan(action.timeComplete - Planetarium.GetUniversalTime(), true), UIStyle.activeLabelStyle);
            }

            GUILayout.EndVertical();

            GUILayout.Space(UIStyle.separatorSpacing);

            /* Resource transfers: */
            GUILayout.BeginVertical();

            if(action.targetVessel != null && action.targetVessel.vessel != null)
            {
                if (action.toTarget.Count == 0)
                {
                    GUILayout.Label("No resources transferred to target vessel.");
                } else
                {
                    GUILayout.Label("Resources Transferred to Target Vessel: ", UIStyle.headingLabelStyle);

                    foreach (ResourceTransferAction.ResourceTransfer xfer in action.toTarget)
                    {
                        displayResourceTransferType(xfer);
                    }
                }
            }

            GUILayout.EndVertical();

            GUILayout.Space(UIStyle.separatorSpacing);

            GUILayout.BeginVertical();

            if (action.toOrigin.Count == 0)
            {
                GUILayout.Label("No resources transferred to origin vessel.");
            } else
            {
                GUILayout.Label("Resources Transferred to Origin Vessel: ", UIStyle.headingLabelStyle);
                foreach (ResourceTransferAction.ResourceTransfer xfer in action.toOrigin)
                {
                    displayResourceTransferType(xfer);
                }
            }

            GUILayout.EndVertical();

            return GUILayout.Button("Back");
        }

        public override void onUpdate() {}
    }
}
