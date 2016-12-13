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
            this.targetVessel = target;
            this.timeRequired = 3600; // 1 hour
            this.toTarget = toTarget;
            this.toOrigin = toOrigin;

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
                } else if(xfer.type == TransferType.TRANSFER_ALL || xfer.type == TransferType.TRANSFER_AMOUNT)
                {
                    double cur = 0, max = 0;
                    linkVessel.getResourceCount(xfer.resourceID, out cur, out max);
                    targetVessel.modifyResource(xfer.resourceID, -1 * cur);
                    linkVessel.setResourceToExtreme(xfer.resourceID, false);
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
                    double cur = 0, max = 0;
                    targetVessel.getResourceCount(xfer.resourceID, out cur, out max);
                    linkVessel.modifyResource(xfer.resourceID, -1 * cur);
                    targetVessel.setResourceToExtreme(xfer.resourceID, false);
                }
            }
        }
    }
}
