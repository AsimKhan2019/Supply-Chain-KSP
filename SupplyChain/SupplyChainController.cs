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
        public List<SupplyPoint> points;
        public List<SupplyLink> links;
        
        public static SupplyLinkView slv;
        public static SupplyPointView spv;

        public static SupplyChainController instance;

        public override void OnAwake()
        {
            instance = this;

            points = new List<SupplyPoint>();
            links = new List<SupplyLink>();
            
            if(slv == null)
                slv = new SupplyLinkView();

            if(spv == null)
                spv = new SupplyPointView();
        }

        public void OnGUI()
        {
            slv.OnGUI();
            spv.OnGUI();
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
            SupplyChainController.instance.points.Add(point);
            Debug.Log("[SupplyChain] Registered new supply point: " + point.name);

            return true; // TODO: maybe check for redundant points?
        }

        public static bool deregisterNewSupplyPoint(SupplyPoint point)
        {
            Debug.Log("[SupplyChain] Unregistered supply point: " + point.name);

            if(SupplyChainController.instance.points.Contains(point))
            {
                SupplyChainController.instance.points.Remove(point);
                return true;
            }
            return false;
        }

        public static SupplyPoint getPointByGuid(Guid id)
        {
            return SupplyChainController.instance.points.Find((SupplyPoint p) => { return id.Equals(p.id); });
        }

        public static SupplyLink getLinkByGuid(Guid id)
        {
            return SupplyChainController.instance.links.Find((SupplyLink l) => { return id.Equals(l.id); });
        }

        public static bool registerNewSupplyLink(SupplyLink link)
        {
            SupplyChainController.instance.links.Add(link);
            Debug.Log("[SupplyChain] Added new supply link from " + link.from.name + " -> " + link.to.name);

            return true; // TODO: maybe check for redundant points?
        }

        public static bool deregisterNewSupplyLink(SupplyLink link)
        {
            Debug.Log("[SupplyChain] Removed supply link from " + link.from.name + " -> " + link.to.name);

            if (SupplyChainController.instance.links.Contains(link))
            {
                SupplyChainController.instance.links.Remove(link);
                return true;
            }
            return false;
        }
    }
}
