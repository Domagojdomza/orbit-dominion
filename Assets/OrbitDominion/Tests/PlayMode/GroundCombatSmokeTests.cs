using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrbitDominion.Tests
{
    public sealed class GroundCombatSmokeTests
    {
        Keyboard keyboard; Mouse mouse;
        InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
        [SetUp] public void Setup()
        {
            PrototypeBootstrap.SuppressPersistence = true;
            background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
        }
        [TearDown] public void Cleanup()
        {
            InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
#endif
            PrototypeBootstrap.SuppressPersistence = false; Time.timeScale = 1;
        }
        IEnumerator Load() { yield return SceneManager.LoadSceneAsync("Solace"); yield return null; yield return null; }

        [UnityTest] public IEnumerator RealMouseAimsFiresAndPauseAndBuilderBlockTheRifle()
        {
            yield return Load(); var world = Object.FindAnyObjectByType<PrototypeBootstrap>(); var s = world.Surface;
            s.Enter(world.Worlds[0], true); s.SetExplorerPose(new Vector3(-9, 0, -18), 0, 0);
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 2 });
            yield return new WaitForSeconds(.4f); Assert.IsTrue(s.Aiming);
            var target = System.Linq.Enumerable.First(s.GroundContacts, c => c.training && c.root.localPosition.x == -9);
            Vector3 aim = target.root.position + Vector3.up * 1.25f - Camera.main.transform.position;
            float yaw = Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(aim.y, new Vector2(aim.x, aim.z).magnitude) * Mathf.Rad2Deg;
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 2, delta = new Vector2(
                Mathf.DeltaAngle(Camera.main.transform.eulerAngles.y, yaw) / .14f,
                -Mathf.DeltaAngle(Camera.main.transform.eulerAngles.x, pitch) / .14f) });
            yield return new WaitForSeconds(.3f);
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 3 });
            yield return new WaitForSeconds(.65f);
            Assert.Greater(s.GroundShots, 0);
            bool hit = false; foreach (var actor in s.GroundContacts) if (actor.training && actor.vitals.Hull < 68) hit = true;
            Assert.IsTrue(hit, "Camera reticle and muzzle must agree on the practice target");
            int shots = s.GroundShots; world.Player.SetPause(true);
            yield return new WaitForSecondsRealtime(.2f); Assert.AreEqual(shots, s.GroundShots);
            Assert.IsFalse(s.ReloadRifle()); world.Player.SetPause(false);
            InputSystem.QueueStateEvent(mouse, new MouseState()); yield return null;
            s.ToggleBuildMode(); Assert.IsTrue(s.BuildMode);
            Assert.IsFalse(s.TryFireGround(Camera.main.transform.position + Camera.main.transform.forward * 10));
            s.ToggleBuildMode(); Assert.IsTrue(s.ReloadRifle());
            yield return new WaitForSeconds(1.8f); Assert.AreEqual(18, s.GroundBattle.Rounds);
        }

        [UnityTest] public IEnumerator ShoulderAimStrafesWithoutTurningAwayAndHipFireDoesNotZoom()
        {
            yield return Load(); var world=Object.FindAnyObjectByType<PrototypeBootstrap>();var s=world.Surface;
            s.Enter(world.Worlds[0],true);s.SetExplorerPose(new Vector3(-9,0,-18),0,0);
            InputSystem.QueueStateEvent(mouse,new MouseState{buttons=2});
            yield return new WaitForSeconds(.55f);
            Assert.IsTrue(s.Aiming);Assert.Less(Camera.main.fieldOfView,55);
            Vector3 before=s.ExplorerLocal;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));
            yield return new WaitForSeconds(.3f);
            var person=GameObject.Find("Wayfarer astronaut").transform;
            Assert.Greater(s.ExplorerLocal.x,before.x+.1f);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(person.eulerAngles.y,Camera.main.transform.eulerAngles.y)),2,
                "Aimed strafing keeps the body facing the reticle");
            var muzzle=GameObject.Find("ARC-18 / muzzle").transform;
            Assert.Less(Vector3.Angle(muzzle.forward,s.GroundAimPoint-muzzle.position),2,
                "The visible barrel must converge on the selected camera point");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            int shots=s.GroundShots;InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});
            yield return new WaitForSeconds(.6f);
            Assert.IsFalse(s.Aiming);Assert.Greater(s.GroundShots,shots);Assert.Greater(Camera.main.fieldOfView,63);
        }

        [UnityTest] public IEnumerator SuitGeometryHasUsableNormalsAndReleasesItsMeshes()
        {
            var root=new GameObject("Suit geometry QA");var mat=VisualFactory.Lit(Color.gray);
            var meshes=new System.Collections.Generic.List<Mesh>();
            try
            {
                var model=root.AddComponent<SurfaceAstronaut>();model.Build(mat,mat,mat,mat,mat);
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh=filter.sharedMesh;meshes.Add(mesh);
                    Assert.AreEqual(mesh.vertexCount,mesh.tangents.Length);
                    var points=mesh.vertices;var normals=mesh.normals;var indices=mesh.triangles;
                    foreach(var tangent in mesh.tangents)Assert.IsTrue(float.IsFinite(tangent.x)&&float.IsFinite(tangent.y)&&float.IsFinite(tangent.z));
                    for(int t=0;t<indices.Length;t+=3)
                    {
                        int a=indices[t],b=indices[t+1],c=indices[t+2];var face=Vector3.Cross(points[b]-points[a],points[c]-points[a]);
                        Assert.Greater(face.sqrMagnitude,1e-12f,mesh.name);
                        Assert.Greater(Vector3.Dot(face,normals[a]+normals[b]+normals[c]),0,"Inverted suit face: "+mesh.name);
                    }
                }
                for(int angle=0;angle<360;angle+=45)
                {
                    root.transform.rotation=Quaternion.Euler(0,angle,0);
                    model.Pose(new Vector3(2,0,3),.1f,true,-20,false,.12f,-.06f);
                    model.AlignRifle(root.transform.TransformPoint(new Vector3(0,3,30)));
                    foreach(var bone in root.GetComponentsInChildren<Transform>())
                        Assert.IsTrue(float.IsFinite(bone.position.x)&&float.IsFinite(bone.position.y)&&float.IsFinite(bone.position.z));
                }
            }
            finally{Object.Destroy(root);Object.Destroy(mat);}
            yield return null;yield return null;
            foreach(var mesh in meshes)Assert.IsTrue(mesh==null,"Suit mesh leaked on destruction");
        }

        [UnityTest] public IEnumerator WallsStopShotsAndNeutralCitizensAreProtected()
        {
            yield return Load(); var world = Object.FindAnyObjectByType<PrototypeBootstrap>(); var s = world.Surface;
            s.Enter(world.Worlds[0], true); s.enabled = false; s.SetExplorerPose(Vector3.zero, 0, 0);
            Assert.IsTrue(s.TryPlaceBuild(SurfaceBuildKind.Wall, new Vector3(0, 0, 8), 0));
            var actor = System.Linq.Enumerable.First(s.GroundContacts, c => c.training);
            actor.root.localPosition = new Vector3(0, s.ExplorerLocal.y, 16);
            Vector3 from = s.ExplorerLocal + Vector3.up * 1.4f, to = actor.root.localPosition + Vector3.up * 1.3f;
            Assert.Less(s.TraceGroundCover(from, to), .7f);
            Assert.IsTrue(s.TryFireGround(actor.root.parent.TransformPoint(to))); Assert.AreEqual(68, actor.vitals.Hull);
            s.Launch(false); s.Enter(world.Sector.Planet("veil-03"), true); s.SetExplorerPose(Vector3.zero, 0, 0);
            var civilian = s.GroundContacts[0]; civilian.root.localPosition = new Vector3(0, s.ExplorerLocal.y, 8);
            s.GroundBattle.Tick(1, false);
            Assert.IsTrue(s.TryFireGround(civilian.root.position + Vector3.up * 1.3f));
            Assert.AreEqual(100, civilian.vitals.Hull); Assert.AreEqual(2, world.Empire.State.Get("veil-03").faction);
        }

        [UnityTest] public IEnumerator GuardsFireSurviveSavingAndClearedTerminalTransfersOwnership()
        {
            yield return Load(); var world = Object.FindAnyObjectByType<PrototypeBootstrap>(); var s = world.Surface;
            string id = "cinder-01"; world.Combat.DisableMilitary(id);
            world.Discovery.Catalog.Detect(id); world.Discovery.Catalog.CompleteScan(id);
            s.Enter(world.Sector.Planet(id), true);
            s.SetExplorerPose(Vector3.zero, 0, 0);
            var guard = System.Linq.Enumerable.First(s.GroundContacts, c => c.hostile);
            guard.root.localPosition = new Vector3(0, s.ExplorerLocal.y, 10); guard.home = guard.root.localPosition;
            yield return new WaitForSeconds(2.2f);
            Assert.Less(s.GroundBattle.Suit.Shield, 60, "Visible alerted guard must return fire");
            Assert.IsFalse(world.Sector.SurfaceCaptureTick(id, true, true, true, 20));
            s.enabled = false;
            foreach (var c in s.GroundContacts) if (c.hostile) c.vitals.Damage(1000, CombatFaction.Union, CombatFaction.Hostile);
            var snapshot = JsonUtility.FromJson<GameSave>(JsonUtility.ToJson(world.Session.Snapshot()));
            Assert.IsTrue(world.Session.Validate(snapshot)); Assert.IsTrue(world.Session.Restore(snapshot));
            Assert.AreEqual(0, s.GroundDefenders, "Reloading cannot respawn defeated guards");
            Assert.IsTrue(world.Discovery.Catalog.HasDetails(id));
            Assert.IsFalse(world.Combat.LocalDefenders(id, CombatFaction.Union));
            int industry = world.Empire.State.Get(id).levels[0];
            s.SetExplorerPose(s.CommandLocal + Vector3.back * 2, 0, 0); s.enabled = true;
            world.Player.SetPause(true);
            Assert.IsFalse(world.Sector.SurfaceCaptureTick(id, true, true, false, 20));
            world.Player.SetPause(false);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G));
            float original = world.sectorSettings.captureSeconds;
            try
            {
                world.sectorSettings.captureSeconds = .5f;
                yield return new WaitForSeconds(.7f);
                Assert.AreEqual(1, world.Empire.State.Get(id).faction);
                Assert.AreEqual(industry, world.Empire.State.Get(id).levels[0]);
            }
            finally { world.sectorSettings.captureSeconds = original; }
        }
    }
}
