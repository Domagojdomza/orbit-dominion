using System;
using UnityEngine;

namespace OrbitDominion
{
    // Keplerian paths in the XZ plane. The authored positions are the epoch (t = 0),
    // so existing player coordinates and old saves still describe the same starting scene.
    public readonly struct OrbitalPath
    {
        public readonly Vector3 star, epoch;
        public readonly double semiMajor, eccentricity, periapsis, meanAtEpoch, period;

        public OrbitalPath(Vector3 star, Vector3 epoch, double eccentricity, double stellarMass)
        {
            this.star=star;this.epoch=epoch;
            double dx=epoch.x-star.x,dz=epoch.z-star.z;
            double radius=Math.Sqrt(dx*dx+dz*dz);
            this.eccentricity=eccentricity;
            // Different initial anomalies keep the system from reaching periapsis together.
            double initialAnomaly=Math.Atan2(dz,dx)*.37;
            periapsis=Math.Atan2(dz,dx)-initialAnomaly;
            semiMajor=radius*(1+eccentricity*Math.Cos(initialAnomaly))/(1-eccentricity*eccentricity);
            double initialE=2*Math.Atan2(Math.Sqrt(1-eccentricity)*Math.Sin(initialAnomaly/2),Math.Sqrt(1+eccentricity)*Math.Cos(initialAnomaly/2));
            meanAtEpoch=initialE-eccentricity*Math.Sin(initialE);
            period=3600*Math.Pow(semiMajor/3000,1.5)/Math.Sqrt(stellarMass);
        }

        public Vector3 Position(double seconds)
        {
            if(seconds==0)return epoch;
            double mean=(meanAtEpoch+2*Math.PI*(seconds/period%1));
            double eccentric=mean;
            for(int i=0;i<7;i++)eccentric-=(eccentric-eccentricity*Math.Sin(eccentric)-mean)/(1-eccentricity*Math.Cos(eccentric));
            double radius=semiMajor*(1-eccentricity*Math.Cos(eccentric));
            double anomaly=2*Math.Atan2(Math.Sqrt(1+eccentricity)*Math.Sin(eccentric/2),Math.Sqrt(1-eccentricity)*Math.Cos(eccentric/2));
            double angle=periapsis+anomaly;
            return new Vector3(star.x+(float)(radius*Math.Cos(angle)),epoch.y,star.z+(float)(radius*Math.Sin(angle)));
        }
    }

    public static class OrbitalRules
    {
        public static OrbitalPath Planet(WorldDefinition planet)
        {
            uint hash=2166136261;
            foreach(char c in planet.id)hash=(hash^c)*16777619;
            double eccentricity=.015+(hash%31)*.001;
            return new OrbitalPath(GalaxyCatalog.StarCenters[planet.system],planet.center,eccentricity,GalaxyCatalog.StarMasses[planet.system]);
        }
        public static float MoonDegreesPerSecond(float planetRadius)
        {
            // Larger planetary systems have slower moons; a full orbit takes 6-10 minutes.
            return 360f/(360f+planetRadius*.65f);
        }
    }
}
