using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace OrbitDominion.Tests
{
    public sealed class SectorTests
    {
        SectorSettings settings;EmpireSettings economy;SectorState sector;EmpireState empire;
        [SetUp] public void Setup()
        {
            settings=ScriptableObject.CreateInstance<SectorSettings>();economy=ScriptableObject.CreateInstance<EmpireSettings>();
            var world=new WorldDefinition("home","HOME",Vector3.zero,100,Color.blue,Color.green,Color.white,Color.cyan,.5f,.5f){survey=new PlanetSurvey("EARTH","Air",20,80,60,70,"Life","Industrial","Wayfarer Union",60,3,"")};
            empire=new EmpireState(new[]{world},economy,1);sector=new SectorState(new[]{"home"},settings);
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(settings);Object.DestroyImmediate(economy);}
        [Test] public void ProtectionPaysEnergyOnceAndCooldownBeginsAtExpiration()
        {
            float minerals=empire.Minerals,energy=empire.Energy;Assert.IsTrue(sector.Protect("home",empire));Assert.IsFalse(sector.Protect("home",empire));
            Assert.AreEqual(energy-settings.shieldCost,empire.Energy);Assert.AreEqual(minerals,empire.Minerals);
            sector.Tick(44,1);Assert.AreEqual(1,sector.Get("home").protection);Assert.AreEqual(0,sector.Get("home").cooldown);
            sector.Tick(3,1);Assert.AreEqual(178,sector.Get("home").cooldown);Assert.IsFalse(sector.Protect("home",empire));
            sector.Tick(178,1);empire.Reward(0,settings.shieldCost);Assert.IsTrue(sector.Protect("home",empire));
        }
        [Test] public void RecentAttackAndUnknownWorldCannotActivateProtection()
        {
            sector.Attacked("home");Assert.IsFalse(sector.Protect("home",empire));Assert.IsFalse(sector.Protect("missing",empire));
            sector.Tick(settings.recentAttackLock,1);Assert.IsTrue(sector.Protect("home",empire));
        }
        [TestCase(false,true,true,false,false)]
        [TestCase(true,false,true,false,false)]
        [TestCase(true,true,false,false,false)]
        [TestCase(true,true,true,true,false)]
        [TestCase(true,true,true,false,true)]
        public void InvalidCaptureResetsContinuousProgress(bool held,bool scanned,bool range,bool defenders,bool pulse)
        {
            Assert.IsFalse(sector.Capture("home",true,true,true,false,false,5));Assert.Greater(sector.Get("home").capture,0);
            Assert.IsFalse(sector.Capture("home",held,scanned,range,defenders,pulse,12));Assert.AreEqual(0,sector.Get("home").capture);
        }
        [Test] public void CaptureRequiresContinuousTimeAndCannotBypassProtection()
        {
            sector.Protect("home",empire);Assert.IsFalse(sector.Capture("home",true,true,true,false,false,20));sector.Tick(45,1);
            Assert.IsFalse(sector.Capture("home",true,true,true,false,false,11));Assert.IsTrue(sector.Capture("home",true,true,true,false,false,1));
        }
        [Test] public void VictoryResetsBelowEightAndRemainsSandboxAfterWinning()
        {
            sector.Tick(59,8);Assert.IsFalse(sector.Data.won);sector.Tick(1,7);Assert.AreEqual(0,sector.Data.victory);
            sector.Tick(60,8);Assert.IsTrue(sector.Data.won);sector.Tick(5,1);Assert.IsTrue(sector.Data.won);
        }
        [Test] public void HomeOwnershipCannotBeLostAndInvalidSectorRestoreIsAtomic()
        {
            Assert.IsFalse(empire.ChangeOwner("home",3));Assert.IsTrue(empire.Get("home").owned);
            var copy=JsonUtility.FromJson<SectorSave>(JsonUtility.ToJson(sector.Data));copy.enemyEnergy=float.NaN;Assert.IsFalse(sector.IsValid(copy));
            Assert.Throws<System.ArgumentException>(()=>sector.Restore(copy));Assert.AreEqual(300,sector.Data.enemyEnergy);
        }
        [Test] public void GalaxyHasTwelveStableWorldsAcrossSixArchetypesAndThreeSystems()
        {
            var home=new WorldDefinition[3];for(int i=0;i<3;i++)home[i]=new WorldDefinition("solace-0"+(i+1),"HOME",Vector3.zero,100,Color.blue,Color.green,Color.white,Color.cyan,.5f,.5f);
            var worlds=GalaxyCatalog.Expand(home,123);Assert.AreEqual(12,worlds.Length);
            var ids=new HashSet<string>();var archetypes=new HashSet<PlanetArchetype>();var systems=new HashSet<int>();
            foreach(var w in worlds){Assert.IsTrue(ids.Add(w.id));archetypes.Add(w.archetype);systems.Add(w.system);}
            Assert.AreEqual(6,archetypes.Count);Assert.AreEqual(3,systems.Count);Assert.AreEqual(2,GalaxyCatalog.SystemAt(GalaxyCatalog.Centers[2]));
        }
        [Test] public void StellarOrbitsPreserveAuthoredEpochAndFollowKeplerPeriod()
        {
            var inner=new WorldDefinition("inner","INNER",new Vector3(3000,20,0),100,Color.blue,Color.green,Color.white,Color.cyan,.5f,.5f);
            var outer=new WorldDefinition("outer","OUTER",new Vector3(6000,20,0),100,Color.blue,Color.green,Color.white,Color.cyan,.5f,.5f);
            var a=new OrbitalPath(Vector3.zero,inner.center,.02,1);
            var b=new OrbitalPath(Vector3.zero,outer.center,.02,1);
            Assert.AreEqual(inner.center,a.Position(0));
            Assert.Less(a.period,b.period);
            Assert.AreEqual(System.Math.Pow(b.semiMajor/a.semiMajor,1.5),b.period/a.period,1e-6);
            Assert.Less(Vector3.Distance(inner.center,a.Position(a.period)),.02f);
            Assert.Greater(Vector3.Distance(inner.center,a.Position(a.period*.25)),1000);
        }
        [Test] public void SurveyShowsCurrentOwnershipOnlyAfterCloseOrbit()
        {
            var planet=new WorldDefinition("p","PLANET",Vector3.zero,100,Color.blue,Color.green,Color.white,Color.cyan,.5f,.5f){survey=new PlanetSurvey("EARTH","Air",20,80,60,70,"Life","None","Unclaimed",0,0,"")};
            var c=new ColonyState{id="p",faction=3,levels=new[]{1,1,2,1,0}};
            StringAssert.DoesNotContain("Cinder Dominion",SurveyPanel.StrategicDetails(planet,DiscoveryState.Scanned,c));
            StringAssert.Contains("Cinder Dominion",SurveyPanel.StrategicDetails(planet,DiscoveryState.Visited,c));
            c.faction=1;c.owned=true;StringAssert.Contains("Wayfarer Union",SurveyPanel.StrategicDetails(planet,DiscoveryState.Visited,c));
        }
    }
}
