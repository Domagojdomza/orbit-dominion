using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrbitDominion.Tests
{
    public sealed class SurfaceSmokeTests
    {
        Keyboard keyboard;
        InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
        [SetUp]
        public void Setup()
        {
            PrototypeBootstrap.SuppressPersistence = true;
            background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard = InputSystem.AddDevice<Keyboard>();
        }
        [TearDown]
        public void Cleanup()
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
#endif
            PrototypeBootstrap.SuppressPersistence = false;
            Time.timeScale = 1;
        }
        IEnumerator Load() { yield return SceneManager.LoadSceneAsync("Solace"); yield return null; yield return null; yield return null; }

        [UnityTest]
        public IEnumerator LandingRoverDriveRelicAndReturnToOrbitUseRealInput()
        {
            yield return Load();
            var world = Object.FindAnyObjectByType<PrototypeBootstrap>();
            Assert.IsFalse(world.Surface.Active);
            var home = world.Worlds[0];
            world.Player.transform.position = home.transform.position + Vector3.back * (home.Definition.radius + 180);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.L));
            yield return null; yield return null;
            Assert.IsTrue(world.Surface.Active);
            Assert.AreEqual("solace-01", world.Surface.PlanetId);
            Assert.IsNotNull(GameObject.Find("Wayfarer rover"));
            Assert.IsNotNull(GameObject.Find("Rover wheel / 0"));
            Assert.IsTrue(world.Player.FlightDisabled);
            Assert.IsFalse(Object.FindAnyObjectByType<ChaseCamera>().enabled);
            float parkedShield = world.Combat.Player.Vitals.Shield;
            world.Combat.ApplyDamage(world.Combat.Player, 100, CombatFaction.Hostile);
            Assert.AreEqual(parkedShield, world.Combat.Player.Vitals.Shield);
            Vector3 before = world.Surface.ExplorerLocal;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return null; yield return null;
            Assert.Greater(Vector3.Distance(before, world.Surface.ExplorerLocal), .001f);
            Assert.Greater(world.Surface.RoverSpeed, 0);
            float initialHeading = world.Surface.RoverHeading;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
            yield return null; yield return null;
            Assert.Greater(world.Surface.RoverHeading, initialHeading);
            Vector2 relic = SurfaceRules.RelicPositions("solace-01", world.settings.galaxySeed)[0];
            world.Surface.SetExplorerPose(new Vector3(relic.x, 0, relic.y), 0, 12);
            int prior = world.Surface.RelicsFound;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F));
            yield return null; yield return null;
            Assert.AreEqual(prior + 1, world.Surface.RelicsFound);
            var snapshot = world.Session.Snapshot();
            Assert.IsTrue(world.Session.Validate(snapshot));
            Assert.AreEqual("solace-01", snapshot.surface.activePlanet);
            var serialized = JsonUtility.FromJson<GameSave>(JsonUtility.ToJson(snapshot));
            Assert.IsTrue(world.Session.Validate(serialized));
            Assert.AreEqual(1, serialized.surface.collected.Count);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.L));
            yield return null; yield return null;
            Assert.IsFalse(world.Surface.Active);
            Assert.IsFalse(world.Player.FlightDisabled);
            Assert.IsTrue(Object.FindAnyObjectByType<ChaseCamera>().enabled);
            Assert.IsTrue(world.Session.Restore(serialized));
            Assert.IsTrue(world.Surface.Active);
            Assert.AreEqual(prior + 1, world.Surface.RelicsFound);
        }

        [UnityTest]
        public IEnumerator OceanAndNeutralCivilizationGenerateDistinctSurfaceScenes()
        {
            yield return Load();
            var world = Object.FindAnyObjectByType<PrototypeBootstrap>();
            world.Surface.Enter(world.Sector.Planet("veil-02"));
            yield return null;
            Assert.AreEqual(SurfaceBiome.Ocean, world.Surface.Biome);
            Assert.IsNotNull(GameObject.Find("Open ocean"));
            Assert.IsNotNull(GameObject.Find("Pelagic glider"));
            Assert.IsTrue(world.Surface.RoverAfloat);
            Assert.IsNotNull(GameObject.Find("Rover flotation pontoon"));
            world.Surface.Launch();
            world.Surface.Enter(world.Sector.Planet("veil-03"));
            yield return null;
            Assert.AreEqual(SurfaceBiome.Temperate, world.Surface.Biome);
            Assert.IsNotNull(GameObject.Find("Ithacan citizen"));
            Assert.IsNotNull(GameObject.Find("Occupied habitat"));
        }

        [UnityTest]
        public IEnumerator PlanetBuilderAndRoverRallyPersistThroughFullSave()
        {
            yield return Load();
            var world = Object.FindAnyObjectByType<PrototypeBootstrap>();
            world.Surface.Enter(world.Worlds[0]); yield return null;
            float minerals = world.Empire.State.Minerals;
            Assert.IsTrue(world.Surface.TryPlaceBuild(SurfaceBuildKind.Foundation, new Vector3(12, 0, 12), 0));
            Assert.IsTrue(world.Surface.TryPlaceBuild(SurfaceBuildKind.Wall, new Vector3(12, 0, 20), 1));
            Assert.Less(world.Empire.State.Minerals, minerals);
            Assert.AreEqual(2, world.Surface.LocalBuildCount);
            Assert.IsNotNull(GameObject.Find("Player build / Foundation / solace-01/build/1"));
            var route = world.Surface.RallyRoute;
            world.Surface.SetExplorerPose(new Vector3(route[0].x, 0, route[0].y), 0, 10);
            Assert.IsTrue(world.Surface.StartRally());
            yield return null;
            for (int i = 1; i < route.Count; i++)
            {
                world.Surface.SetExplorerPose(new Vector3(route[i].x, 0, route[i].y), 0, 10);
                yield return null;
            }
            Assert.IsFalse(world.Surface.RallyActive);
            Assert.AreEqual(1, world.Surface.RallyRecord.completions);
            var snapshot = world.Session.Snapshot();
            Assert.IsTrue(world.Session.Validate(snapshot));
            var serialized = JsonUtility.FromJson<GameSave>(JsonUtility.ToJson(snapshot));
            Assert.AreEqual(2, serialized.surface.sandbox.builds.Count);
            Assert.AreEqual(1, serialized.surface.sandbox.rallies.Count);
            world.Surface.Launch(false);
            Assert.IsTrue(world.Session.Restore(serialized));
            Assert.IsTrue(world.Surface.Active);
            Assert.AreEqual(2, world.Surface.LocalBuildCount);
            Assert.IsNotNull(GameObject.Find("Player build / Wall / solace-01/build/2"));
        }
    }
}
