using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace SupplyChain
{
    public class SupplyPointView : IDisposable
    {
        private Texture tex = null;
        private bool windowActive = false;
        private ApplicationLauncherButton button = null;
        private Rect windowPos = new Rect(0, 0, 600, 600);

        private Dictionary<SupplyPoint, List<Vessel>> vesselsAtPoint;

        public SupplyPointView()
        {
            tex = new Texture();

            vesselsAtPoint = new Dictionary<SupplyPoint, List<Vessel>>();

            GameEvents.OnFlightGlobalsReady.Add(updateVesselsAtPoint);

            /*
            GameEvents.onGUIApplicationLauncherReady.Add(addAppLauncherButton);

            // one of these should work
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(destroyAppLauncherButton);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(destroyAppLauncherButton);
            GameEvents.onGameSceneLoadRequested.Add(destroyAppLauncherButton);
            */

            addAppLauncherButton();

            GameEvents.onTimeWarpRateChanged.Add(() => { this.windowActive = false; });
        }

        private void addAppLauncherButton()
        {
            if (button == null)
            {
                button = ApplicationLauncher.Instance.AddModApplication(
                    () => { updateVesselsAtPoint(); this.windowActive = true; },    // On toggle active.
                    () => { this.windowActive = false; },   // On toggle inactive.
                    null, null, null, null,
                    ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW, // Scenes to show in.
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

        private void updateVesselsAtPoint(bool something = false)
        {
            vesselsAtPoint.Clear();

            foreach (Vessel v in FlightGlobals.Vessels)
            {
                foreach (SupplyPoint p in SupplyChainController.instance.points)
                {
                    if (p.isVesselAtPoint(v))
                    {
                        if (!vesselsAtPoint.ContainsKey(p))
                        {
                            vesselsAtPoint.Add(p, new List<Vessel>());
                        }
                        vesselsAtPoint[p].Add(v);
                        break;
                    }
                }
            }
        }

        private SupplyPoint selectedPoint = null;
        private Vector2 scrollPoint;

        private void windowFunc(int id)
        {
            GUILayout.BeginHorizontal();

            /* List of supply points. */
            scrollPoint = GUILayout.BeginScrollView(scrollPoint);
            foreach(SupplyPoint p in SupplyChainController.instance.points)
            {
                if(GUILayout.Button(p.name))
                {
                    selectedPoint = (p == selectedPoint) ? null : p;
                }
            }
            GUILayout.EndScrollView();

            if(selectedPoint != null)
            {
                GUILayout.BeginVertical();

                selectedPoint.guiDisplayData(id);

                GUILayout.Label("Vessels here:");
                if(vesselsAtPoint.ContainsKey(selectedPoint))
                {
                    foreach (Vessel v in vesselsAtPoint[selectedPoint])
                    {
                        GUILayout.Label(v.name);
                    }
                } else
                {
                    GUILayout.Label("No vessels at point.");
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();
        
            GUI.DragWindow();
        }

        public void OnGUI()
        {
            if (windowActive)
            {
                windowPos = GUI.Window(1, windowPos, windowFunc, "Supply Points");
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
