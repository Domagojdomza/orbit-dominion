using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OrbitDominion.Tests
{
    public class DiscoveryTests
    {
        DiscoveryCatalog Catalog()=>new(73149,new[]{"home","frontier","ice"},"home");
        [Test] public void FreshGalaxyHidesAllNonHomeProperties()
        {
            var c=Catalog();Assert.AreEqual(DiscoveryState.Visited,c.State("home"));Assert.AreEqual(DiscoveryState.Unknown,c.State("frontier"));Assert.IsFalse(c.HasDetails("frontier"));
        }
        [Test] public void StatesCannotSkipScanOrRegress()
        {
            var c=Catalog();Assert.IsFalse(c.CompleteScan("frontier"));Assert.IsFalse(c.Visit("frontier"));
            c.Detect("frontier");Assert.IsFalse(c.Visit("frontier"));Assert.IsFalse(c.HasDetails("frontier"));
            Assert.IsTrue(c.CompleteScan("frontier"));Assert.IsTrue(c.Visit("frontier"));
            c.Detect("frontier");Assert.AreEqual(DiscoveryState.Visited,c.State("frontier"));Assert.IsFalse(c.Detect("missing"));
        }
        [Test] public void ScanRequiresFullDurationAndCompletesOnlyOnce()
        {
            var c=Catalog();c.Detect("frontier");var scan=new ScanSession();
            Assert.IsFalse(scan.Tick(c,"frontier",true,650,false,650,5,4));Assert.IsFalse(c.HasDetails("frontier"));
            Assert.IsTrue(scan.Tick(c,"frontier",true,650,false,650,5,1));Assert.IsTrue(c.HasDetails("frontier"));
            Assert.IsFalse(scan.Tick(c,"frontier",true,650,false,650,5,5));
        }
        [TestCase(false,400,false)][TestCase(true,651,false)][TestCase(true,400,true)]
        public void ReleasingLeavingRangeOrCruisingDiscardsPartialScan(bool held,float range,bool pulse)
        {
            var c=Catalog();c.Detect("frontier");var scan=new ScanSession();scan.Tick(c,"frontier",true,400,false,650,5,2);
            scan.Tick(c,"frontier",held,range,pulse,650,5,1);Assert.AreEqual(0,scan.Progress);Assert.IsFalse(c.HasDetails("frontier"));
        }
        [Test] public void TargetSwitchCannotTransferScanProgress()
        {
            var c=Catalog();c.Detect("frontier");c.Detect("ice");var scan=new ScanSession();
            scan.Tick(c,"frontier",true,400,false,650,5,4);scan.Tick(c,"ice",true,400,false,650,5,1);
            Assert.That(scan.Progress,Is.EqualTo(.2f).Within(.001f));Assert.IsFalse(c.HasDetails("ice"));
        }
        [Test] public void UnscannedCardDoesNotLeakDefinitionProperties()
        {
            var world=new WorldDefinition("frontier","SECRET NAME",Vector3.zero,10,Color.blue,Color.green,Color.white,Color.cyan,.5f,.5f)
                {survey=new PlanetSurvey("SECRET TYPE","SECRET ATMOSPHERE",24,88,57,84,"SECRET LIFE","None","SECRET OWNER",50,2,"SECRET TRAIT")};
            var hidden=SurveyPanel.Details(world,DiscoveryState.Detected);StringAssert.DoesNotContain("SECRET",hidden);StringAssert.DoesNotContain("88%",hidden);
            StringAssert.Contains("SECRET TYPE",SurveyPanel.Details(world,DiscoveryState.Scanned));
            StringAssert.DoesNotContain("SECRET OWNER",SurveyPanel.Details(world,DiscoveryState.Scanned));
            StringAssert.Contains("SECRET OWNER",SurveyPanel.Details(world,DiscoveryState.Visited));
        }
        [Test] public void InvalidRestoreDoesNotMutateLiveState()
        {
            var c=Catalog();c.Detect("frontier");var invalid=c.Snapshot();invalid.planets[1].id="home";
            Assert.Throws<ArgumentException>(()=>c.Restore(invalid));Assert.AreEqual(DiscoveryState.Detected,c.State("frontier"));
            invalid=c.Snapshot();invalid.seed++;Assert.IsFalse(c.IsValid(invalid));
            invalid=c.Snapshot();invalid.version++;Assert.IsFalse(c.IsValid(invalid));
            invalid=c.Snapshot();invalid.planets[1].state=(DiscoveryState)99;Assert.IsFalse(c.IsValid(invalid));
        }
        [Test] public void ArchiveRoundTripRetainsBeaconAndDiscoveries()
        {
            WithArchive((path,archive)=>
            {
                var c=Catalog();c.ReachBeacon();c.Detect("frontier");c.CompleteScan("frontier");archive.Save(c);
                var restored=Catalog();archive.Load(restored);Assert.IsTrue(restored.BeaconReached);Assert.AreEqual(DiscoveryState.Scanned,restored.State("frontier"));
                c.Visit("frontier");archive.Save(c);Assert.IsTrue(File.Exists(path+".bak"));Assert.IsFalse(File.Exists(path+".tmp"));
                archive.Load(restored);Assert.AreEqual(DiscoveryState.Visited,restored.State("frontier"));
            });
        }
        [Test] public void CorruptionRecoversBackupWithoutOverwritingOriginal()
        {
            WithArchive((path,archive)=>
            {
                var c=Catalog();c.Detect("frontier");archive.Save(c);c.CompleteScan("frontier");archive.Save(c);
                File.WriteAllText(path,"corrupt");var restored=Catalog();StringAssert.Contains("BACKUP",archive.Load(restored));
                Assert.AreEqual(DiscoveryState.Detected,restored.State("frontier"));Assert.Catch(()=>archive.Save(restored));Assert.AreEqual("corrupt",File.ReadAllText(path));
            });
        }
        void WithArchive(Action<string,DiscoveryArchive> test)
        {
            string folder=Path.Combine(Path.GetTempPath(),"OrbitDiscoveryTest-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,"survey.json");
            try{test(path,new DiscoveryArchive(path));}
            finally{foreach(string file in new[]{path,path+".tmp",path+".bak"})if(File.Exists(file))File.Delete(file);Directory.Delete(folder);}
        }
    }
}
