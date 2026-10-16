using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OrbitDominion.Tests
{
    public sealed class EmpireTests
    {
        EmpireSettings settings;
        WorldDefinition[] worlds;
        EmpireState empire;
        DiscoveryCatalog discovery;
        [SetUp] public void Setup()
        {
            settings=ScriptableObject.CreateInstance<EmpireSettings>();
            worlds=new[]{World("home",90),World("colony",80),World("ice",19)};
            discovery=new DiscoveryCatalog(1,new[]{"home","colony","ice"},"home");empire=new EmpireState(worlds,settings,1);
        }
        static WorldDefinition World(string id,int habitability)=>new(id,id,Vector3.zero,100,Color.blue,Color.green,Color.gray,Color.cyan,.5f,.5f){survey=new PlanetSurvey("TEST","Air",20,habitability,60,70,"Simple","None","Unclaimed",0,0,"")};
        [TearDown] public void Cleanup()=>UnityEngine.Object.DestroyImmediate(settings);
        void Scan(string id){discovery.Detect(id);discovery.CompleteScan(id);}
        void Colonize(){Scan("colony");Assert.IsTrue(empire.StartColony("colony",discovery,150,false,Vector3.back));empire.Tick(settings.deploymentSeconds,"colony",false);}
        [Test] public void ColonyRequiresScanRangeViabilityAndPaymentOnlyOnce()
        {
            float minerals=empire.Minerals;
            Assert.IsFalse(empire.StartColony("colony",discovery,150,false,Vector3.back));Scan("colony");Scan("ice");
            Assert.IsFalse(empire.StartColony("colony",discovery,221,false,Vector3.back));
            Assert.IsFalse(empire.StartColony("colony",discovery,150,true,Vector3.back));
            Assert.IsFalse(empire.StartColony("ice",discovery,150,false,Vector3.back));
            Assert.AreEqual(minerals,empire.Minerals);
            Assert.IsTrue(empire.StartColony("colony",discovery,150,false,Vector3.back));
            Assert.IsFalse(empire.StartColony("colony",discovery,150,false,Vector3.back));
            Assert.AreEqual(minerals-settings.colonyMinerals,empire.Minerals);
        }
        [Test] public void DeploymentPausesOutOfOrbitThenResumesWithoutSecondCharge()
        {
            Scan("colony");empire.StartColony("colony",discovery,150,false,Vector3.back);
            empire.Tick(4,"colony",false);empire.Tick(30,null,false);empire.Tick(30,"colony",true);
            Assert.AreEqual(8,empire.Get("colony").deploymentRemaining);Assert.IsFalse(empire.Get("colony").owned);
            empire.Tick(8,"colony",false);Assert.IsTrue(empire.Get("colony").owned);Assert.AreEqual(1,empire.Get("colony").Development);
        }
        [Test] public void OnlyOwnedWorldsProduceAndResourcePropertiesMatter()
        {
            Assert.AreEqual(Vector2.zero,empire.Production("colony"));var before=empire.TotalProduction;Colonize();
            Assert.Greater(empire.TotalProduction.x,before.x);Assert.Greater(empire.TotalProduction.y,before.y);
            Assert.AreEqual(Vector2.zero,empire.Production("ice"));
        }
        [Test] public void ConstructionIsTimedSerialAndEnforcesSlotsAndLevelCap()
        {
            Colonize();empire.Tick(500,null,false);
            Assert.IsTrue(empire.StartBuilding("colony",BuildingKind.Energy));Assert.AreEqual(0,empire.Get("colony").levels[1]);
            Assert.IsFalse(empire.StartBuilding("colony",BuildingKind.Mining));empire.Tick(18,null,false);
            Assert.AreEqual(1,empire.Get("colony").levels[1]);Assert.IsTrue(empire.StartBuilding("colony",BuildingKind.Mining));empire.Tick(18,null,false);
            Assert.IsFalse(empire.StartBuilding("colony",BuildingKind.Defense));
            Assert.IsTrue(empire.StartBuilding("colony",BuildingKind.Settlement));empire.Tick(36,null,false);
            Assert.IsTrue(empire.StartBuilding("colony",BuildingKind.Defense));empire.Tick(18,null,false);
            Assert.AreEqual(30,empire.Get("colony").Defense);
            Assert.IsTrue(empire.StartBuilding("colony",BuildingKind.Settlement));empire.Tick(54,null,false);
            Assert.IsFalse(empire.StartBuilding("colony",BuildingKind.Settlement));
        }
        [Test] public void InsufficientResourcesAndInvalidBuildingsCannotCharge()
        {
            Colonize();var save=empire.Snapshot(discovery);save.minerals=0;save.energy=0;Assert.IsTrue(empire.Restore(save,discovery));
            Assert.IsFalse(empire.StartBuilding("colony",BuildingKind.Energy));Assert.IsFalse(empire.StartBuilding("colony",(BuildingKind)80));
            Assert.AreEqual(0,empire.Minerals);Assert.AreEqual(0,empire.Energy);
        }
        [Test] public void IncomeIsIndependentOfTickSizeAcrossConstructionCompletion()
        {
            Colonize();empire.StartBuilding("colony",BuildingKind.Energy);var copy=new EmpireState(worlds,settings,1);copy.Restore(empire.Snapshot(discovery),discovery);
            empire.Tick(40,null,false);for(int i=0;i<80;i++)copy.Tick(.5f,null,false);
            Assert.AreEqual(empire.Minerals,copy.Minerals,.01f);Assert.AreEqual(empire.Energy,copy.Energy,.01f);
        }
        [Test] public void InvalidSaveRejectsWholeSnapshotWithoutMutation()
        {
            var save=empire.Snapshot(discovery);save.minerals=1;save.colonies[1].owned=true;
            Assert.IsFalse(empire.Restore(save,discovery));Assert.AreEqual(settings.startingMinerals,empire.Minerals);
            save=empire.Snapshot(discovery);save.energy=float.NaN;Assert.IsFalse(empire.IsValid(save,discovery));
            save=empire.Snapshot(discovery);save.colonies[0].construction=9;Assert.IsFalse(empire.IsValid(save,discovery));
            save=empire.Snapshot(discovery);save.colonies[0].levels=null;Assert.IsFalse(empire.IsValid(save,discovery));
        }
        [Test] public void ArchiveRestoresWorkAndRecoversBackupWithoutOverwritingCorruptPrimary()
        {
            string folder=Path.Combine(Path.GetTempPath(),"OrbitEmpireTest-"+Guid.NewGuid().ToString("N"));string path=Path.Combine(folder,"empire.json");
            try
            {
                Colonize();empire.StartBuilding("colony",BuildingKind.Energy);empire.Tick(5,null,false);var archive=new EmpireArchive(path);archive.Save(empire,discovery);
                empire.Tick(2,null,false);archive.Save(empire,discovery);
                var restored=new EmpireState(worlds,settings,1);archive.Load(restored,discovery);Assert.AreEqual(11,restored.Get("colony").constructionRemaining);
                File.WriteAllText(path,"broken");StringAssert.Contains("BACKUP",archive.Load(restored,discovery));Assert.AreEqual(13,restored.Get("colony").constructionRemaining);
                Assert.Throws<InvalidDataException>(()=>archive.Save(restored,discovery));Assert.AreEqual("broken",File.ReadAllText(path));
            }
            finally{foreach(string file in new[]{path,path+".bak",path+".tmp"})if(File.Exists(file))File.Delete(file);if(Directory.Exists(folder))Directory.Delete(folder);}
        }
        [Test] public void DeploymentAndDiscoveryRoundTripPreservesNewerSurveyProgress()
        {
            Scan("colony");empire.StartColony("colony",discovery,150,false,Vector3.right);empire.Tick(3,"colony",false);var save=empire.Snapshot(discovery);
            Scan("ice");Assert.IsTrue(empire.Restore(save,discovery));Assert.AreEqual(9,empire.Get("colony").deploymentRemaining);
            Assert.IsTrue(discovery.HasDetails("ice"));Assert.AreEqual(Vector3.right,empire.Get("colony").landingDirection);
        }
        [Test] public void IncompatibleSurveyJsonIsPreservedAndReportedWithoutThrowing()
        {
            string folder=Path.Combine(Path.GetTempPath(),"OrbitSurveyVersion-"+Guid.NewGuid().ToString("N"));string path=Path.Combine(folder,"survey.json");
            Directory.CreateDirectory(folder);
            try
            {
                var save=discovery.Snapshot();save.version=99;string json=JsonUtility.ToJson(save);File.WriteAllText(path,json);
                var archive=new DiscoveryArchive(path);StringAssert.Contains("INVALID",archive.Load(discovery));
                Assert.Throws<InvalidDataException>(()=>archive.Save(discovery));Assert.AreEqual(json,File.ReadAllText(path));
            }
            finally{if(File.Exists(path))File.Delete(path);Directory.Delete(folder);}
        }
        [TestCase("Rival Union",0)]
        [TestCase("Unclaimed",30)]
        public void OccupiedOrDefendedWorldCannotBeColonized(string owner,int defense)
        {
            Scan("colony");worlds[1].survey.owner=owner;worlds[1].survey.defense=defense;
            Assert.IsFalse(empire.StartColony("colony",discovery,150,false,Vector3.back));Assert.AreEqual(settings.startingMinerals,empire.Minerals);
        }
    }
}
