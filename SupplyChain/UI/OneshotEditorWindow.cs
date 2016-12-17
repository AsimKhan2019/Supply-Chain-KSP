using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace SupplyChain.UI
{
    public class OneshotEditorWindow : SupplyBaseWindow
    {
        private static OneshotEditorWindow actualInstance = null;
        public new static OneshotEditorWindow instance
        {
            get
            {
                if (actualInstance == null)
                {
                    actualInstance = new OneshotEditorWindow();
                }

                return actualInstance;
            }

            set { }
        }

        private HashSet<SupplyLink> traversableLinks;
        
        public OneshotEditorWindow()
        {
            this.toolbarIconURL = "SupplyChain/Icons/SupplyLinkIcon";
            this.windowPos = new Rect(0, 0, 800, 800);
            this.windowName = "Supply Chain Actions";
            this.activeScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.TRACKSTATION;
            
            traversableLinks = new HashSet<SupplyLink>();
        }

        public override void onUpdate()
        {
            traversableLinks.Clear();

            /* Save traversable links. */
            foreach (SupplyLink l in SupplyChainController.instance.links)
            {
                if (!l.active && l.canExecute())
                    traversableLinks.Add(l);
            }

            if (selectedLink != null)
                selectedLink.onUpdate();

            if (selectedTransfer != null)
                selectedTransfer.onUpdate();
        }
        private Vector2 scrollPoint;

        private SupplyLinkEditor selectedLink = null;
        private ResourceTransferEditor selectedTransfer = null;

        private bool drawActionFireButton(SupplyChainAction act)
        {
            if(act.active)
            {
                GUILayout.Label(
                    "Action In Progress...\n" +
                    "T-"+UIStyle.formatTimespan(act.timeComplete - Planetarium.GetUniversalTime(), true),
                    UIStyle.activeStyle);
                return false;
            } else
            {
                if(act.canExecute())
                {
                    return GUILayout.Button("Execute Action", UIStyle.passableStyle);
                } else
                {
                    GUILayout.Label("Execute Action", UIStyle.impassableStyle);
                    return false;
                }
            }
        }

        public override void windowInternals(int id)
        {
            GUILayout.BeginHorizontal();

            /* Action select view */
            scrollPoint = GUILayout.BeginScrollView(scrollPoint);

            if(selectedLink != null)
            {
                if (selectedLink.drawEditorWindow())
                {
                    selectedLink = null;
                }

                if (selectedLink != null && drawActionFireButton(selectedLink.action))
                {
                    selectedLink = null;
                }
            } else if(selectedTransfer != null)
            {
                if (selectedTransfer.drawEditorWindow())
                {
                    selectedTransfer = null;
                }

                if(selectedTransfer != null && drawActionFireButton(selectedTransfer.action))
                {
                    selectedTransfer = null;
                }
            } else
            {
                foreach (VesselData vd in SupplyChainController.instance.trackedVessels)
                {
                    GUILayout.Label(
                        (vd.vessel.loaded ? vd.vessel.name : vd.vessel.protoVessel.vesselName)
                        + " @ " + vd.vessel.GetOrbitDriver().referenceBody.name);

                    foreach (SupplyLink l in vd.links)
                    {
                        GUIStyle st = UIStyle.impassableStyle;

                        if (traversableLinks.Contains(l))
                        {
                            st = UIStyle.passableStyle;
                        }
                        else if (l.active)
                        {
                            st = UIStyle.activeStyle;
                        }

                        if (GUILayout.Button(l.location.name + " -> " + l.to.name, st))
                        {
                            selectedLink = new SupplyLinkEditor(l);
                            selectedTransfer = null;
                        }
                    }

                    if (vd.orbitalDockingEnabled)
                    {
                        if (GUILayout.Button("Transfer Resources from Vessel", UIStyle.passableStyle))
                        {
                            selectedTransfer = new ResourceTransferEditor(vd);
                            selectedLink = null;
                        }
                    }
                    else
                    {
                        GUILayout.Label("Transfer Resources from Vessel", UIStyle.impassableStyle);
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
        }
    }
}
