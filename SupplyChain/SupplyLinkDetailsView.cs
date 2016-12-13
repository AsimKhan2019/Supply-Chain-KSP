using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain
{
    public class SupplyLinkDetailsView
    {
        private Vector2 selectScrollPt;

        private bool linkMassStatus = false;
        private Dictionary<int, bool> linkResourceStatus;
        private bool linkPositionStatus = false;
        private bool linkOverallStatus = false;

        private GUIStyle passableStyle;
        private GUIStyle activeStyle;
        private GUIStyle impassableStyle;

        private GUIStyle passableLabelStyle;
        private GUIStyle activeLabelStyle;
        private GUIStyle impassableLabelStyle;

        private SupplyLink selectedLink = null;

        public SupplyLinkDetailsView(SupplyLink selected)
        {
            this.selectedLink = selected;
            linkMassStatus = selected.checkVesselMass();
            linkResourceStatus = selected.checkVesselResources();
            linkPositionStatus = selected.checkVesselPosition();
            linkOverallStatus = selected.canExecute();
        }

        public void onGUI()
        {
            setupStyling();

            /* Supply Link detail view */
            selectScrollPt = GUILayout.BeginScrollView(selectScrollPt);

            /* Basic data. */
            GUILayout.BeginVertical();

            GUILayout.Label("Vessel: " + selectedLink.linkVessel.vessel.name);
            GUILayout.Label("From: " + selectedLink.location.name, linkPositionStatus ? passableLabelStyle : impassableLabelStyle);
            GUILayout.Label("To: " + selectedLink.to.name);

            GUILayout.EndVertical();

            /* Requirements. */
            GUILayout.BeginVertical();

            GUILayout.Label("Requirements:");
            GUILayout.Label("Maximum Mass: " + Convert.ToString(Math.Round(selectedLink.maxMass, 3)) + " tons", linkMassStatus ? passableLabelStyle : impassableLabelStyle);

            GUILayout.Label("Time Required: " + SupplyActionView.formatTimespan(selectedLink.timeRequired));

            foreach (int rsc in linkResourceStatus.Keys)
            {
                GUILayout.Label(PartResourceLibrary.Instance.GetDefinition(rsc).name + ": " + selectedLink.resourcesRequired[rsc],
                    linkResourceStatus[rsc] ? passableLabelStyle : impassableLabelStyle);
            }

            GUILayout.EndVertical();

            if (selectedLink.active)
            {
                GUILayout.Label("Currently Traversing Link\nT-" +
                    SupplyActionView.formatTimespan(selectedLink.timeComplete - Planetarium.GetUniversalTime(), true),
                    activeStyle
                );
            }
            else if (linkOverallStatus)
            {
                if (GUILayout.Button("Traverse Link"))
                {
                    selectedLink.startAction();
                }
            }
                
            GUILayout.EndScrollView();
        }

        public void setupStyling()
        {
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
