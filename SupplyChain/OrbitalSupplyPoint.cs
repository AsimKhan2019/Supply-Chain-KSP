﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain
{
    class OrbitalSupplyPoint : SupplyPoint
    {
        /**
         * The six keplerian elements, minus true anomaly at epoch
         */

        double ecc; // Eccentricity (unitless)
        double sma; // Semimajor Axis (meters)
        double inc; // Inclination (degrees)
        double lan; // Longitude of the Ascending Node (degrees)
        double argP; // Argument of Periapsis (degrees)

        public OrbitalSupplyPoint() { }

        public OrbitalSupplyPoint(Vessel v)
        {
            id = Guid.NewGuid();

            Orbit o = v.GetOrbitDriver().orbit;
            ecc = o.eccentricity;
            sma = o.semiMajorAxis;
            inc = o.inclination;
            lan = o.LAN;
            argP = o.argumentOfPeriapsis;
            soi = o.referenceBody;

            double pe = ((1 - ecc) * sma) - soi.Radius;
            double ap = ((1 + ecc) * sma) - soi.Radius;

            name = Convert.ToString(pe) + " x " + Convert.ToString(ap) + " @ " + soi.name;
        }

        public override bool isVesselAtPoint(Vessel v)
        {
            Debug.Log("[SupplyChain] Testing " + v.name + " against " + name + ": Attempting to retrieve orbit...");
            Orbit curOrbit = v.GetOrbit();

            if (curOrbit.referenceBody != soi)
            {
                Debug.Log("[SupplyChain] Testing " + v.name + " against " + name + ": Not in same SOI!");
                return false;
            }

            Debug.Log("[SupplyChain] Testing " + v.name + " against " + name + ": Testing orbits...");
            if ( // TODO: tweak these thresholds
                (Math.Abs(curOrbit.eccentricity - ecc) < 0.05) && // Allowed eccentricity variance is +- 0.05.
                (Math.Abs(curOrbit.semiMajorAxis - sma) < 10000.0) && // Allowed SMA variance is +- 10km.
                (Math.Abs(curOrbit.inclination - inc) < 5) // Allowed inclination variance is +- 5 degrees
               )
            {
                return true;
            }

            if(Math.Abs(curOrbit.eccentricity - ecc) > 0.05)
            {
                Debug.Log("[SupplyChain] Testing " + v.name + " against " + name + ": Eccentricity check failed! (diff = " + Convert.ToString(Math.Abs(curOrbit.eccentricity - ecc)) + ")");   
            }

            if (Math.Abs(curOrbit.semiMajorAxis - sma) > 10000.0)
            {
                Debug.Log("[SupplyChain] Testing " + v.name + " against " + name + ": SMA check failed! (diff = " + Convert.ToString(Math.Abs(curOrbit.semiMajorAxis - sma)) + ")");
            }

            if (Math.Abs(curOrbit.inclination - inc) > 5)
            {
                Debug.Log("[SupplyChain] Testing " + v.name + " against " + name + ": Inclination check failed! (diff = " + Convert.ToString(Math.Abs(curOrbit.inclination - inc)) + ")");
            }
            return false;
        }

        public override void moveVesselToPoint(Vessel v)
        {
            Orbit orb = v.GetOrbitDriver().orbit;

            orb.inclination = inc;
            orb.eccentricity = ecc;
            orb.semiMajorAxis = sma;
            orb.LAN = lan;
            orb.argumentOfPeriapsis = argP;
            orb.meanAnomalyAtEpoch = 0;
            orb.referenceBody = soi;

            orb.Init();
            orb.UpdateFromUT(Planetarium.GetUniversalTime());
        }

        public override string friendlyName()
        {
            return name;
        }

        public override void Load(ConfigNode node)
        {
            id = new Guid(node.GetValue("id"));
            int soiIdx = 0;
            node.TryGetValue("soi", ref soiIdx);
            soi = FlightGlobals.Bodies[soiIdx];
            node.TryGetValue("name", ref name);
            node.TryGetValue("ecc", ref ecc);
            node.TryGetValue("sma", ref sma);
            node.TryGetValue("inc", ref inc);
            node.TryGetValue("lan", ref lan);
            node.TryGetValue("argP", ref argP);
        }

        public override void Save(ConfigNode node)
        {
            node.AddValue("type", "orbit");
            node.AddValue("soi", soi.flightGlobalsIndex);
            node.AddValue("name", name);
            node.AddValue("id", id.ToString());
            node.AddValue("ecc", ecc);
            node.AddValue("sma", sma);
            node.AddValue("inc", inc);
            node.AddValue("lan", lan);
            node.AddValue("argP", argP);
        }
    }
}