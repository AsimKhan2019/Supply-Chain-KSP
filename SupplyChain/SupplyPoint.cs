using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SupplyChain
{
    public abstract class SupplyPoint : IConfigNode
    {
        public String name;
        public Guid id;
        public CelestialBody soi;
        
        public abstract bool isVesselAtPoint(Vessel v);
        public abstract void moveVesselToPoint(Vessel v);

        public abstract void guiDisplayData(int id);

        public abstract void Load(ConfigNode node);
        public abstract void Save(ConfigNode node);
    }
}
