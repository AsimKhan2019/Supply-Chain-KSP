using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain.UI
{
    /* Supply Links can't really be edited, so this class mainly provides
     * a details view for the editor window.
     * 
     * This could also possibly be used to switch between different supply links on the same vessel,
     * but that would depend on implementing sequence validity checking.
     */
    public class SupplyLinkEditor : ActionEditorView
    {
        private Vector2 viewScrollPoint;

        public new SupplyLink action;

        public SupplyLinkEditor(SupplyLink selected)
        {
            action = selected;
        }

        public override bool drawEditorWindow()
        {
            viewScrollPoint = GUILayout.BeginScrollView(viewScrollPoint);

            GUILayout.Label("Vessel: " + action.linkVessel.vessel.name);
            GUILayout.Label("From: " + action.location.name);
            GUILayout.Label("To: " + action.to.name);

            GUILayout.EndVertical();

            /* Requirements. */
            GUILayout.BeginVertical();

            GUILayout.Label("Requirements:");
            GUILayout.Label("Maximum Mass: " + Convert.ToString(Math.Round(action.maxMass, 3)) + " tons");

            GUILayout.Label("Time Required: " + UIStyle.formatTimespan(action.timeRequired));

            foreach (int rsc in action.resourcesRequired.Keys)
            {
                GUILayout.Label(PartResourceLibrary.Instance.GetDefinition(rsc).name + ": " + action.resourcesRequired[rsc]);
            }

            if (GUILayout.Button("Back"))
            {
                return true;
            }

            GUILayout.EndScrollView();

            return false;
        }

        public override bool drawSelectorButton()
        {
            return GUILayout.Button(
                "Supply Link: From" + action.location.name + "\n->" + action.to.name
            );
        }

        public override void onUpdate() {}
    }
}
