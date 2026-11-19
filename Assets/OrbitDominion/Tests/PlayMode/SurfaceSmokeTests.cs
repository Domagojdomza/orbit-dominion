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
        public IEnumerator WalkingParkingPauseArchiveAndShipBoardingUseRealInput()
        {
            yield return Load();
            var world = Object.FindAnyObjectByType<PrototypeBootstrap>();
            var surface = world.Surface; surface.Enter(world.Worlds[0], true);
            yield return null;
            Vector3 eye = Camera.main.transform.position;
            Vector3 chest = GameObject.Find("Wayfarer astronaut").transform.position + Vector3.up * 1.5f;
            foreach (var hull in GameObject.Find("Wayfarer landing craft").GetComponentsInChildren<MeshCollider>())
                Assert.IsFalse(hull.Raycast(new Ray(eye, (chest - eye).normalized), out _, Vector3.Distance(eye, chest)),
                    "Ship wings must not obscure the person immediately after landing");
            Vector3 start = surface.ExplorerLocal, parked = surface.ParkedRoverLocal;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            yield return new WaitForSeconds(.35f);
            Assert.IsTrue(surface.OnFoot);
            Assert.Greater(Vector3.Distance(start, surface.ExplorerLocal), .4f);
            Assert.AreEqual(parked, surface.ParkedRoverLocal, "Walking must leave the rover parked");
            Assert.IsNotNull(GameObject.Find("Wayfarer astronaut"));
            world.Player.SetPause(true); Vector3 paused = surface.ExplorerLocal;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(paused, surface.ExplorerLocal); Assert.IsFalse(surface.TryInteract());
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); world.Player.SetPause(false);
            yield return new WaitForSeconds(.2f);
            var saved = JsonUtility.FromJson<GameSave>(JsonUtility.ToJson(world.Session.Snapshot()));
            Assert.IsTrue(saved.surface.onFoot); Assert.IsTrue(world.Session.Validate(saved));
            Assert.IsTrue(world.Session.Restore(saved));
            Assert.IsTrue(surface.OnFoot); Assert.AreEqual(parked, surface.ParkedRoverLocal);
            Assert.Less(Vector3.Distance(saved.surface.explorerPosition, surface.ExplorerLocal), .01f);
            surface.SetExplorerPose(parked + Vector3.left * 3.7f, 0, 12);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); yield return null; yield return null;
            Assert.IsFalse(surface.OnFoot);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return new WaitForSeconds(.25f);
            Assert.Greater(surface.RoverSpeed, 1.2f); Assert.IsFalse(surface.TryInteract(), "Moving rover cannot eject the character");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return new WaitForSeconds(.5f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); yield return null; yield return null;
            Assert.IsTrue(surface.OnFoot);
            Assert.Less(surface.RoverSpeed, .01f); Assert.IsFalse(surface.StartRally());
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
            surface.SetExplorerPose(surface.ShipEntrance, 270, 12);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); yield return null; yield return null;
            Assert.IsFalse(surface.Active); Assert.IsFalse(world.Player.FlightDisabled);
            Assert.IsNull(GameObject.Find("Wayfarer astronaut"));
        }

        [UnityTest]
        public IEnumerator WalkingAndFollowCameraRespectConstructedWalls()
        {
            yield return Load();
            var world = Object.FindAnyObjectByType<PrototypeBootstrap>(); var surface = world.Surface;
            surface.Enter(world.Worlds[0], true); surface.SetExplorerPose(Vector3.zero, 90, 12);
            Assert.IsTrue(surface.TryPlaceBuild(SurfaceBuildKind.Wall, new Vector3(4, 0, 0), 1));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return new WaitForSeconds(1.4f);
            Assert.Greater(surface.ExplorerLocal.x, 1); Assert.Less(surface.ExplorerLocal.x, 3.4f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
            surface.SetExplorerPose(new Vector3(7, 0, 0), 90, 0);
            var root = GameObject.Find("Wayfarer astronaut").transform.parent;
            Assert.Greater(root.InverseTransformPoint(Camera.main.transform.position).x, 4.5f,
                "The follow camera must pull in before crossing the wall");
        }

        [UnityTest]
        public IEnumerator OceanAllowsWalkingOnDeckButRejectsOpenWaterExit()
        {
            yield return Load();
            var world = Object.FindAnyObjectByType<PrototypeBootstrap>(); var surface = world.Surface;
            surface.Enter(world.Sector.Planet("veil-02"));
            Assert.IsFalse(surface.TryInteract()); Assert.IsFalse(surface.OnFoot);
            surface.SetExplorerPose(new Vector3(8, 0, -24), 0, 12);
            Assert.IsTrue(surface.TryInteract()); Assert.IsTrue(surface.OnFoot);
            float initialY = surface.ExplorerLocal.y;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            yield return new WaitForSeconds(1);
            Assert.Less(surface.ExplorerLocal.z, -21.3f);
            Assert.AreEqual(initialY, surface.ExplorerLocal.y, .01f);
            Assert.Greater(surface.ExplorerLocal.y, SurfaceRules.WaterLevel(SurfaceBiome.Ocean));
        }

        [UnityTest]
        public IEnumerator DetailedBuildingMeshesRespectSavedFootprintsAndReleaseOnDestruction()
        {
            var material = VisualFactory.Lit(Color.gray);
            var roots = new System.Collections.Generic.List<GameObject>();
            var meshes = new System.Collections.Generic.List<Mesh>();
            try
            {
                foreach (SurfaceBuildKind kind in System.Enum.GetValues(typeof(SurfaceBuildKind)))
                {
                    var root = new GameObject("Envelope QA / " + kind); roots.Add(root);
                    SurfaceArchitecture.Build(kind, root.transform, material, material, material, material, material, material, material);
                    var filters = root.GetComponentsInChildren<MeshFilter>();
                    Assert.AreEqual(1, filters.Length, "Static details should combine by material: " + kind);
                    Vector2 footprint = SurfaceSandboxRules.Footprint(kind, 0);
                    foreach (var filter in filters)
                    {
                        Mesh mesh = filter.sharedMesh; meshes.Add(mesh);
                        Assert.LessOrEqual(mesh.bounds.max.x, footprint.x * .5f + .01f, kind.ToString());
                        Assert.GreaterOrEqual(mesh.bounds.min.x, -footprint.x * .5f - .01f, kind.ToString());
                        Assert.LessOrEqual(mesh.bounds.max.z, footprint.y * .5f + .01f, kind.ToString());
                        Assert.GreaterOrEqual(mesh.bounds.min.z, -footprint.y * .5f - .01f, kind.ToString());
                        Assert.LessOrEqual(mesh.bounds.max.y, SurfaceSandboxRules.StructureHeight(kind) + .05f, kind.ToString());
                        Assert.AreEqual(mesh.vertexCount, mesh.uv.Length);
                        Assert.AreEqual(mesh.vertexCount, mesh.tangents.Length);
                        Vector3[] vertices = mesh.vertices, normals = mesh.normals; int[] indices = mesh.triangles;
                        foreach (var t in mesh.tangents) Assert.IsTrue(float.IsFinite(t.x) && float.IsFinite(t.y) && float.IsFinite(t.z));
                        for (int i = 0; i < indices.Length; i += 3)
                        {
                            int a = indices[i], b = indices[i + 1], c = indices[i + 2];
                            Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                            Assert.Greater(face.sqrMagnitude, 1e-12f, "Degenerate face: " + kind);
                            Assert.Greater(Vector3.Dot(face, normals[a] + normals[b] + normals[c]), 0, "Inverted face: " + kind);
                        }
                    }
                }
            }
            finally
            {
                foreach (var root in roots) Object.Destroy(root);
                Object.Destroy(material);
            }
            yield return null; yield return null;
            foreach (var mesh in meshes) Assert.IsTrue(mesh == null, "Generated mesh leaked after model destruction");
        }

        [UnityTest]
        public IEnumerator LandingRoverDriveRelicAndReturnToOrbitUseRealInput()
        {
            yield return Load();
            var world = Object.FindAnyObjectByType<PrototypeBootstrap>();
            Assert.IsFalse(world.Surface.Active);
            var home = world.Worlds[0];
            var orbitAmbientMode=RenderSettings.ambientMode;
            var orbitAmbientProbe=RenderSettings.ambientProbe;
            Color orbitSky=RenderSettings.ambientSkyColor,orbitEquator=RenderSettings.ambientEquatorColor,orbitGround=RenderSettings.ambientGroundColor;
            float orbitAmbientIntensity=RenderSettings.ambientIntensity;
            world.Player.transform.position = home.transform.position + Vector3.back * (home.Definition.radius + 180);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.L));
            yield return null; yield return null;
            Assert.IsTrue(world.Surface.Active);
            Assert.AreEqual("solace-01", world.Surface.PlanetId);
            Assert.IsNotNull(GameObject.Find("Wayfarer rover"));
            Assert.IsNotNull(GameObject.Find("Rover wheel / 0"));
            var roverRoot = GameObject.Find("Wayfarer rover");
            var roverMeshes = new System.Collections.Generic.List<Mesh>();
            foreach (var owner in roverRoot.GetComponentsInChildren<OwnedMesh>()) roverMeshes.Add(owner.mesh);
            Assert.Greater(roverMeshes.Count, 6);
            for (int wheel = 0; wheel < 6; wheel++)
            {
                var wheelRoot = GameObject.Find("Rover wheel / " + wheel).transform;
                Assert.AreEqual(Vector3.one, wheelRoot.localScale, "Articulated wheel dimensions must be baked into the mesh");
                Assert.Greater(wheelRoot.GetComponentsInChildren<MeshFilter>().Length, 0);
            }
            Assert.IsTrue(world.Player.FlightDisabled);
            Assert.IsFalse(Object.FindAnyObjectByType<ChaseCamera>().enabled);
            yield return new WaitForSeconds(.25f);
            var sunlight = GameObject.Find("Solace / directional starlight").transform;
            Assert.Less(Vector3.Angle(new Vector3(-.58f, -.36f, -.72f), sunlight.forward), .1f,
                "Local daylight must match the surface sky, including direction vectors shorter than one metre");
            var fillLight = new Color[1];
            RenderSettings.ambientProbe.Evaluate(new[] { Vector3.back }, fillLight);
            Assert.Greater(fillLight[0].maxColorComponent, .1f, "Surface Lit meshes need diffuse sky fill in shadow");
            float parkedShield = world.Combat.Player.Vitals.Shield;
            world.Combat.ApplyDamage(world.Combat.Player, 100, CombatFaction.Hostile);
            Assert.AreEqual(parkedShield, world.Combat.Player.Vitals.Shield);
            Assert.IsTrue(world.Surface.OnFoot, "Landing disembarks the astronaut at the ship");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return null; yield return null;
            Assert.IsFalse(world.Surface.OnFoot, "The parked rover must be reachable from the airlock");
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
            Assert.AreEqual(orbitAmbientMode,RenderSettings.ambientMode);
            Assert.AreEqual(orbitSky,RenderSettings.ambientSkyColor);
            Assert.AreEqual(orbitEquator,RenderSettings.ambientEquatorColor);
            Assert.AreEqual(orbitGround,RenderSettings.ambientGroundColor);
            Assert.AreEqual(orbitAmbientIntensity,RenderSettings.ambientIntensity);
            for (int channel = 0; channel < 3; channel++) for (int coefficient = 0; coefficient < 9; coefficient++)
                Assert.AreEqual(orbitAmbientProbe[channel, coefficient], RenderSettings.ambientProbe[channel, coefficient], .00001f);
            yield return null; yield return null;
            foreach (var mesh in roverMeshes) Assert.IsTrue(mesh == null, "Rover mesh leaked after leaving the surface");
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
            yield return null; yield return null;
            Transform tree = null;
            foreach (var node in world.GetComponentsInChildren<Transform>())
                if (node.name == "Surface scenery / Grove" && Mathf.Abs(node.localPosition.x) < 110 && Mathf.Abs(node.localPosition.z) < 110)
                { tree = node; break; }
            Assert.IsNotNull(tree);
            Vector3 treePosition = tree.localPosition;
            world.Empire.State.Reward(100, 100);
            Assert.IsTrue(world.Surface.TryPlaceBuild(SurfaceBuildKind.Foundation, treePosition, 0));
            Assert.IsFalse(tree.gameObject.activeSelf, "Trees must not grow through a constructed lot");
            var clearedLotSave = world.Session.Snapshot();
            world.Surface.Launch(false); yield return null; yield return null;
            Assert.IsTrue(world.Session.Restore(clearedLotSave));
            Transform restoredTree = null;
            foreach (var node in world.GetComponentsInChildren<Transform>(true))
                if (node.name == "Surface scenery / Grove" && Vector3.Distance(node.localPosition, treePosition) < .01f)
                { restoredTree = node; break; }
            Assert.IsNotNull(restoredTree);
            Assert.IsFalse(restoredTree.gameObject.activeSelf, "Saved lots must clear scenery on reconstruction");
            Assert.IsTrue(world.Surface.TryRecycleBuild(treePosition));
            Assert.IsTrue(restoredTree.gameObject.activeSelf, "Recycling the final overlapping lot restores its scenery");
        }
    }
}
