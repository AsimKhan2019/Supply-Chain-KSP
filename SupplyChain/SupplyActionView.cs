using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace SupplyChain
{
    public class SupplyActionView
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
        
        public SupplyActionView()
        {
            tex = GameDatabase.Instance.GetTexture("SupplyChain/Icons/SupplyLinkIcon", false);
            
            traversableLinks = new HashSet<SupplyLink>();

            GameEvents.OnFlightGlobalsReady.Add(updateSupplyLinks);

            addAppLauncherButton();
            
            GameEvents.onTimeWarpRateChanged.Add(() => { updateSupplyLinks(); });
        }

        private void updateSupplyLinks(bool something = false)
        {
            traversableLinks.Clear();

            /* Save traversable links. */
            foreach (SupplyLink l in SupplyChainController.instance.links)
            {
                if (!l.active && l.canExecute())
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


        /*
         * small form = "DD:HH:MM:SS"
         * large form = "dd days, hh hours, mm minutes, ss seconds"
         */
        public static string formatTimespan(double ts, bool smallForm = false)
        {
            string ret = "";

            int t = (int)Math.Round(ts);

            int days = 0;

            if (GameSettings.KERBIN_TIME)
            {
                days = t / (int)Math.Round(FlightGlobals.Bodies[1].solarDayLength);
                t %= (int)Math.Round(FlightGlobals.Bodies[1].solarDayLength);
            }
            else
            {
                days = t / 86400;
                t %= 86400;
            }

            int hours = t / 3600;
            t %= 3600;

            int minutes = t / 60;
            t %= 60;

            if (smallForm)
            {
                if (days > 0)
                    ret += days.ToString("D2");

                if (hours > 0)
                    ret += ((ret.Length > 0) ? ":" : "") + hours.ToString("D2") + "";

                if (minutes > 0)
                    ret += ((ret.Length > 0) ? ":" : "") + minutes.ToString("D2") + "";

                if (t > 0)
                    ret += ((ret.Length > 0) ? ":" : "") + t.ToString("D2") + "";
            }
            else
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

        private Vector2 scrollPoint;

        private SupplyLinkDetailsView sldv = null;
        private SupplyTransferDetailsView stdv = null;

        private SupplyLink selectedLink = null;
        private VesselData selectedTransfer = null;
        

        private void windowFunc(int id)
        {
            GUILayout.BeginHorizontal();

            /* Action select view */
            scrollPoint = GUILayout.BeginScrollView(scrollPoint);
            foreach(VesselData vd in SupplyChainController.instance.trackedVessels)
            {
                GUILayout.Label(
                    (vd.vessel.loaded ? vd.vessel.name : vd.vessel.protoVessel.vesselName)
                    + " @ " + vd.vessel.GetOrbitDriver().referenceBody.name);

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

                    if (GUILayout.Button(l.location.name + " -> " + l.to.name, st))
                    {
                        if (selectedLink == l)
                        {
                            selectedLink = null;
                            sldv = null;
                        } else
                        {
                            sldv = new SupplyLinkDetailsView(l);
                            selectedLink = l;
                        }

                        stdv = null;
                        selectedTransfer = null;
                    }
                }

                if(vd.orbitalDockingEnabled)
                {
                    if (GUILayout.Button("Transfer Resources", passableStyle))
                    {
                        if(selectedTransfer == vd)
                        {
                            stdv = null;
                            selectedTransfer = null;
                        } else
                        {
                            stdv = new SupplyTransferDetailsView(vd);
                            selectedTransfer = vd;
                        }

                        sldv = null;
                        selectedLink = null;
                    }
                } else
                {
                    GUILayout.Label("Transfer Resources from Vessel", impassableStyle);
                }
            }
            GUILayout.EndScrollView();

            if (stdv != null)
            {
                stdv.OnGUI();
            }
            else if (sldv != null)
            {
                sldv.onGUI();
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
                windowPos = GUI.Window(0, windowPos, windowFunc, "Supply Chain Actions");
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
