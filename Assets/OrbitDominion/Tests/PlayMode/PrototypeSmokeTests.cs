using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;

namespace OrbitDominion.Tests
{
    public class PrototypeSmokeTests
    {
        Keyboard keyboard;
        InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode previousInputBehavior;
#endif
        [SetUp] public void ConfigureBatchInput()
        {
            PrototypeBootstrap.SuppressPersistence=true;
            previousBackground=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            previousInputBehavior=InputSystem.settings.editorInputBehaviorInPlayMode;
            // Batch tests have no focused Game view. Route the virtual keyboard to the game explicitly.
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        }
        [TearDown] public void RestoreInput()
        {
            PrototypeBootstrap.SuppressPersistence=false;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior=previousBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=previousInputBehavior;
#endif
            Time.timeScale=1;
        }
        [UnityTest] public IEnumerator MainSceneCreatesOnePilotAndThreeWorlds()
        {
            yield return SceneManager.LoadSceneAsync("Solace");yield return null;
            Assert.AreEqual(1,Object.FindObjectsByType<PlayerFlight>().Length);
            Assert.AreEqual(12,Object.FindObjectsByType<PlanetView>().Length);
            Assert.IsNotNull(GameObject.Find("Meridian station"));Assert.IsNotNull(Camera.main);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ReloadDoesNotDuplicateSystems()
        {
            yield return SceneManager.LoadSceneAsync("Solace");yield return null;
            yield return SceneManager.LoadSceneAsync("Solace");yield return null;
            Assert.AreEqual(1,Object.FindObjectsByType<PrototypeBootstrap>().Length);
            Assert.AreEqual(1,Object.FindObjectsByType<PlayerFlight>().Length);
        }
        [UnityTest] public IEnumerator ThrustMovesShipAndBrakeStopsIt()
        {
            yield return SceneManager.LoadSceneAsync("Solace");yield return null;
            var player=Object.FindAnyObjectByType<PlayerFlight>();player.SetPause(false);
            keyboard=InputSystem.AddDevice<Keyboard>();Vector3 start=player.transform.position;
            InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.W));
            InputSystem.Update();
            Assert.IsTrue(keyboard.wKey.isPressed,"Virtual keyboard did not receive W input.");
            yield return new WaitForSeconds(.5f);
            Assert.Greater(Vector3.Distance(start,player.transform.position),1,$"Enabled={player.enabled}, paused={player.Paused}, speed={player.Speed}, delta={Time.deltaTime}, keyboard={keyboard.wKey.isPressed}");
            InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.Space));
            yield return new WaitForSeconds(.5f);Assert.Less(player.Speed,1);
            InputSystem.RemoveDevice(keyboard);
        }
        [UnityTest] public IEnumerator PulsePhysicallyTravelsAndDisengagesOnArrival()
        {
            yield return SceneManager.LoadSceneAsync("Solace");yield return null;
            var world=Object.FindAnyObjectByType<PrototypeBootstrap>();var player=world.Player;var target=world.Worlds[1].Definition;
            player.transform.SetPositionAndRotation(target.center-Vector3.forward*(target.radius+450),Quaternion.identity);
            Vector3 start=player.transform.position;keyboard=InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.P));
            yield return null;yield return null;
            Assert.IsTrue(player.Pulse,"Pulse should engage outside the gravity exclusion zone.");
            InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
            yield return new WaitForSeconds(5);
            Assert.Greater(Vector3.Distance(start,player.transform.position),200,"Pulse must physically move the ship.");
            Assert.IsFalse(player.Pulse,"Pulse should disengage on planetary approach.");
            Assert.GreaterOrEqual(Vector3.Distance(player.transform.position,world.Worlds[1].transform.position)-target.radius,player.settings.shipClearance-.1f);
        }
        [UnityTest] public IEnumerator ObstacleSafetyBrakesBeforeCollision()
        {
            yield return SceneManager.LoadSceneAsync("Solace");yield return null;
            var player=Object.FindAnyObjectByType<PlayerFlight>();var obstacle=new GameObject("Test obstacle");
            obstacle.transform.position=player.transform.position+player.transform.forward*200;obstacle.AddComponent<SphereCollider>().radius=15;
            keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.W));
                yield return new WaitForSeconds(5);
                Assert.Greater(player.DistanceTravelled,20);
                Assert.Greater(Vector3.Distance(player.transform.position,obstacle.transform.position),15+player.settings.shipClearance);
                Assert.Less(player.Speed,10);
            }
            finally {Object.Destroy(obstacle);}
        }
        [UnityTest] public IEnumerator PauseFreezesFlightAndResumes()
        {
            yield return SceneManager.LoadSceneAsync("Solace");yield return null;
            var player=Object.FindAnyObjectByType<PlayerFlight>();keyboard=InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.W));
            yield return new WaitForSeconds(.3f);player.SetPause(true);Vector3 stopped=player.transform.position;
            yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(stopped,player.transform.position);
            player.SetPause(false);yield return new WaitForSeconds(.2f);Assert.Greater(Vector3.Distance(stopped,player.transform.position),.5f);
        }
        [UnityTest] public IEnumerator OrbitPhaseMovesPlanetsAndAnchorsAndSurvivesSave()
        {
            yield return SceneManager.LoadSceneAsync("Solace");yield return null;
            var world=Object.FindAnyObjectByType<PrototypeBootstrap>();
            var home=world.Worlds[0];var nereid=world.Worlds[2];
            var command=world.Combat.Find("Corsair command");
            Vector3 homeStart=home.transform.position;
            Vector3 outpostOffset=command.transform.position-nereid.transform.position;
            world.Orbits.SetTime(480);
            Assert.Greater(Vector3.Distance(homeStart,home.transform.position),100);
            Assert.Less(Vector3.Distance(outpostOffset,command.transform.position-nereid.transform.position),.02f);
            world.Player.SetPause(true);
            Vector3 paused=home.transform.position;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.Less(Vector3.Distance(paused,home.transform.position),.02f);
            var snapshot=world.Session.Snapshot();
            Assert.IsTrue(world.Session.Validate(snapshot));
            world.Orbits.SetTime(900);
            Assert.IsTrue(world.Session.Restore(snapshot));
            Assert.AreEqual(snapshot.orbitalSeconds,world.Orbits.Elapsed,.01f);
            Assert.Less(Vector3.Distance(home.transform.position,paused),.02f);
            world.Player.SetPause(false);
        }
    }
}

