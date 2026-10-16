using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace OrbitDominion.Tests
{
    public sealed class SectorSmokeTests
    {
        Keyboard keyboard;InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
        [SetUp] public void Setup()
        {
            PrototypeBootstrap.SuppressPersistence=true;background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard=InputSystem.AddDevice<Keyboard>();
        }
        [TearDown] public void Cleanup()
        {
            InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=background;PrototypeBootstrap.SuppressPersistence=false;Time.timeScale=1;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
#endif
        }
        IEnumerator Load(){yield return SceneManager.LoadSceneAsync("Solace");yield return null;yield return null;yield return null;}
        [UnityTest] public IEnumerator MapInputPausesAndEscapeResumesTwelveWorldSector()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();Assert.AreEqual(12,w.Worlds.Length);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.M));yield return null;yield return null;Assert.IsTrue(w.Sector.MapOpen);Assert.IsTrue(w.Player.Paused);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;Assert.IsFalse(w.Sector.MapOpen);Assert.IsFalse(w.Player.Paused);
        }
        [UnityTest] public IEnumerator ProtectionBlocksDamageAndSatelliteFire()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();var satellite=w.Combat.Find("solace-01/defense/0");
            Assert.IsTrue(w.Sector.State.Protect("solace-01",w.Empire.State));float shield=satellite.Vitals.Shield;
            w.Combat.ApplyDamage(satellite,500,CombatFaction.Hostile);Assert.AreEqual(shield,satellite.Vitals.Shield);
            Assert.IsFalse(w.Combat.Fire(satellite,Vector3.up));yield return new WaitForSeconds(.3f);
            Assert.IsTrue(GameObject.Find("Planetary protection").activeInHierarchy);
        }
        [UnityTest] public IEnumerator ConquestNeedsScanAndDefendersClearedAndPreservesCivilianBuildings()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();w.Combat.enabled=false;string id="cinder-01";var p=w.Sector.Planet(id);
            w.Player.transform.position=p.transform.position+Vector3.back*(p.Definition.radius+180);w.Discovery.Catalog.Detect(id);w.Discovery.SelectTarget(p);
            Assert.IsFalse(w.Sector.CaptureTick(id,true,20));w.Discovery.Catalog.CompleteScan(id);Assert.IsFalse(w.Sector.CaptureTick(id,true,20));
            int mining=w.Empire.State.Get(id).levels[0],settlement=w.Empire.State.Get(id).Development;
            foreach(var actor in w.Combat.Actors)if(actor!=null&&actor.HomePlanet==id)w.Combat.ApplyDamage(actor,10000,CombatFaction.Union);
            Assert.IsTrue(w.Sector.CaptureTick(id,true,w.sectorSettings.captureSeconds));yield return null;yield return null;
            Assert.IsTrue(w.Empire.State.Get(id).owned);Assert.AreEqual(mining,w.Empire.State.Get(id).levels[0]);Assert.AreEqual(settlement,w.Empire.State.Get(id).Development);Assert.AreEqual(0,w.Empire.State.Get(id).levels[3]);Assert.Greater(w.Empire.State.Production(id).x,0);
        }
        [UnityTest] public IEnumerator FleetsLaunchFromEnemyTerritoryAndTravelContinuously()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();var f=w.Sector.LaunchFleet("cinder-01","solace-02");Assert.IsNotNull(f);
            var ship=w.Combat.Find(f.ships[0]);Vector3 start=ship.transform.position;
            Assert.Less(Vector3.Distance(start,w.Sector.Planet("cinder-01").transform.position),700);Assert.Greater(Vector3.Distance(start,w.Sector.Planet("solace-02").transform.position),20000);
            yield return new WaitForSeconds(.2f);Assert.Greater(Vector3.Distance(start,ship.transform.position),30);Assert.Less(Vector3.Distance(start,ship.transform.position),500);Assert.IsTrue(ship.Traveling);
        }
        [UnityTest] public IEnumerator NeutralCivilizationOnlyRetaliatesAfterAttack()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();var actor=w.Combat.Find("veil-03/command");Assert.AreEqual(CombatFaction.Neutral,actor.Faction);
            w.Combat.ApplyDamage(actor,10,CombatFaction.Union);Assert.AreEqual(CombatFaction.Hostile,actor.Faction);Assert.IsTrue(w.Sector.State.Get("veil-03").provoked);
        }
        [UnityTest] public IEnumerator TutorialWaitsForCompletedConstructionAndRecognizesEarlyActions()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();string id="solace-02";
            w.Discovery.Catalog.Detect(id);w.Discovery.Catalog.CompleteScan(id);w.Empire.State.StartColony(id,w.Discovery.Catalog,150,false,Vector3.back);w.Empire.State.Tick(12,id,false);
            w.Sector.State.Data.tutorial=7;Assert.IsTrue(w.Empire.State.StartBuilding(id,BuildingKind.Energy));w.Sector.TickTutorial();Assert.AreEqual(7,w.Sector.State.Data.tutorial);
            w.Empire.State.Tick(18,null,false);w.Sector.TickTutorial();Assert.AreEqual(8,w.Sector.State.Data.tutorial);
        }
        [UnityTest] public IEnumerator FullArchiveRoundTripRestoresShipFleetShieldsConstructionAndTutorial()
        {
            string folder=Path.Combine(Path.GetTempPath(),"OrbitFullSave-"+Guid.NewGuid().ToString("N"));string path=Path.Combine(folder,"flight.json");
            try
            {
                yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();w.Combat.enabled=false;
                w.Player.transform.SetPositionAndRotation(new Vector3(500,3000,-800),Quaternion.Euler(12,30,5));w.Sector.State.Protect("solace-01",w.Empire.State);
                w.Empire.State.StartBuilding("solace-01",BuildingKind.Mining);w.Sector.State.Data.tutorial=4;w.Sector.State.Data.anomalies.Add("ruins");
                var fleet=w.Sector.LaunchFleet("cinder-01","solace-02");var snapshot=w.Session.Snapshot();Assert.IsTrue(w.Session.Validate(snapshot));
                var archive=new GameArchive(path,w.Session.Validate);archive.Save(snapshot);var saved=archive.Load(out _);Assert.IsNotNull(saved);
                yield return Load();w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();Assert.IsTrue(w.Session.Restore(saved));yield return null;yield return null;
                Assert.Less(Vector3.Distance(w.Player.transform.position,saved.playerPosition),1);Assert.Greater(w.Sector.State.Get("solace-01").protection,0);Assert.AreEqual(0,w.Empire.State.Get("solace-01").construction);
                Assert.IsTrue(w.Sector.State.Data.anomalies.Contains("ruins"));Assert.IsNotNull(w.Combat.Find(fleet.ships[0]));Assert.AreEqual(1,UnityEngine.Object.FindObjectsByType<GameSession>().Length);
                Assert.AreEqual(12,UnityEngine.Object.FindObjectsByType<PlanetView>().Length);Assert.AreEqual(1,UnityEngine.Object.FindObjectsByType<PlayerFlight>().Length);
                var ids=new System.Collections.Generic.HashSet<string>();foreach(var actor in w.Combat.Actors)Assert.IsTrue(ids.Add(actor.Id),actor.Id);
                var second=new GameArchive(path,w.Session.Validate);second.Save(w.Session.Snapshot());File.WriteAllText(path,"broken");Assert.IsNotNull(second.Load(out string status));StringAssert.Contains("BACKUP",status);Assert.Throws<InvalidDataException>(()=>second.Save(w.Session.Snapshot()));
            }
            finally{foreach(var file in new[]{path,path+".tmp",path+".bak"})if(File.Exists(file))File.Delete(file);if(Directory.Exists(folder))Directory.Delete(folder);}
        }
        [UnityTest] public IEnumerator LegacyThreeWorldSaveMigratesWithoutRewritingOriginal()
        {
            string folder=Path.Combine(Path.GetTempPath(),"OrbitMigration-"+Guid.NewGuid().ToString("N"));string path=Path.Combine(folder,"empire-v1.json");
            try
            {
                yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();var defs=new[]{w.Worlds[0].Definition,w.Worlds[1].Definition,w.Worlds[2].Definition};
                var oldDiscovery=new DiscoveryCatalog(w.settings.galaxySeed,new[]{"solace-01","solace-02","solace-03"},"solace-01");oldDiscovery.Detect("solace-02");oldDiscovery.CompleteScan("solace-02");
                var oldEmpire=new EmpireState(defs,w.empireSettings,w.settings.galaxySeed);oldEmpire.StartColony("solace-02",oldDiscovery,150,false,Vector3.back);oldEmpire.Tick(12,"solace-02",false);
                new EmpireArchive(path).Save(oldEmpire,oldDiscovery);string original=File.ReadAllText(path);w.Session.ImportLegacy(folder);
                Assert.IsTrue(w.Empire.State.Get("solace-02").owned);Assert.IsTrue(w.Discovery.Catalog.HasDetails("solace-02"));Assert.AreEqual(3,w.Empire.State.Get("cinder-01").faction);Assert.AreEqual(original,File.ReadAllText(path));
            }
            finally{foreach(var file in new[]{path,path+".tmp",path+".bak"})if(File.Exists(file))File.Delete(file);if(Directory.Exists(folder))Directory.Delete(folder);}
        }
        [UnityTest] public IEnumerator InvestigationUsesIAndRewardsOnlyOnce()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();w.Player.transform.position=SectorController.AnomalyPositions[0]+Vector3.back*80;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.I));yield return new WaitForSeconds(w.sectorSettings.anomalySeconds+.2f);
            Assert.IsTrue(w.Sector.State.Data.anomalies.Contains("ruins"));float minerals=w.Empire.State.Minerals;
            w.Sector.InvestigateTick(20,true);Assert.AreEqual(minerals,w.Empire.State.Minerals);Assert.AreEqual(1,w.Sector.State.Data.anomalies.Count);
        }
        [UnityTest] public IEnumerator EnemyEconomyBuildsAndFleetOccupationChangesOwnership()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();w.Sector.State.Data.decision=0;
            yield return new WaitForSeconds(1.2f);Assert.Greater(w.Sector.State.Data.fleets.Count,0);Assert.GreaterOrEqual(w.Empire.State.Get("cinder-01").construction,0);
            w.Sector.State.Data.enemyMinerals+=500;w.Sector.State.Data.enemyEnergy+=500;
            var fleet=w.Sector.LaunchFleet("cinder-01","solace-04");Assert.IsNotNull(fleet);
            foreach(string id in fleet.ships){var actor=w.Combat.Find(id);actor.transform.position=actor.Destination;actor.Traveling=false;}
            w.Sector.TickFleets(w.sectorSettings.enemyCaptureSeconds);Assert.AreEqual(3,w.Empire.State.Get("solace-04").faction);Assert.IsTrue(w.Empire.State.Get("solace-01").owned);
        }
        [UnityTest] public IEnumerator RebuiltDefensesOnCapturedNeutralWorldBelongToNewOwner()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();w.Combat.enabled=false;string id="veil-03";
            w.Combat.Provoke(id);foreach(var actor in w.Combat.Actors)if(actor!=null&&actor.HomePlanet==id)w.Combat.ApplyDamage(actor,10000,CombatFaction.Union);
            var p=w.Sector.Planet(id);w.Player.transform.position=p.transform.position+Vector3.back*(p.Definition.radius+180);w.Discovery.Catalog.Detect(id);w.Discovery.Catalog.CompleteScan(id);w.Discovery.SelectTarget(p);
            Assert.IsTrue(w.Sector.CaptureTick(id,true,w.sectorSettings.captureSeconds));Assert.IsFalse(w.Sector.State.Get(id).provoked);
            Assert.IsTrue(w.Empire.State.StartBuilding(id,BuildingKind.Defense));w.Empire.State.Tick(w.empireSettings.constructionSeconds,null,false);yield return null;yield return null;
            Assert.AreEqual(CombatFaction.Union,w.Combat.Find(id+"/defense/0").Faction);
        }
    }
}
