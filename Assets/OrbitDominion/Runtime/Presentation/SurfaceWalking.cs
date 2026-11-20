using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OrbitDominion
{
    public sealed partial class SurfaceExpedition
    {
        readonly List<(Transform root, Vector3 size, Vector3 center)> walkObstacles = new();
        readonly List<Collider> cameraHull = new();
        SurfaceAstronaut astronaut;
        WalkMotion walk;
        Vector3 parkedRover;
        float parkedHeading, walkCameraYaw;
        public bool OnFoot { get; private set; }
        public float WalkingSpeed => walk.velocity.magnitude;
        public Vector3 ParkedRoverLocal => OnFoot ? parkedRover : explorerLocal;
        public Vector3 ShipEntrance => new(4.6f, FootFloor(4.6f, -28), -28);

        static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        bool OnLandingDeck(float x, float z) => Mathf.Abs(x) < 6.1f && Mathf.Abs(z + 28) < 6.6f;
        float FootFloor(float x, float z)
        {
            float floor = Terrain(x, z);
            if (OnLandingDeck(x, z)) floor = Mathf.Max(floor, Ground(0, -28) + .225f);
            return SurfaceSandboxRules.BuiltFloor(sandbox.Builds, PlanetId, new Vector2(x, z), floor);
        }

        public string InteractionHint
        {
            get
            {
                if (!Active || buildMode) return "";
                if (!OnFoot) return "E / EXIT ROVER";
                if (FlatDistance(explorerLocal, ShipEntrance) < 2.5f) return "E / BOARD SHIP AND RETURN TO ORBIT";
                if (FlatDistance(explorerLocal, parkedRover) < 5.2f) return "E / ENTER ROVER";
                return $"ROVER {FlatDistance(explorerLocal, parkedRover):0} m  ·  SHIP {FlatDistance(explorerLocal, ShipEntrance):0} m";
            }
        }

        void BuildAstronaut()
        {
            var root = new GameObject("Wayfarer astronaut"); root.transform.SetParent(sceneRoot, false);
            astronaut = root.AddComponent<SurfaceAstronaut>();
            astronaut.Build(Make(new Color(.77f, .81f, .79f), .12f, null, .38f),
                Make(new Color(.055f, .075f, .09f), .12f, null, .24f),
                Make(new Color(.07f, .16f, .21f), .8f, null, .93f),
                Make(new Color(.93f, .31f, .08f), .22f, null, .45f),
                Make(new Color(.1f, .7f, .85f), .2f, new Color(.1f, 1.3f, 1.5f)));
            astronaut.gameObject.SetActive(false);
        }

        void BeginWalking(Vector3 position, float heading)
        {
            OnFoot = true; explorerLocal = position;
            explorerLocal.y = FootFloor(position.x, position.z);
            walk = new WalkMotion(Vector3.zero, heading); walkCameraYaw = heading;
            pitch = 12; cameraYawOffset = 0;
            astronaut.gameObject.SetActive(true);
            UpdateCamera(true);
        }

        void ArriveOnFoot()
        {
            SetExplorerPose(new Vector3(8, 0, -24), 0, 12);
            parkedRover = explorerLocal; parkedHeading = rover.heading;
            BeginWalking(new Vector3(4.4f, 0, -24), 0);
            Message = "AIRLOCK OPEN / Explore on foot · E beside the rover to drive";
        }

        public bool TryInteract()
        {
            if (!Active || world.Player.Paused || buildMode || Recovering) return false;
            if (!OnFoot) return TryExitRover();
            if (FlatDistance(explorerLocal, ShipEntrance) < 2.5f) { battle.Suit.Reset(); Launch(); return true; }
            if (FlatDistance(explorerLocal, parkedRover) >= 5.2f) return false;
            OnFoot = false; astronaut.gameObject.SetActive(false);
            explorerLocal = parkedRover; rover = new RoverMotion(parkedHeading, 0);
            walk = new WalkMotion(Vector3.zero, parkedHeading); cameraYawOffset = 0; pitch = 7;
            Message = "ROVER BOARDED / W/S drive · A/D steer · E to step outside";
            UpdateCamera(true); world.Session?.SaveNow(); return true;
        }

        bool TryExitRover()
        {
            if (Mathf.Abs(rover.speed) > 1.2f) { Message = "STOP THE ROVER / Release throttle before exiting"; return false; }
            parkedRover = explorerLocal; parkedHeading = rover.heading;
            // Check both doors and both ends; never place the person inside a building or water.
            foreach (Vector3 offset in new[] { Vector3.left * 3.7f, Vector3.right * 3.7f, Vector3.back * 5, Vector3.forward * 5 })
            {
                Vector3 at = parkedRover + Quaternion.Euler(0, parkedHeading, 0) * offset;
                at.y = FootFloor(at.x, at.z);
                if (!CanStand(at)) continue;
                rover = new RoverMotion(parkedHeading, 0);
                if (rallyActive) { rallyActive = false; UpdateRallyGates(); }
                BeginWalking(at, parkedHeading);
                Message = "ON FOOT / WASD walk · SHIFT run · Mouse look · E to board";
                world.Session?.SaveNow(); return true;
            }
            Message = "NO SAFE EXIT / Park beside dry ground or the landing platform"; return false;
        }

        bool CanStand(Vector3 feet)
        {
            if (Mathf.Abs(feet.x) > SurfaceRules.HalfWidth - 3 || Mathf.Abs(feet.z) > SurfaceRules.HalfWidth - 3 ||
                feet.y < SurfaceRules.WaterLevel(Biome) + .12f) return false;
            return !WalkObstructed(feet + Vector3.up * .9f, true);
        }

        bool WalkObstructed(Vector3 point, bool includeRover)
        {
            foreach (var build in sandbox.Builds)
            {
                if (build.planet != PlanetId || SurfaceSandboxRules.IsDriveable(build.kind)) continue;
                Vector2 footprint = SurfaceSandboxRules.Footprint(build.kind, 0);
                float height = SurfaceSandboxRules.StructureHeight(build.kind);
                if (SurfaceWalkRules.InsideBox(point, build.position + Vector3.up * height * .5f,
                    new Vector3(footprint.x, height, footprint.y), build.rotation * 90)) return true;
            }
            foreach (var obstacle in walkObstacles)
                if (obstacle.root != null && SurfaceWalkRules.InsideBox(point, obstacle.root.localPosition +
                    obstacle.root.localRotation * obstacle.center, obstacle.size, obstacle.root.localEulerAngles.y)) return true;
            foreach (var item in scenery)
            {
                if (item.root == null || !item.root.gameObject.activeSelf) continue;
                float radius = item.root.name.Contains("Outcrop") ? item.radius * .8f : .55f;
                if (point.y > item.root.localPosition.y && point.y < item.root.localPosition.y + item.height &&
                    FlatDistance(point, item.root.localPosition) < radius + SurfaceWalkRules.Radius) return true;
            }
            return includeRover && SurfaceWalkRules.InsideBox(point, parkedRover + Vector3.up * 1.5f,
                new Vector3(5.4f, 3, 6.8f), parkedHeading);
        }

        void UpdateWalking(Keyboard keys, Vector2 look)
        {
            float dt = Mathf.Min(Time.deltaTime, .1f);
            var mouse = Mouse.current;
            aiming = !buildMode && (mouse?.rightButton.isPressed == true || mouse?.leftButton.isPressed == true || firingPose > 0);
            sprinting = keys.leftShiftKey.isPressed && !aiming && !buildMode;
            walkCameraYaw = Mathf.Repeat(walkCameraYaw + look.x * .14f, 360);
            Vector2 input = buildMode ? Vector2.zero : new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
            walk = SurfaceWalkRules.Step(walk, aiming ? input * .6f : input, walkCameraYaw, sprinting, dt);
            if (aiming) walk = new WalkMotion(walk.velocity, Mathf.MoveTowardsAngle(walk.heading, walkCameraYaw, dt * 720));
            Vector3 start = explorerLocal;
            Vector3 move = walk.velocity * dt;
            int steps = Mathf.Max(1, Mathf.CeilToInt(move.magnitude / .15f));
            for (int i = 0; i < steps; i++)
            {
                Vector3 step = move / steps;
                if (!WalkStep(step)) { WalkStep(new Vector3(step.x, 0, 0)); WalkStep(new Vector3(0, 0, step.z)); }
            }
            float actualSpeed = dt > 0 ? FlatDistance(start, explorerLocal) / dt : 0;
            if (actualSpeed < .01f) walk = new WalkMotion(Vector3.zero, walk.heading);
            if (buildMode) HandleBuildInput(keys);
            UpdateCamera(false);
            Vector3 localVelocity = astronaut.transform.InverseTransformDirection(walk.velocity);
            if (actualSpeed < .01f) localVelocity = Vector3.zero;
            Vector3 lateral = astronaut.transform.right * .115f;
            astronaut.Pose(localVelocity, dt, aiming, pitch, battle.ReloadRemaining > 0,
                FootFloor(explorerLocal.x - lateral.x, explorerLocal.z - lateral.z) - explorerLocal.y,
                FootFloor(explorerLocal.x + lateral.x, explorerLocal.z + lateral.z) - explorerLocal.y);
            if (!buildMode) HandleRifleInput(keys);
        }

        bool WalkStep(Vector3 step)
        {
            if (step.sqrMagnitude < .000001f) return false;
            Vector3 next = explorerLocal + step; next.y = FootFloor(next.x, next.z);
            if (!CanStand(next) || !SurfaceWalkRules.CanStep(explorerLocal.y, next.y, step.magnitude)) return false;
            explorerLocal = next; return true;
        }

        void UpdateWalkingCamera(bool immediate)
        {
            astronaut.transform.localPosition = explorerLocal;
            astronaut.transform.localRotation = Quaternion.Euler(0, walk.heading, 0);
            Vector3 focus = explorerLocal + Vector3.up * 1.5f;
            Vector3 direction = Quaternion.Euler(Mathf.Clamp(pitch, -25, 55), walkCameraYaw, 0) * Vector3.forward;
            Vector3 shoulder = Quaternion.Euler(0, walkCameraYaw, 0) * Vector3.right * (aiming ? .65f : .32f);
            Vector3 desired = focus - direction * (aiming ? 2.7f : 5.2f) + Vector3.up * (aiming ? .15f : .6f) + shoulder;
            Vector3 localCamera = sceneRoot.InverseTransformPoint(lens.transform.position);
            desired = immediate ? desired : Vector3.Lerp(localCamera, desired, 1 - Mathf.Exp(-12 * Time.deltaTime));
            Vector3 delta = desired - focus;
            // The swept ship wings are not a box: raycast their actual rendered meshes.
            var ray = new Ray(sceneRoot.TransformPoint(focus), delta.normalized);
            float distance = delta.magnitude;
            foreach (var hull in cameraHull)
            {
                if (hull == null) continue;
                if (hull.Raycast(ray, out var hit, distance)) distance = Mathf.Max(.15f, hit.distance - .3f);
                // The focus can be below a thin wing; Unity ignores backfaces by default.
                var reverse = new Ray(sceneRoot.TransformPoint(focus + delta), -delta.normalized);
                if (hull.Raycast(reverse, out hit, delta.magnitude))
                    distance = Mathf.Min(distance, Mathf.Max(.15f, delta.magnitude - hit.distance - .3f));
            }
            delta = delta.normalized * distance;
            int samples = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .16f));
            Vector3 safe = focus;
            for (int i = 1; i <= samples; i++)
            {
                Vector3 sample = focus + delta * (i / (float)samples);
                if (sample.y < FootFloor(sample.x, sample.z) + .25f || WalkObstructed(sample, true)) break;
                safe = sample;
            }
            lens.transform.position = sceneRoot.TransformPoint(safe);
            Vector3 target = sceneRoot.TransformPoint(aiming ? focus + direction * 20 : focus + direction * .8f);
            lens.transform.rotation = Quaternion.LookRotation(target - lens.transform.position, Vector3.up);
            lens.fieldOfView = immediate ? (aiming ? 52 : 65) : Mathf.Lerp(lens.fieldOfView, aiming ? 52 : 65, 1 - Mathf.Exp(-12 * Time.deltaTime));
        }
    }
}
