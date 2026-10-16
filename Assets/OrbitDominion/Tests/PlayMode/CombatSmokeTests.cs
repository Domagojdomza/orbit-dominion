using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace OrbitDominion.Tests
{
    public sealed class CombatSmokeTests
    {
        Keyboard keyboard;Mouse mouse;InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
        [SetUp] public void Setup()
        {
            PrototypeBootstrap.SuppressPersistence=true;background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
        }
        [TearDown] public void Cleanup()
        {
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.settings.backgroundBehavior=background;PrototypeBootstrap.SuppressPersistence=false;Time.timeScale=1;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
#endif
        }
        IEnumerator Load(){yield return SceneManager.LoadSceneAsync("Solace");yield return null;yield return null;}
        CombatActor EnemyAhead(PrototypeBootstrap w)
        {
            w.Player.transform.SetPositionAndRotation(new Vector3(0,3000,0),Quaternion.identity);w.Player.StopFlight();
            return w.Combat.SpawnHostile("Test interceptor",CombatRole.Fighter,new Vector3(0,3000,150));
        }
        [UnityTest] public IEnumerator MouseFiresRSelectsAndImpactIsVisible()
        {
            yield return Load();var w=Object.FindAnyObjectByType<PrototypeBootstrap>();var enemy=EnemyAhead(w);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R));yield return null;yield return null;
            Assert.AreEqual(enemy,w.Combat.Target);
            InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));yield return null;yield return null;
            Assert.Greater(w.Combat.ShotsFired,0);Assert.Less(enemy.Vitals.Shield,30);Assert.IsNotNull(GameObject.Find("Energy beam"));Assert.IsNotNull(GameObject.Find("Shield impact"));
        }
        [UnityTest] public IEnumerator PausedWeaponsCannotDamageAndCooldownPreventsDoubleFire()
        {
            yield return Load();var w=Object.FindAnyObjectByType<PrototypeBootstrap>();var enemy=EnemyAhead(w);w.Combat.enabled=false;
            w.Player.SetPause(true);Assert.IsFalse(w.Combat.FirePlayer());Assert.AreEqual(30,enemy.Vitals.Shield);
            w.Player.SetPause(false);Assert.IsTrue(w.Combat.FirePlayer());Assert.IsFalse(w.Combat.FirePlayer());Assert.AreEqual(6,enemy.Vitals.Shield);
        }
        [UnityTest] public IEnumerator AsteroidAndPlanetBlockEnergyBeams()
        {
            yield return Load();var w=Object.FindAnyObjectByType<PrototypeBootstrap>();var enemy=EnemyAhead(w);w.Combat.enabled=false;
            var rock=GameObject.CreatePrimitive(PrimitiveType.Sphere);rock.transform.position=new Vector3(0,3000,70);rock.transform.localScale=Vector3.one*30;Physics.SyncTransforms();
            Assert.IsTrue(w.Combat.FirePlayer());Assert.AreEqual(30,enemy.Vitals.Shield);Object.Destroy(rock);yield return null;
            var planet=w.Worlds[1];w.Player.transform.SetPositionAndRotation(planet.transform.position-Vector3.forward*(planet.Definition.radius+100),Quaternion.identity);
            enemy.transform.position=planet.transform.position+Vector3.forward*(planet.Definition.radius+50);w.Combat.Player.Cooldown=0;
            Assert.IsTrue(w.Combat.FirePlayer());Assert.AreEqual(30,enemy.Vitals.Shield);
        }
        [UnityTest] public IEnumerator BuiltSatelliteAutomaticallyFiresAtHostileAndRepairsAfterDestruction()
        {
            yield return Load();var w=Object.FindAnyObjectByType<PrototypeBootstrap>();CombatActor satellite=null;
            foreach(var actor in w.Combat.Actors)if(actor.Faction==CombatFaction.Union&&actor.Role==CombatRole.Satellite){satellite=actor;break;}
            Assert.IsNotNull(satellite);var enemy=w.Combat.SpawnHostile("Satellite test target",CombatRole.Satellite,satellite.transform.position+Vector3.up*100);
            yield return new WaitForSeconds(1);Assert.Less(enemy.Vitals.Shield,30);
            w.Combat.ApplyDamage(satellite,1000,CombatFaction.Hostile);Assert.IsFalse(satellite.Vitals.Alive);
            w.Sector.State.Get(satellite.HomePlanet).recentAttack=0;satellite.RepairRemaining=.05f;yield return new WaitForSeconds(.1f);Assert.IsTrue(satellite.Vitals.Alive);
        }
        [UnityTest] public IEnumerator PlayerDeathRespawnsAtHomeAndPreservesEmpire()
        {
            yield return Load();var w=Object.FindAnyObjectByType<PrototypeBootstrap>();EnemyAhead(w);float minerals=w.Empire.State.Minerals;
            w.Combat.ApplyDamage(w.Combat.Player,1000,CombatFaction.Hostile);Assert.IsFalse(w.Combat.Player.Vitals.Alive);Assert.IsTrue(w.Player.FlightDisabled);
            Assert.IsNotNull(GameObject.Find("Combat explosion"));yield return new WaitForSeconds(w.combatSettings.respawnSeconds+.15f);
            Vector3 homeOrbit=w.Worlds[0].transform.position+PrototypeBootstrap.StartPosition-w.Worlds[0].Definition.center;
            Assert.IsTrue(w.Combat.Player.Vitals.Alive);Assert.IsFalse(w.Player.FlightDisabled);Assert.Less(Vector3.Distance(w.Player.transform.position,homeOrbit),1);
            Assert.IsTrue(w.Empire.State.Get(w.Worlds[0].Definition.id).owned);Assert.GreaterOrEqual(w.Empire.State.Minerals,minerals);
            w.Combat.ApplyDamage(w.Combat.Player,1000,CombatFaction.Hostile);Assert.IsTrue(w.Combat.Player.Vitals.Alive,"Rescue protection prevents immediate repeat death.");
        }
        [UnityTest] public IEnumerator HostileEncounterWarnsThenFightersApproachAndDamageShields()
        {
            yield return Load();var w=Object.FindAnyObjectByType<PrototypeBootstrap>();w.Player.transform.position=w.Combat.OutpostPosition+Vector3.back*400;
            Assert.IsFalse(w.Combat.Engaged);yield return new WaitForSeconds(w.combatSettings.warningSeconds+3.5f);
            Assert.IsTrue(w.Combat.Engaged);Assert.Less(w.Combat.Player.Vitals.Shield,w.combatSettings.playerShield);Assert.IsFalse(w.Player.Pulse);
        }
        [UnityTest] public IEnumerator PulseBlocksWeaponsAndIncomingFireDisengagesAndLocksPulse()
        {
            yield return Load();var w=Object.FindAnyObjectByType<PrototypeBootstrap>();w.Combat.enabled=false;
            w.Player.transform.SetPositionAndRotation(new Vector3(0,3000,0),Quaternion.identity);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.P));yield return null;yield return null;
            Assert.IsTrue(w.Player.Pulse);Assert.IsFalse(w.Combat.FirePlayer());
            w.Combat.ApplyDamage(w.Combat.Player,8,CombatFaction.Hostile);Assert.IsFalse(w.Player.Pulse);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.P));yield return null;yield return null;
            Assert.IsFalse(w.Player.Pulse,"Combat lock must prevent immediately re-engaging cruise.");
        }
    }
}
