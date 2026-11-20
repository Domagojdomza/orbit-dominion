using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace OrbitDominion
{
    public sealed partial class PrototypeBootstrap
    {
        IEnumerator CaptureGround()
        {
            bool background = Application.runInBackground;
            var inputBackground = InputSystem.settings.backgroundBehavior;
            Application.runInBackground = true; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard = InputSystem.AddDevice<Keyboard>(); var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                yield return new WaitForSeconds(2);
                Sector.enabled = false;
                Surface.Enter(Worlds[0], true); Surface.SetExplorerPose(new Vector3(-9, 0, -18), 0, 0);
                InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 2 });
                yield return new WaitForSeconds(1);
                foreach (var target in Surface.GroundContacts) if (target.training && target.root.localPosition.x == -9)
                {
                    Vector3 aim = target.root.position + Vector3.up * 1.25f - Camera.main.transform.position;
                    float yaw = Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg;
                    float tilt = -Mathf.Atan2(aim.y, new Vector2(aim.x, aim.z).magnitude) * Mathf.Rad2Deg;
                    InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 2, delta = new Vector2(
                        Mathf.DeltaAngle(Camera.main.transform.eulerAngles.y, yaw) / .14f,
                        -Mathf.DeltaAngle(Camera.main.transform.eulerAngles.x, tilt) / .14f) });
                }
                yield return new WaitForSeconds(.4f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../ground-practice.png"));
                yield return new WaitForSeconds(.3f);
                InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 3 });
                yield return new WaitForSeconds(.7f);
                InputSystem.QueueStateEvent(mouse, new MouseState());
                int practiceShots = Surface.GroundShots;
                yield return new WaitForSeconds(.4f);
                Surface.enabled = false;
                var hudRoot = GetComponent<UIDocument>().rootVisualElement;
                hudRoot.style.display = DisplayStyle.None;
                var astronaut = GameObject.Find("Wayfarer astronaut").transform;
                var view = astronaut.position + new Vector3(1.7f, 1.65f, 2.6f);
                Camera.main.transform.SetPositionAndRotation(view, Quaternion.LookRotation(astronaut.position + Vector3.up * 1.05f - view));
                Camera.main.fieldOfView = 45;
                yield return new WaitForSeconds(.7f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../ground-astronaut.png"));
                yield return new WaitForSeconds(.5f);
                // Extra suit inspection poses belong only to this isolated QA session.
                var suit = astronaut.GetComponent<SurfaceAstronaut>();
                view = astronaut.position + new Vector3(-1.6f,1.5f,-2.8f);
                Camera.main.transform.SetPositionAndRotation(view,Quaternion.LookRotation(astronaut.position+Vector3.up*1.04f-view));
                yield return new WaitForSeconds(.4f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../astronaut-rear.png"));
                yield return new WaitForSeconds(.4f);
                view = astronaut.position + new Vector3(2.3f,1.6f,2.3f);
                Camera.main.transform.SetPositionAndRotation(view,Quaternion.LookRotation(astronaut.position+Vector3.up*1.08f-view));
                for(int pose=0;pose<25;pose++){suit.Pose(Vector3.zero,.02f,true,0,false,0,0);yield return null;}
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../astronaut-aim.png"));
                yield return new WaitForSeconds(.4f);
                for(int pose=0;pose<18;pose++){suit.Pose(new Vector3(0,0,4.5f),.02f,false,0,false,0,0);yield return null;}
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../astronaut-stride.png"));
                yield return new WaitForSeconds(.4f);
                Surface.Launch(false); hudRoot.style.display = DisplayStyle.Flex; Surface.enabled = true;
                string id = "cinder-01"; Combat.DisableMilitary(id);
                Discovery.Catalog.Detect(id); Discovery.Catalog.CompleteScan(id);
                Surface.Enter(Sector.Planet(id), true); Surface.SetExplorerPose(new Vector3(0, 0, -2), 0, 0);
                // Staged clear ground isolates weapon/AI readability from district occlusion.
                int index = 0;
                foreach (var actor in Surface.GroundContacts) if (actor.hostile)
                {
                    actor.root.localPosition = new Vector3((index - 1) * 4, Surface.ExplorerLocal.y, 10 + (index % 2) * 4);
                    actor.home = actor.root.localPosition; index++;
                }
                InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 2 });
                yield return new WaitForSeconds(2);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../ground-combat.png"));
                yield return new WaitForSeconds(.4f); Surface.enabled = false;
                foreach (var actor in Surface.GroundContacts) if (actor.hostile)
                    for (int shot = 0; shot < 4 && actor.vitals.Alive; shot++)
                    {
                        Surface.GroundBattle.Tick(.25f, false);
                        Surface.TryFireGround(actor.root.position + Vector3.up * 1.25f);
                        yield return new WaitForSeconds(.22f);
                    }
                var archive = new GameArchive(System.IO.Path.Combine(Application.dataPath, "../qa-ground-v2.json"), Session.Validate);
                archive.Save(Session.Snapshot()); var loaded = archive.Load(out _);
                bool valid = Session.Validate(loaded) && Session.Restore(loaded);
                int guardsAfterLoad = Surface.GroundDefenders;
                Surface.enabled = true; Surface.SetExplorerPose(Surface.CommandLocal + Vector3.back * 3, 0, 6);
                InputSystem.QueueStateEvent(mouse, new MouseState()); InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G));
                yield return new WaitForSeconds(5);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../ground-capture.png"));
                yield return new WaitForSeconds(8);
                bool captured = Empire.State.Get(id).owned;
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../ground-secured.png"));
                archive.Save(Session.Snapshot());
                Debug.Log($"ORBIT_GROUND_CAPTURE: practiceShots={practiceShots}, guardsAfterLoad={guardsAfterLoad}, saveValid={valid}, captured={captured}, suitHull={Surface.GroundBattle.Suit.Hull:0}");
                yield return new WaitForSeconds(1);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
                Application.runInBackground = background; InputSystem.settings.backgroundBehavior = inputBackground;
            }
            Application.Quit();
        }
    }
}
