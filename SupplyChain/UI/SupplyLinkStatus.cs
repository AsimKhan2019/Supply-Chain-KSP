using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain.UI
{
    public class SupplyLinkStatus : ActionStatusView
    {
        private Vector2 selectScrollPt;

        private bool linkMassStatus = false;
        private Dictionary<int, bool> linkResourceStatus;
        private bool linkPositionStatus = false;
        private bool linkOverallStatus = false;

        public new SupplyLink action;

        public SupplyLinkStatus(SupplyLink selected)
        {
            this.action = selected;
            onUpdate();
        }

        public override bool drawStatusWindow()
        {
            /* Supply Link detail view */
            selectScrollPt = GUILayout.BeginScrollView(selectScrollPt);


            /* Basic data. */
            GUILayout.BeginVertical();

            GUILayout.Label("Vessel: " + action.linkVessel.vessel.name);
            GUILayout.Label("From: " + action.location.name, linkPositionStatus ? UIStyle.passableLabelStyle : UIStyle.impassableLabelStyle);
            GUILayout.Label("To: " + action.to.name);
            if(action.active)
            {
                GUILayout.Label("Currently Active: T-" + UIStyle.formatTimespan(action.timeComplete - Planetarium.GetUniversalTime(), true), UIStyle.activeLabelStyle);
            }

            GUILayout.EndVertical();


            /* Requirements. */
            GUILayout.BeginVertical();

            GUILayout.Label("Requirements:");
            GUILayout.Label("Maximum Mass: " + Convert.ToString(Math.Round(action.maxMass, 3)) + " tons", linkMassStatus ? UIStyle.passableLabelStyle : UIStyle.impassableLabelStyle);

            GUILayout.Label("Time Required: " + UIStyle.formatTimespan(action.timeRequired));

            foreach (int rsc in linkResourceStatus.Keys)
            {
                GUILayout.Label(PartResourceLibrary.Instance.GetDefinition(rsc).name + ": " + action.resourcesRequired[rsc],
                    linkResourceStatus[rsc] ? UIStyle.passableLabelStyle : UIStyle.impassableLabelStyle);
            }

            GUILayout.EndVertical();
                
            GUILayout.EndScrollView();

            return GUILayout.Button("Back");
        }

        public override bool drawSelectorButton()
        {
            if(action.active)
            {
                return GUILayout.Button(
                    "Supply Link: " + action.location.name + " -> " + action.to.name +
                    "\nT-" + UIStyle.formatTimespan(action.timeComplete - Planetarium.GetUniversalTime(), true),
                    UIStyle.activeStyle
                );
            } else
            {
                return GUILayout.Button(
                    "Supply Link: " + action.location.name + "\n-> " + action.to.name,
                    (this.linkOverallStatus ? UIStyle.passableStyle : UIStyle.impassableLabelStyle)
                );
            }
        }

        public static void drawStatusLabel(SupplyLink l)
        {
            if (l.active)
            {
                GUILayout.Label(
                    l.location.name + " -> " + l.to.name +
                    " (Active: T-" + UIStyle.formatTimespan(l.timeComplete - Planetarium.GetUniversalTime(), true) + ")",
                    UIStyle.activeStyle
                );
            }
            else
            {
                GUILayout.Label(l.location.name + "-> " + l.to.name);
            }
        }

        public override void onUpdate()
        {
            linkMassStatus = this.action.checkVesselMass();
            linkResourceStatus = this.action.checkVesselResources();
            linkPositionStatus = this.action.checkVesselPosition();
            linkOverallStatus = this.action.canExecute();
        }
    }
}
