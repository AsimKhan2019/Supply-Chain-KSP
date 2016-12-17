using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain
{
    public class ResourceTransferAction : SupplyChainAction
    {
        public VesselData targetVessel;

        public enum TransferType
        {
            TRANSFER_AMOUNT = 0,    ///< Transfer at most X units of resource
            TRANSFER_ALL = 1,       ///< Transfer any units of resource found on source
            TRANSFER_WAIT = 2,      ///< Transfer at least X units of resource and wait if necessary
        };

        public class ResourceTransfer
        {
            public int resourceID;
            public double amount;
            public TransferType type;
        };
        
        public List<ResourceTransfer> toTarget;
        public List<ResourceTransfer> toOrigin;

        private Dictionary<int, double> targetRequirements;
        private Dictionary<int, double> originRequirements;

        public ResourceTransferAction(VesselData origin, VesselData target,
            List<ResourceTransfer> toOrigin, List<ResourceTransfer> toTarget)
        {
            this.linkVessel = origin;
            this.location = origin.currentLocation;
            this.targetVessel = target;

            this.timeRequired = 3600; // 1 hour

            this.toTarget = toTarget;
            this.toOrigin = toOrigin;

            calculateRequirements();
        }

        public ResourceTransferAction(VesselData origin)
        {
            this.linkVessel = origin;
            this.location = origin.currentLocation;
            this.timeRequired = 3600;

            this.toTarget = new List<ResourceTransfer>();
            this.toOrigin = new List<ResourceTransfer>();
        }

        public ResourceTransferAction()
        {
            this.timeRequired = 3600;
            this.toTarget = new List<ResourceTransfer>();
            this.toOrigin = new List<ResourceTransfer>();
        }

        private void calculateRequirements()
        {
            targetRequirements = new Dictionary<int, double>();
            originRequirements = new Dictionary<int, double>();

            foreach (ResourceTransfer t in toTarget)
            {
                if (t.type == TransferType.TRANSFER_WAIT)
                {
                    targetRequirements.Add(t.resourceID, t.amount);
                }
            }

            foreach (ResourceTransfer t in toOrigin)
            {
                if (t.type == TransferType.TRANSFER_WAIT)
                {
                    originRequirements.Add(t.resourceID, t.amount);
                }
            }
        }
        public override bool canExecute()
        {
            if(targetVessel == null || targetVessel.vessel == null || this.location == null)
            {
                return false;
            }

            /* Find target vessel location. */
            SupplyPoint targetLocation = targetVessel.currentLocation;

            if(targetLocation != null)
            {
                return targetLocation.isVesselAtPoint(this.linkVessel.vessel);
            } else
            {
                Debug.LogError("[SupplyChain] Vessel " + targetVessel.trackingID.ToString() + " not in valid location!");
            }

            return false;
        }

        public override bool canFinish()
        {
            calculateRequirements();

            Dictionary<int, bool> targetStatus = targetVessel.checkResources(targetRequirements);
            Dictionary<int, bool> originStatus = linkVessel.checkResources(originRequirements);
            
            foreach(bool v in targetStatus.Values)
            {
                if (!v)
                {
                    Debug.Log("[SupplyChain] TransferAction: waiting on target resources");
                    return false;
                }
            }

            foreach (bool v in originStatus.Values)
            {
                if (!v)
                {
                    Debug.Log("[SupplyChain] TransferAction: waiting on origin resources");
                    return false;
                }
            }

            return true;
        }

        public override bool startAction()
        {
            if (!this.canExecute()) {
                Debug.Log("[SupplyChain] TransferAction: cannot execute!");
                return false;
            }

            this.active = true;
            this.timeComplete = (Planetarium.GetUniversalTime() + this.timeRequired);

            if (!SupplyChainController.instance.activeActions.Contains(this))
                SupplyChainController.instance.activeActions.Add(this);

            return true;
        }

        public override void finishAction()
        {
            this.active = false;

            Debug.Log("[SupplyChain] TransferAction: performing resource transfers.");

            foreach (ResourceTransfer xfer in toTarget)
            {
                if(xfer.type == TransferType.TRANSFER_AMOUNT)
                {
                    targetVessel.modifyResource(xfer.resourceID, -1*xfer.amount);
                    linkVessel.modifyResource(xfer.resourceID, xfer.amount);
                }
                else if (xfer.type == TransferType.TRANSFER_ALL || xfer.type == TransferType.TRANSFER_AMOUNT)
                {
                    double tgtCur = 0, tgtMax = 0;
                    targetVessel.getResourceCount(xfer.resourceID, out tgtCur, out tgtMax);

                    double orgCur = 0, orgMax = 0;
                    linkVessel.getResourceCount(xfer.resourceID, out orgCur, out orgMax);

                    if (orgCur > tgtMax)
                    {
                        targetVessel.setResourceToExtreme(xfer.resourceID, true);
                        linkVessel.modifyResource(xfer.resourceID, tgtMax - tgtCur);
                    }
                    else
                    {
                        targetVessel.modifyResource(xfer.resourceID, -1 * tgtCur);
                        linkVessel.setResourceToExtreme(xfer.resourceID, false);
                    }
                }
            }

            foreach (ResourceTransfer xfer in toOrigin)
            {
                if (xfer.type == TransferType.TRANSFER_AMOUNT)
                {
                    linkVessel.modifyResource(xfer.resourceID, -1 * xfer.amount);
                    targetVessel.modifyResource(xfer.resourceID, xfer.amount);
                }
                else if (xfer.type == TransferType.TRANSFER_ALL || xfer.type == TransferType.TRANSFER_AMOUNT)
                {
                    double tgtCur = 0, tgtMax = 0;
                    targetVessel.getResourceCount(xfer.resourceID, out tgtCur, out tgtMax);

                    double orgCur = 0, orgMax = 0;
                    linkVessel.getResourceCount(xfer.resourceID, out orgCur, out orgMax);

                    if(tgtCur > orgMax)
                    {
                        linkVessel.setResourceToExtreme(xfer.resourceID, true);
                        targetVessel.modifyResource(xfer.resourceID, orgMax - orgCur);
                    } else /* orgMax > tgtCur */
                    {
                        linkVessel.modifyResource(xfer.resourceID, -1 * tgtCur); // add all of current resource on target to origin
                        targetVessel.setResourceToExtreme(xfer.resourceID, false);
                    }   
                }
            }
        }

        public override void LoadCustom(ConfigNode node)
        {
            bool targetTracked = false;
            node.TryGetValue("targetTracked", ref targetTracked);

            if(targetTracked)
            {
                this.targetVessel = SupplyChainController.getVesselTrackingInfo(new Guid(node.GetValue("target")));
            } else
            {
                if(node.HasValue("target"))
                {
                    this.targetVessel = null;
                } else
                {
                    VesselData targetData = new VesselData();
                    targetData.Load(node.GetNode("targetData"));
                    this.targetVessel = targetData;
                }
            }

            ConfigNode[] xferNodes = node.GetNodes("Transfer");
            foreach(ConfigNode xferNode in xferNodes)
            {
                ResourceTransfer xfer = new ResourceTransfer();

                string destination = xferNode.GetValue("destination");

                xfer.resourceID = PartResourceLibrary.Instance.GetDefinition(xferNode.GetValue("resource")).id;
                xferNode.TryGetValue("amount", ref xfer.amount);

                int xferType = 0;
                xferNode.TryGetValue("type", ref xferType);

                xfer.type = (TransferType)xferType;

                if(destination == "origin")
                {
                    toOrigin.Add(xfer);
                } else if(destination == "target")
                {
                    toTarget.Add(xfer);
                } else
                {
                    Debug.LogError("[SupplyChain] ResourceTransferAction: Got invalid destination!");
                }
            }

            calculateRequirements();
        }

        public override void SaveCustom(ConfigNode node)
        {
            node.AddValue("type", "ResourceTransfer");

            /* Save target vessel ID first.
             * The target VesselData might not be registered with SupplyChainController,
             * so check for that. */
            if (targetVessel == null || targetVessel.vessel == null)
            {
                node.AddValue("targetTracked", false);
                node.AddValue("target", "none");
            } else
            {
                if(SupplyChainController.isVesselTracked(targetVessel.vessel))
                {
                    node.AddValue("targetTracked", true);
                    node.AddValue("target", targetVessel.trackingID.ToString());
                } else
                {
                    node.AddValue("targetTracked", false);
                    ConfigNode tgtNode = node.AddNode("targetData");
                    targetVessel.Save(tgtNode);
                }
            }

            /* Save the actual transfer data next. */
            foreach(ResourceTransfer xfer in this.toOrigin) {
                ConfigNode xferNode = node.AddNode("Transfer");
                xferNode.AddValue("destination", "origin");
                xferNode.AddValue("resource", PartResourceLibrary.Instance.GetDefinition(xfer.resourceID).name);
                xferNode.AddValue("amount", xfer.amount);
                xferNode.AddValue("type", (int)xfer.type);
            }

            foreach (ResourceTransfer xfer in this.toTarget)
            {
                ConfigNode xferNode = node.AddNode("Transfer");
                xferNode.AddValue("destination", "target");
                xferNode.AddValue("resource", PartResourceLibrary.Instance.GetDefinition(xfer.resourceID).name);
                xferNode.AddValue("amount", xfer.amount);
                xferNode.AddValue("type", (int)xfer.type);
            }
            
        }
    }
}
