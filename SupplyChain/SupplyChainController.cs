using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class SupplyChainController : ScenarioModule
    {
        public static List<SupplyPoint> points;
        public static List<SupplyLink> links;
        public static ApplicationLauncherButton button;
        public static bool windowShown = false;
        Texture tex;

        public override void OnAwake()
        {
            if(points == null)
                points = new List<SupplyPoint>();

            if(links == null)
                links = new List<SupplyLink>();

            
            GameEvents.onGUIApplicationLauncherReady.Add(CreateLauncherButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(DestroyLauncherButton);
        }


        public void CreateLauncherButton()
        {
            if(button == null)
            {
                tex = new Texture();
                button = ApplicationLauncher.Instance.AddModApplication(showWindow, hideWindow, null, null, null, null, ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.TRACKSTATION, tex);
            }
        }

        public void DestroyLauncherButton()
        {
            if(button != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(button);
                button = null;
            }
        }

        public void DestroyLauncherButton(GameScenes something)
        {
            ApplicationLauncher.Instance.RemoveModApplication(button);
        }

        private void windowFunc(int id)
        {
            GUILayout.BeginVertical();

            foreach(SupplyLink link in links)
            {
                Debug.Log("[SupplyChain] Testing link: " + link.from.name + " -> " + link.to.name);
                if(link.canTraverseLink())
                {
                    Debug.Log("[SupplyChain] Vessel " + link.linkVessel.name + " can traverse link!");
                    if(GUILayout.Button(link.from.name + " -> " + link.to.name))
                    {
                        link.traverseLink();
                    }
                } else
                {
                    Debug.Log("[SupplyChain] Vessel " + link.linkVessel.name + " cannot traverse link.");
                }
            }

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        private Rect windowPos = new Rect(0, 0, 500, 300);

        public void OnGUI()
        {
            if(windowShown)
            {
                GUI.Window(0, windowPos, windowFunc, "Supply Chain");
            }
        }

        public void showWindow()
        {
            windowShown = true;
        }

        public void hideWindow()
        {
            windowShown = false;
        }

        public override void OnSave(ConfigNode node)
        {
            if (points == null)
                points = new List<SupplyPoint>();

            if (links == null)
                links = new List<SupplyLink>();

            Debug.Log("[SupplyChain] Saving SupplyPoints...");
            /* Save all SupplyPoints. */
            foreach (SupplyPoint point in points)
            {
                Debug.Log("[SupplyChain] Saved supply point: " + point.name);
                ConfigNode pointNode = node.AddNode("SupplyPoint");
                point.Save(pointNode);
            }

            Debug.Log("[SupplyChain] Saving SupplyLinks...");
            /* Save all SupplyLinks. */
            foreach (SupplyLink link in links)
            {
                Debug.Log("[SupplyChain] Saved supply link: " + link.from.name + " -> " + link.to.name);
                ConfigNode linkNode = node.AddNode("SupplyLink");
                link.Save(linkNode);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            if (points == null)
                points = new List<SupplyPoint>();

            if (links == null)
                links = new List<SupplyLink>();

            /* Load all SupplyPoints. */
            Debug.Log("[SupplyChain] Loading SupplyPoints...");
            ConfigNode[] pointNodes = node.GetNodes("SupplyPoint");
            foreach (ConfigNode pointNode in pointNodes)
            {
                if (!points.Exists( (SupplyPoint p) => { return p.id.Equals(new Guid(pointNode.GetValue("id"))); } ))
                {
                    String type = pointNode.GetValue("type");
                    switch (type)
                    {
                        case "orbit":
                            OrbitalSupplyPoint point = new OrbitalSupplyPoint();
                            point.Load(pointNode);
                            points.Add(point);
                            Debug.Log("[SupplyChain] Loaded SupplyPoint: " + point.name);
                            break;
                        default:
                            Debug.LogError("[SupplyChain] unrecognized supply point type: " + type);
                            break;
                    }
                }
            }

            /* Load all SupplyLinks. */
            Debug.Log("[SupplyChain] Loading SupplyLinks...");
            ConfigNode[] linkNodes = node.GetNodes("SupplyLink");
            foreach (ConfigNode linkNode in linkNodes)
            {
                if (!links.Exists((SupplyLink l) => { return l.id.Equals(new Guid(linkNode.GetValue("id"))); }))
                {
                    SupplyLink link = new SupplyLink();
                    link.Load(linkNode);
                    links.Add(link);
                    Debug.Log("[SupplyChain] Loaded Supply Link: " + link.from.name + " -> " + link.to.name);
                }
            }
        }

        public static bool registerNewSupplyPoint(SupplyPoint point)
        {
            points.Add(point);
            Debug.Log("[SupplyChain] Registered new supply point: " + point.friendlyName());

            return true; // TODO: maybe check for redundant points?
        }

        public static bool deregisterNewSupplyPoint(SupplyPoint point)
        {
            Debug.Log("[SupplyChain] Unregistered supply point: " + point.friendlyName());

            if(points.Contains(point))
            {
                points.Remove(point);
                return true;
            }
            return false;
        }

        public static SupplyPoint getPointByGuid(Guid id)
        {
            return points.Find((SupplyPoint p) => { return id.Equals(p.id); });
        }

        public static SupplyLink getLinkByGuid(Guid id)
        {
            return links.Find((SupplyLink l) => { return id.Equals(l.id); });
        }

        public static bool registerNewSupplyLink(SupplyLink link)
        {
            links.Add(link);
            Debug.Log("[SupplyChain] Added new supply link from " + link.from.name + " -> " + link.to.name);

            return true; // TODO: maybe check for redundant points?
        }

        public static bool deregisterNewSupplyLink(SupplyLink link)
        {
            Debug.Log("[SupplyChain] Removed supply link from " + link.from.name + " -> " + link.to.name);

            if (links.Contains(link))
            {
                links.Remove(link);
                return true;
            }
            return false;
        }
    }
}
