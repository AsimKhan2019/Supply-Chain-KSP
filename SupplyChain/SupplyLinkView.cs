using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace SupplyChain
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    class SupplyLinkView : MonoBehaviour
    {
        private Texture tex = null;
        private bool windowActive = false;
        private ApplicationLauncherButton button = null;
        private Rect windowPos = new Rect(0, 0, 500, 300);

        private GUIStyle passableStyle;     // for supply links that can be traversed
        private GUIStyle impassableStyle;   // for supply links that cannot be traversed

        public void OnAwake()
        {
            tex = new Texture();

            passableStyle = new GUIStyle();
            passableStyle.normal.textColor = Color.green;
            impassableStyle = new GUIStyle();
            impassableStyle.normal.textColor = Color.red;

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
            Dictionary<Vessel, List<SupplyLink>> supplyLinksByVessel = new Dictionary<Vessel, List<SupplyLink>>();

            /* Sort supply links by vessel.*/
            foreach(SupplyLink l in SupplyChainController.links)
            {
                if(!supplyLinksByVessel.ContainsKey(l.linkVessel))
                {
                    supplyLinksByVessel.Add(l.linkVessel, new List<SupplyLink>());
                }

                supplyLinksByVessel[l.linkVessel].Add(l);
            }

            scrollPoint = GUILayout.BeginScrollView(scrollPoint);
            foreach(Vessel v in supplyLinksByVessel.Keys)
            {
                GUILayout.Label(v.name + " @ " + v.orbit.referenceBody.name);
                foreach(SupplyLink l in supplyLinksByVessel[v])
                {
                    if(l.canTraverseLink())
                    {
                        if (GUILayout.Button(l.from.name + " -> " + l.to.name, passableStyle))
                        {
                            l.traverseLink();
                        }
                    } else
                    {
                        GUILayout.Button(l.from.name + " -> " + l.to.name, impassableStyle)
                    }
                    
                }
            }
            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        public void OnGUI()
        {
            if (windowActive)
            {
                GUI.Window(0, windowPos, windowFunc, "Supply Links");
            }
        }
    }
}
