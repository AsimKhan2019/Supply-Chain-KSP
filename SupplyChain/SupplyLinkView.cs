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

        private GUIStyle passableStyle;     // for supply links that can be traversed
        private GUIStyle impassableStyle;   // for supply links that cannot be traversed

        private GUIStyle passableLabelStyle;
        private GUIStyle impassableLabelStyle;

        private Dictionary<Vessel, List<SupplyLink>> supplyLinksByVessel;
        private HashSet<SupplyLink> traversableLinks;
        
        public SupplyLinkView()
        {
            tex = new Texture();

            supplyLinksByVessel = new Dictionary<Vessel, List<SupplyLink>>();
            traversableLinks = new HashSet<SupplyLink>();

            GameEvents.OnFlightGlobalsReady.Add(updateSupplyLinks);

            addAppLauncherButton();

            /*
            GameEvents.onGUIApplicationLauncherReady.Add(addAppLauncherButton);

            GameEvents.onGUIApplicationLauncherUnreadifying.Add(destroyAppLauncherButton);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(destroyAppLauncherButton);
            GameEvents.onGameSceneLoadRequested.Add(destroyAppLauncherButton);
            */

            GameEvents.onTimeWarpRateChanged.Add(() => { this.windowActive = false; if (button != null) { button.SetFalse(); } });
        }

        private void updateSupplyLinks(bool something = false)
        {
            supplyLinksByVessel.Clear();
            traversableLinks.Clear();

            /* Sort supply links by vessel.*/
            foreach (SupplyLink l in SupplyChainController.instance.links)
            {
                if (!supplyLinksByVessel.ContainsKey(l.linkVessel))
                {
                    supplyLinksByVessel.Add(l.linkVessel, new List<SupplyLink>());
                }

                supplyLinksByVessel[l.linkVessel].Add(l);

                if (l.canTraverseLink())
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

        private void windowFunc(int id)
        {
            GUILayout.BeginHorizontal();

            scrollPoint = GUILayout.BeginScrollView(scrollPoint);
            foreach(Vessel v in supplyLinksByVessel.Keys)
            {
                GUILayout.Label(v.name + " @ " + v.orbit.referenceBody.name);
                foreach(SupplyLink l in supplyLinksByVessel[v])
                {
                    if(traversableLinks.Contains(l))
                    {
                        if (GUILayout.Button(l.from.name + " -> " + l.to.name, passableStyle))
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
                    } else
                    {
                        if(GUILayout.Button(l.from.name + " -> " + l.to.name, impassableStyle))
                        {
                            selectedLink = (selectedLink == l) ? null : l;
                            if(selectedLink != null)
                            {
                                linkMassStatus = l.checkVesselMass();
                                linkResourceStatus = l.checkVesselResources();
                                linkPositionStatus = l.checkVesselPosition();
                                linkOverallStatus = false;
                            }
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

                GUILayout.Label("Vessel: " + selectedLink.linkVessel.name);
                GUILayout.Label("From: " + selectedLink.from.name, linkPositionStatus ? passableLabelStyle : impassableLabelStyle);
                GUILayout.Label("To: " + selectedLink.to.name);

                GUILayout.EndVertical();

                /* Requirements. */
                GUILayout.BeginVertical();

                GUILayout.Label("Requirements:");
                GUILayout.Label("Maximum Mass: " + Convert.ToString(Math.Round(selectedLink.maxMass, 3)) + " tons", linkMassStatus ? passableLabelStyle : impassableLabelStyle);

                int t = (int)Math.Round(selectedLink.timeRequired);
                int days = t / (int)Math.Round(FlightGlobals.Bodies[1].solarDayLength);
                t %= (int)Math.Round(FlightGlobals.Bodies[1].solarDayLength);
                int hours = t / 3600;
                t %= 3600;
                int minutes = t / 60;
                t %= 60;

                GUILayout.Label("Time Required: " +
                    (days > 0 ? Convert.ToString(days) + " days " : "") +
                    (hours > 0 ? Convert.ToString(hours) + " hours " : "") +
                    (minutes > 0 ? Convert.ToString(minutes) + " minutes " : "") +
                    (t > 0 ? Convert.ToString(t) + " seconds" : "")
                );

                foreach(int rsc in linkResourceStatus.Keys)
                {
                    GUILayout.Label(PartResourceLibrary.Instance.GetDefinition(rsc).name + ": " + selectedLink.resourcesRequired[rsc],
                        linkResourceStatus[rsc] ? passableLabelStyle : impassableLabelStyle);
                }

                GUILayout.EndVertical();

                if(linkOverallStatus)
                {
                    if(GUILayout.Button("Traverse Link"))
                    {
                        selectedLink.traverseLink();
                        button.Disable();
                        windowActive = false;
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
