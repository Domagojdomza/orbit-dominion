using NUnit.Framework;
using UnityEngine;
namespace OrbitDominion.Tests
{
    public class FlightRulesTests
    {
        [TestCase(109,false)][TestCase(110,false)][TestCase(111,true)]
        public void PulseRespectsGravityBoundary(float distance,bool expected)=>Assert.AreEqual(expected,FlightRules.CanPulse(distance,110));
        [Test] public void ArrivalSlowsBeforeCutoff(){Assert.AreEqual(100,FlightRules.ApproachSpeed(110,110,100,1500));Assert.AreEqual(1500,FlightRules.ApproachSpeed(560,110,100,1500));Assert.That(FlightRules.ApproachSpeed(300,110,100,1500),Is.InRange(101,1499));}
        [Test] public void SurfaceGuardPushesShipOutsidePlanet(){var result=FlightRules.SafePosition(new Vector3(5,0,0),Vector3.zero,20,Vector3.up);Assert.AreEqual(new Vector3(20,0,0),result);}
        [Test] public void CenterCollisionHasSafeFallback(){Assert.AreEqual(Vector3.up*20,FlightRules.SafePosition(Vector3.zero,Vector3.zero,20,Vector3.up));}
        [Test] public void ClearPositionIsUnchanged(){var p=new Vector3(30,0,0);Assert.AreEqual(p,FlightRules.SafePosition(p,Vector3.zero,20,Vector3.up));}
    }
}
