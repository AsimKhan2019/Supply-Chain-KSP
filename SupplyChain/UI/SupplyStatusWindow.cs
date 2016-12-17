using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

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

            this.toolbarIconURL = "SupplyChain/Icons/SupplyStatusIcon";
            this.windowName = "Supply Status";
            this.windowPos = new Rect(0, 0, 600, 600);
            this.activeScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.FLIGHT;
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
                    GUILayout.Label(selectedVesselData.vessel.name + ":", UIStyle.headingLabelStyle);

                    GUILayout.Space(UIStyle.separatorSpacing);

                    /* Basic info */
                    GUILayout.Label("Basic Information:", UIStyle.headingLabelStyle);
                    if (selectedVesselData.vessel.loaded)
                    {
                        GUILayout.Label("Mass: " + Math.Round(selectedVesselData.vessel.totalMass, 5).ToString() + " tons");
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

                    /* Vessel Resources */
                    Dictionary<int, double> currentResourceCounts;
                    Dictionary<int, double> maxResourceCounts;

                    GUILayout.Space(UIStyle.separatorSpacing);

                    GUILayout.Label("Vessel Resources:", UIStyle.headingLabelStyle);

                    selectedVesselData.getAllResourceCounts(out currentResourceCounts, out maxResourceCounts);
                    foreach(int rscID in maxResourceCounts.Keys)
                    {
                        GUILayout.Label(
                            PartResourceLibrary.Instance.GetDefinition(rscID).name + ": " +
                            currentResourceCounts[rscID].ToString() + " / " + maxResourceCounts[rscID].ToString()
                        );
                    }

                    GUILayout.Space(UIStyle.separatorSpacing);

                    /* Vessel Capabilities: */
                    GUILayout.Label("Vessel Capabilities:", UIStyle.headingLabelStyle);
                    if (selectedVesselData.orbitalDockingEnabled)
                    {
                        GUILayout.Label("Orbital Resource Transfer Enabled", UIStyle.passableLabelStyle);
                    } else
                    {
                        GUILayout.Label("Orbital Resource Transfer Disabled", UIStyle.impassableLabelStyle);
                    }

                    GUILayout.Space(UIStyle.separatorSpacing);

                    /* Vessel Supply Links */
                    GUILayout.Label("Vessel Supply Links:", UIStyle.headingLabelStyle);
                    foreach (SupplyLink l in selectedVesselData.links)
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
                            vd.vessel.name + " (MET T+" + UIStyle.formatTimespan(Math.Round(vd.vessel.missionTime), true).ToString() + ")"
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

            foreach (ActionStatusView v in toRemove)
            {
                activeActionViews.Remove(v);
            }
            

            /* Add newly active views. */
            foreach (SupplyChainAction activeAction in SupplyChainController.instance.activeActions)
            {
                if (!activeActionViews.Exists((ActionStatusView v) => { return v.action == activeAction; }))
                {
                    activeActionViews.Add(ActionStatusView.getActionDetailsView(activeAction));
                }
            }
            
            foreach (ActionStatusView view in activeActionViews)
            {
                view.onUpdate();
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
