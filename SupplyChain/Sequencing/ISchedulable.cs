using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SupplyChain.Sequencing
{
    public interface ISchedulable
    {
        /* Called by scheduler. This method needs to check conditions, advance state, etc. */
        void schedulerUpdate();

        /* Called by VesselData.lockVessel-- said VesselData passes itself as argument. */
        void onLockTaken(VesselData vd);
    }
}
