using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace SupplyChain.UI
{
    public class SupplyPointWindow : SupplyBaseWindow
    {
        public SupplyPointWindow()
        {
            this.toolbarIconURL = "SupplyChain/Icons/SupplyPointIcon";
            this.windowName = "Supply Points";
            this.windowPos = new Rect(0, 0, 600, 600);
            this.activeScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.FLIGHT;
        }

        private static SupplyPointWindow actualInstance = null;
        public new static SupplyPointWindow instance
        {
            get
            {
                if (actualInstance == null)
                {
                    actualInstance = new SupplyPointWindow();
                }

                return actualInstance;
            }

            set { }
        }


        private SupplyPoint selectedPoint = null;
        private Vector2 scrollPoint;

        public override void windowInternals(int id)
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
                if(SupplyChainController.instance.vesselsAtPoint.ContainsKey(selectedPoint))
                {
                    foreach (Vessel v in SupplyChainController.instance.vesselsAtPoint[selectedPoint])
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
        }

        public override void onUpdate() {}
    }
}
