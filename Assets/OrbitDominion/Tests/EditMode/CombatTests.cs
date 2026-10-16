using NUnit.Framework;
using UnityEngine;
namespace OrbitDominion.Tests
{
    public sealed class CombatTests
    {
        [Test] public void ShieldAbsorbsDamageBeforeHullAndDeathIsTerminal()
        {
            var v=new VitalState(100,40);Assert.IsTrue(v.Damage(55,CombatFaction.Hostile,CombatFaction.Union));
            Assert.AreEqual(0,v.Shield);Assert.AreEqual(85,v.Hull);
            v.Damage(500,CombatFaction.Hostile,CombatFaction.Union);Assert.IsFalse(v.Alive);Assert.AreEqual(0,v.Hull);
            Assert.IsFalse(v.Damage(10,CombatFaction.Hostile,CombatFaction.Union));v.Tick(100,5,10);Assert.AreEqual(0,v.Shield);
            v.Reset();Assert.IsTrue(v.Alive);Assert.AreEqual(40,v.Shield);
        }
        [Test] public void FriendlyFireProtectionAndInvalidDamageCannotChangeVitals()
        {
            var v=new VitalState(100,40);
            Assert.IsFalse(v.Damage(50,CombatFaction.Union,CombatFaction.Union));
            Assert.IsFalse(v.Damage(50,CombatFaction.Hostile,CombatFaction.Union,true));
            foreach(float amount in new[]{-1,0,float.NaN,float.PositiveInfinity})Assert.IsFalse(v.Damage(amount,CombatFaction.Hostile,CombatFaction.Union));
            Assert.AreEqual(40,v.Shield);Assert.AreEqual(100,v.Hull);
        }
        [Test] public void RegenerationWaitsAfterLastHitAndHandlesLargeTicks()
        {
            var v=new VitalState(100,50);v.Damage(40,CombatFaction.Hostile,CombatFaction.Union);v.Tick(4,5,10);Assert.AreEqual(10,v.Shield);
            v.Tick(3,5,10);Assert.AreEqual(30,v.Shield);v.Damage(5,CombatFaction.Hostile,CombatFaction.Union);v.Tick(5,5,10);Assert.AreEqual(25,v.Shield);
            v.Tick(100,5,10);Assert.AreEqual(50,v.Shield);
        }
        [TestCase(false,false,false,0,false)]
        [TestCase(true,true,false,0,false)]
        [TestCase(true,false,true,0,false)]
        [TestCase(true,false,false,.1f,false)]
        [TestCase(true,false,false,0,true)]
        public void WeaponRequiresLiveUnpausedNonPulseShipAndReadyCooldown(bool alive,bool paused,bool pulse,float cooldown,bool expected)
            =>Assert.AreEqual(expected,CombatRules.CanFire(alive,paused,pulse,cooldown));
        [Test] public void RaySphereReturnsNearestSurfaceAndRejectsBehindOrMissedTargets()
        {
            Assert.AreEqual(90,CombatRules.SphereHit(Vector3.zero,Vector3.forward,new Vector3(0,0,100),10),.001f);
            Assert.AreEqual(float.PositiveInfinity,CombatRules.SphereHit(Vector3.zero,Vector3.forward,new Vector3(0,0,-100),10));
            Assert.AreEqual(float.PositiveInfinity,CombatRules.SphereHit(Vector3.zero,Vector3.forward,new Vector3(50,0,100),10));
            Assert.AreEqual(0,CombatRules.SphereHit(Vector3.zero,Vector3.forward,Vector3.zero,10));
        }
    }
}
