using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace SupplyChain
{
    public class SupplyLinkView
    {
        private Texture tex = null;
        private bool windowActive = false;
        private ApplicationLauncherButton button = null;
        private Rect windowPos = new Rect(0, 0, 600, 600);

        private GUIStyle passableStyle;
        private GUIStyle activeStyle;
        private GUIStyle impassableStyle;

        private GUIStyle passableLabelStyle;
        private GUIStyle activeLabelStyle;
        private GUIStyle impassableLabelStyle;
        
        private HashSet<SupplyLink> traversableLinks;
        
        public SupplyLinkView()
        {
            tex = GameDatabase.Instance.GetTexture("SupplyChain/Icons/SupplyLinkIcon", false);
            
            traversableLinks = new HashSet<SupplyLink>();

            GameEvents.OnFlightGlobalsReady.Add(updateSupplyLinks);

            addAppLauncherButton();

            /*
            GameEvents.onGUIApplicationLauncherReady.Add(addAppLauncherButton);

            GameEvents.onGUIApplicationLauncherUnreadifying.Add(destroyAppLauncherButton);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(destroyAppLauncherButton);
            GameEvents.onGameSceneLoadRequested.Add(destroyAppLauncherButton);
            */

            GameEvents.onTimeWarpRateChanged.Add(() => { updateSupplyLinks(); });
        }

        private void updateSupplyLinks(bool something = false)
        {
            traversableLinks.Clear();

            /* Save traversable links. */
            foreach (SupplyLink l in SupplyChainController.instance.links)
            {
                if (!l.active && l.canTraverseLink())
                    traversableLinks.Add(l);
            }
        }

        private void addAppLauncherButton()
        {
            if (button == null)
            {
                button = ApplicationLauncher.Instance.AddModApplication(
                    () => { updateSupplyLinks();  this.windowActive = true; },    // On toggle active.
                    () => { this.windowActive = false; },   // On toggle inactive.
                    null, null, null, null,
                    ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.TRACKSTATION, // Scenes to show in.
                    tex // Texture.
                );
                
            }
        }

        private void destroyAppLauncherButton()
        {
            this.destroyAppLauncherButton(GameScenes.SPACECENTER);
        }

        private void destroyAppLauncherButton(GameScenes scenes)
        {
            if(button != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(button);
                button = null;
            }
        }
        
        private Vector2 scrollPoint;
        private Vector2 selectScrollPt;

        private SupplyLink selectedLink = null;

        private bool linkMassStatus = false;
        private Dictionary<int, bool> linkResourceStatus;
        private bool linkPositionStatus = false;
        private bool linkOverallStatus = false;

        /*
         * small form = "DD:HH:MM:SS"
         * large form = "dd days, hh hours, mm minutes, ss seconds"
         */
        public static string formatTimespan(double ts, bool smallForm=false)
        {
            string ret = "";

            int t = (int)Math.Round(ts);

            int days = 0;
            
            if (GameSettings.KERBIN_TIME)
            {
                days = t / (int)Math.Round(FlightGlobals.Bodies[1].solarDayLength);
                t %= (int)Math.Round(FlightGlobals.Bodies[1].solarDayLength);
            } else
            {
                days = t / 86400;
                t %= 86400;
            }

            int hours = t / 3600;
            t %= 3600;

            int minutes = t / 60;
            t %= 60;

            if(smallForm)
            {
                if (days > 0)
                    ret += days.ToString("D2");

                if (hours > 0)
                    ret += ((ret.Length > 0) ? ":" : "") + hours.ToString("D2") + "";

                if (minutes > 0)
                    ret += ((ret.Length > 0) ? ":" : "") + minutes.ToString("D2") + "";

                if (t > 0)
                    ret += ((ret.Length > 0) ? ":" : "") + t.ToString("D2") + "";
            } else
            {
                if (days > 0)
                    ret += days.ToString() + " days";

                if (hours > 0)
                    ret += ((ret.Length > 0) ? ", " : "") + hours.ToString() + " hours";

                if (minutes > 0)
                    ret += ((ret.Length > 0) ? ", " : "") + minutes.ToString() + " minutes";

                if (t > 0)
                    ret += ((ret.Length > 0) ? ", " : "") + t.ToString() + " seconds";
            }

            return ret;
        }

        private void windowFunc(int id)
        {
            GUILayout.BeginHorizontal();

            scrollPoint = GUILayout.BeginScrollView(scrollPoint);
            foreach(VesselData vd in SupplyChainController.instance.trackedVessels)
            {
                GUILayout.Label(
                    (vd.vessel.loaded ? vd.vessel.name : vd.vessel.protoVessel.vesselName)
                    + " @ " + vd.vessel.GetOrbitDriver().referenceBody.name);

                Debug.Log("[SupplyChain] Showing tracked vessel: " + (vd.vessel.loaded ? vd.vessel.name : vd.vessel.protoVessel.vesselName));

                foreach (SupplyLink l in vd.links)
                {
                    GUIStyle st = impassableStyle;

                    if (traversableLinks.Contains(l))
                    {
                        st = passableStyle;
                    } else if(l.active)
                    {
                        st = activeStyle;
                    }

                    if (GUILayout.Button(l.from.name + " -> " + l.to.name, st))
                    {
                        selectedLink = (selectedLink == l) ? null : l;
                        if (selectedLink != null)
                        {
                            linkMassStatus = l.checkVesselMass();
                            linkResourceStatus = l.checkVesselResources();
                            linkPositionStatus = l.checkVesselPosition();
                            linkOverallStatus = true;
                        }
                    }
                }
            }
            GUILayout.EndScrollView();

            if(selectedLink != null)
            {
                selectScrollPt = GUILayout.BeginScrollView(selectScrollPt);
                
                /* Basic data. */
                GUILayout.BeginVertical();

                GUILayout.Label("Vessel: " + selectedLink.linkVessel.vessel.name);
                GUILayout.Label("From: " + selectedLink.from.name, linkPositionStatus ? passableLabelStyle : impassableLabelStyle);
                GUILayout.Label("To: " + selectedLink.to.name);

                GUILayout.EndVertical();

                /* Requirements. */
                GUILayout.BeginVertical();

                GUILayout.Label("Requirements:");
                GUILayout.Label("Maximum Mass: " + Convert.ToString(Math.Round(selectedLink.maxMass, 3)) + " tons", linkMassStatus ? passableLabelStyle : impassableLabelStyle);



                GUILayout.Label("Time Required: " + SupplyLinkView.formatTimespan(selectedLink.timeRequired));

                foreach(int rsc in linkResourceStatus.Keys)
                {
                    GUILayout.Label(PartResourceLibrary.Instance.GetDefinition(rsc).name + ": " + selectedLink.resourcesRequired[rsc],
                        linkResourceStatus[rsc] ? passableLabelStyle : impassableLabelStyle);
                }

                GUILayout.EndVertical();

                if(selectedLink.active)
                {
                    GUILayout.Label("Currently Traversing Link\nT-"+
                        SupplyLinkView.formatTimespan(selectedLink.timeComplete - Planetarium.GetUniversalTime(), true),
                        activeStyle
                    );
                } else if (linkOverallStatus)
                {
                    if (GUILayout.Button("Traverse Link"))
                    {
                        selectedLink.traverseLink();
                    }
                }


                GUILayout.EndScrollView();
            }

            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        public void OnGUI()
        {
            if(passableStyle == null)
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

            if (windowActive)
            {
                windowPos = GUI.Window(0, windowPos, windowFunc, "Supply Links");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    windowActive = false;
                    this.destroyAppLauncherButton();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
