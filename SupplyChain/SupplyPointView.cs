using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace SupplyChain
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    class SupplyPointView : MonoBehaviour
    {
        private Texture tex = null;
        private bool windowActive = false;
        private ApplicationLauncherButton button = null;
        private Rect windowPos = new Rect(0, 0, 500, 300);

        public void OnAwake()
        {
            tex = new Texture();
            GameEvents.onGUIApplicationLauncherReady.Add(addAppLauncherButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(destroyAppLauncherButton);
        }

        private void addAppLauncherButton()
        {
            if (button == null)
            {
                button = ApplicationLauncher.Instance.AddModApplication(
                    () => { this.windowActive = true; },    // On toggle active.
                    () => { this.windowActive = false; },   // On toggle inactive.
                    null, null, null, null,
                    ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.TRACKSTATION, // Scenes to show in.
                    tex // Texture.
                );
            }
        }

        private void destroyAppLauncherButton(GameScenes scenes)
        {
            if(button != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(button);
                button = null;
            }
        }

        private SupplyPoint selectedPoint = null;
        private Vector2 scrollPoint;

        private void windowFunc(int id)
        {
            GUILayout.BeginHorizontal();

            /* List of supply points. */
            scrollPoint = GUILayout.BeginScrollView(scrollPoint);
            foreach(SupplyPoint p in SupplyChainController.points)
            {
                if(GUILayout.Button(p.name))
                {
                    selectedPoint = p;
                }
            }
            GUILayout.EndScrollView();

            if(selectedPoint != null)
            {
                GUILayout.BeginVertical();

                selectedPoint.guiDisplayData(id);

                foreach(Vessel v in SupplyChainController.vesselsAtPoint[selectedPoint])
                {
                    GUILayout.Label(v.name);
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
                GUI.Window(0, windowPos, windowFunc, "Supply Points");
            }
        }
    }
}
