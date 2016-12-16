using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain.UI
{
    public class SupplyStatusWindow : SupplyBaseWindow
    {
        private static SupplyStatusWindow actualInstance = null;
        public new static SupplyStatusWindow instance
        {
            get
            {
                if(actualInstance == null)
                {
                    actualInstance = new SupplyStatusWindow();
                }

                return actualInstance;
            }

            set {}
        }



        private string[] statusViews = { "Active Actions", "Vessel Tracking" };
        private int statusViewNumber = 0;

        private List<ActionStatusView> activeActionViews;
        private ActionStatusView selectedActView = null;

        public SupplyStatusWindow()
        {
            if (activeActionViews == null)
                activeActionViews = new List<ActionStatusView>();
        }

        public void showActiveActions()
        {
            if(selectedActView != null)
            {
                if(selectedActView.drawStatusWindow())
                {
                    selectedActView = null;
                }
            } else
            {
                GUILayout.Label("Currently Active Actions:");

                foreach (ActionStatusView v in activeActionViews)
                {
                    if (v.drawSelectorButton())
                    {
                        selectedActView = v;
                    }
                }
            }
        }

        private Vector2 trackingInfoScroll;
        private VesselData selectedVesselData = null;
        public void showVesselTrackingInfo()
        {
            trackingInfoScroll = GUILayout.BeginScrollView(trackingInfoScroll);
            GUILayout.BeginVertical();
            if (selectedVesselData != null)
            {
                if(selectedVesselData.vessel != null)
                {
                    /* Vessel Info: */
                    GUILayout.Label(selectedVesselData.vessel.name + ":");

                    /* Vessel Mass: */
                    if (selectedVesselData.vessel.loaded)
                    {
                        GUILayout.Label("Mass: " + selectedVesselData.vessel.totalMass.ToString());
                        GUILayout.Label("Crew: " + selectedVesselData.vessel.GetCrewCount().ToString());
                        if (selectedVesselData.currentLocation != null)
                        {
                            GUILayout.Label("Current Location: " + selectedVesselData.currentLocation.name);
                        }
                        else
                        {
                            GUILayout.Label("Current Location: " + selectedVesselData.vessel.GetOrbit().referenceBody.name + " (not at supply point)");
                        }
                    } else
                    {
                        GUILayout.Label(
                            "Mass: " + 
                            selectedVesselData.vessel.protoVessel.protoPartSnapshots.Sum( (ProtoPartSnapshot ps) => { return ps.mass; } ).ToString()
                        );

                        GUILayout.Label(
                            "Crew: " +
                            selectedVesselData.vessel.protoVessel.protoPartSnapshots.Sum((ProtoPartSnapshot ps) => { return ps.protoModuleCrew.Count; }).ToString()
                        );

                        if (selectedVesselData.currentLocation != null)
                        {
                            GUILayout.Label("Current Location: " + selectedVesselData.currentLocation.name);
                        }
                        else
                        {
                            GUILayout.Label(
                                "Current Location: " + 
                                FlightGlobals.Bodies[selectedVesselData.vessel.protoVessel.orbitSnapShot.ReferenceBodyIndex].name +
                                " (not at supply point)"
                            );
                        }
                    }

                    /* Vessel Resources: */
                    Dictionary<int, double> currentResourceCounts;
                    Dictionary<int, double> maxResourceCounts;

                    selectedVesselData.getAllResourceCounts(out currentResourceCounts, out maxResourceCounts);
                    foreach(int rscID in maxResourceCounts.Keys)
                    {
                        GUILayout.Label(
                            PartResourceLibrary.Instance.GetDefinition(rscID).name + ": " +
                            currentResourceCounts[rscID].ToString() + " / " + maxResourceCounts[rscID].ToString()
                        );
                    }

                    /* Vessel Capabilities: */
                    if(selectedVesselData.orbitalDockingEnabled)
                    {
                        GUILayout.Label("Orbital Resource Transfer Enabled", UIStyle.passableLabelStyle);
                    } else
                    {
                        GUILayout.Label("Orbital Resource Transfer Disabled", UIStyle.impassableLabelStyle);
                    }

                    /* Vessel Supply Links */
                    foreach(SupplyLink l in selectedVesselData.links)
                    {
                        SupplyLinkStatus.drawStatusLabel(l);
                    }
                }
                else
                {
                    GUILayout.Label("Unknown vessel selected.");
                }

                if (GUILayout.Button("Back"))
                    selectedVesselData = null;
            } else
            {
                foreach(VesselData vd in SupplyChainController.instance.trackedVessels)
                {
                    if(vd.vessel != null)
                    {
                        if (GUILayout.Button(
                            vd.vessel.name + " (MET" + UIStyle.formatTimespan(Math.Round(vd.vessel.missionTime), true).ToString() + ")"
                        ))
                        { 
                            selectedVesselData = vd;
                            trackingInfoScroll = new Vector2();
                        }
                    }
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        public override void onUpdate()
        {
            /* Figure out what's active and what's not active. */

            /* Remove views that are no longer active. */
            List<ActionStatusView> toRemove = new List<ActionStatusView>();
            foreach (ActionStatusView view in activeActionViews)
            {
                if (!SupplyChainController.instance.activeActions.Contains(view.action))
                {
                    toRemove.Add(view);
                }
            }

            foreach (ActionStatusView v in activeActionViews)
            {
                activeActionViews.Remove(v);
            }

            /* Add newly active views. */
            foreach (SupplyChainAction activeAction in SupplyChainController.instance.activeActions)
            {
                if (!activeActionViews.Exists((ActionStatusView v) => { return v.action.Equals(activeAction); }))
                {
                    activeActionViews.Add(ActionStatusView.getActionDetailsView(activeAction));
                }
            }
        }

        public override void windowInternals(int id)
        {
            GUILayout.BeginVertical();

            statusViewNumber = GUILayout.Toolbar(statusViewNumber, statusViews);

            this.windowName = this.statusViews[statusViewNumber];

            if(statusViewNumber == 0)
            {
                showActiveActions();
            } else if(statusViewNumber == 1)
            {
                showVesselTrackingInfo();
            }

            GUILayout.EndVertical();
        }
    }
}
