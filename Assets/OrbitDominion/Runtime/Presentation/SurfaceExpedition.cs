using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OrbitDominion
{
    [DefaultExecutionOrder(60)]
    public sealed class SurfaceExpedition : MonoBehaviour
    {
        static readonly Vector3 SurfaceOrigin = new(0, -80000, 0);
        readonly HashSet<string> collected = new();
        readonly List<Material> materials = new();
        readonly List<GameObject> relicVisuals = new();
        readonly Dictionary<string, GameObject> buildVisuals = new();
        readonly List<GameObject> rallyGates = new();
        readonly SurfaceSandboxState sandbox = new();
        readonly Transform[] wheels = new Transform[6];
        readonly GameObject[] pontoons = new GameObject[2];
        PrototypeBootstrap world;
        Camera lens;
        ChaseCamera chase;
        Transform sceneRoot, explorer;
        GameObject buildCursor;
        PlanetView planet;
        Vector3 explorerLocal;
        Vector2[] relicPositions;
        float pitch, cameraYawOffset, pingRemaining, wheelAngle, rallyElapsed;
        RoverMotion rover;
        Vector2[] rallyRoute;
        int buildLayer, buildRotation, rallyCheckpoint;
        SurfaceBuildKind selectedBuild = SurfaceBuildKind.Foundation;
        bool buildMode, rallyActive;
        Material buildAlloy, buildPanel, buildGlass, buildPreview, buildConcrete, buildRoad, buildMarking, buildHazard, rallyIdle, rallyCurrent;
        bool previousFog;
        FogMode previousFogMode;
        Color previousFogColor;
        float previousFogDensity, previousFarClip;
        CameraClearFlags previousClearFlags;
        Color previousBackground;

        public bool Active => planet != null;
        public string PlanetId => planet?.Definition.id;
        public string PlanetName => planet?.Definition.title;
        public SurfaceBiome Biome => planet == null ? SurfaceBiome.Temperate : SurfaceRules.Biome(planet.Definition);
        public Vector3 ExplorerLocal => explorerLocal;
        public float RoverSpeed => rover.speed;
        public float RoverHeading => rover.heading;
        public bool RoverAfloat => Active && Terrain(explorerLocal.x, explorerLocal.z) < SurfaceRules.WaterLevel(Biome) + .3f;
        public int RelicsFound => collected.Count;
        public bool BuildMode => buildMode;
        public SurfaceBuildKind SelectedBuild => selectedBuild;
        public int LocalBuildCount { get { int count = 0; foreach (var build in sandbox.Builds) if (build.planet == PlanetId) count++; return count; } }
        public bool RallyActive => rallyActive;
        public int RallyCheckpoint => rallyCheckpoint;
        public float RallyElapsed => rallyElapsed;
        public IReadOnlyList<Vector2> RallyRoute => rallyRoute;
        public SurfaceRallySave RallyRecord => Active ? sandbox.Rally(PlanetId) : null;
        public string Message { get; private set; } = "";

        public void Initialize(PrototypeBootstrap bootstrap, Camera camera, ChaseCamera flightCamera)
        {
            world = bootstrap; lens = camera; chase = flightCamera;
        }

        public string LandingHint
        {
            get
            {
                if (Active) return "L / RETURN TO ORBIT";
                if (world?.Player == null || world.Player.Paused) return "";
                var nearest = NearbyScannedPlanet();
                if (nearest == null) return "";
                if (world.Player.Pulse) return "SURFACE ENTRY / Disengage pulse";
                if (world.Player.Speed > 35) return "SURFACE ENTRY / Brake below 35 m/s";
                if (world.Combat.ActiveThreat) return "SURFACE ENTRY / Clear immediate threat";
                if (world.Empire.State.Get(nearest.Definition.id).faction == 3 && world.Combat.LocalDefenders(nearest.Definition.id, CombatFaction.Union))
                    return "SURFACE ENTRY / Clear hostile defenses";
                return "L / LAND ON " + nearest.Definition.title.ToUpperInvariant();
            }
        }

        PlanetView NearbyScannedPlanet()
        {
            PlanetView nearest = null;
            float distance = 320;
            foreach (var candidate in world.Worlds)
            {
                float range = Vector3.Distance(world.Player.transform.position, candidate.transform.position) - candidate.Definition.radius;
                if (range < distance && world.Discovery.Catalog.HasDetails(candidate.Definition.id)) { nearest = candidate; distance = range; }
            }
            return nearest;
        }

        public bool TryLand()
        {
            if (Active || world.Player.Paused || world.Player.Pulse || world.Player.Speed > 35 || world.Combat.ActiveThreat || !world.Combat.Player.Vitals.Alive) return false;
            var candidate = NearbyScannedPlanet();
            if (candidate == null) return false;
            if (world.Empire.State.Get(candidate.Definition.id).faction == 3 && world.Combat.LocalDefenders(candidate.Definition.id, CombatFaction.Union)) return false;
            Enter(candidate);
            world.Session?.SaveNow();
            return true;
        }

        public void Enter(PlanetView destination)
        {
            if (Active || destination == null) return;
            planet = destination;
            world.Discovery.Catalog.Visit(destination.Definition.id);
            world.Discovery.SelectTarget(destination);
            world.Player.StopFlight();
            world.Player.FlightDisabled = true;
            chase.enabled = false;
            previousFarClip = lens.farClipPlane;
            previousClearFlags = lens.clearFlags;
            previousBackground = lens.backgroundColor;
            previousFog = RenderSettings.fog;
            previousFogMode = RenderSettings.fogMode;
            previousFogColor = RenderSettings.fogColor;
            previousFogDensity = RenderSettings.fogDensity;
            sceneRoot = new GameObject("Surface expedition / " + destination.Definition.title).transform;
            sceneRoot.SetParent(transform, false);
            sceneRoot.position = SurfaceOrigin;
            BuildSite();
            BuildPlayerStructures();
            BuildRallyCourse();
            explorerLocal = new Vector3(0, Ground(0, 0), 0);
            rover = new RoverMotion(0, 0); pitch = 7; cameraYawOffset = 0; wheelAngle = 0;
            explorer = new GameObject("Wayfarer rover").transform;
            explorer.SetParent(sceneRoot, false);
            BuildRover();
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = SkyColor();
            lens.farClipPlane = 950;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = SkyColor();
            RenderSettings.fogDensity = Biome == SurfaceBiome.Ocean ? .0018f : .0028f;
            Message = "ROVER DEPLOYED / " + destination.Definition.title + " · Drive and recover hidden relics";
            UpdateCamera(true);
        }

        public void Launch(bool persist = true)
        {
            if (!Active) return;
            string name = planet.Definition.title;
            planet = null;
            if (sceneRoot != null) Destroy(sceneRoot.gameObject);
            sceneRoot = null; explorer = null;
            buildCursor = null; buildVisuals.Clear(); rallyGates.Clear(); rallyRoute = null;
            buildMode = false; rallyActive = false; rallyElapsed = 0; rallyCheckpoint = 0;
            rover = new RoverMotion(0, 0);
            for (int i = 0; i < wheels.Length; i++) wheels[i] = null;
            for (int i = 0; i < pontoons.Length; i++) pontoons[i] = null;
            foreach (var material in materials) if (material != null) Destroy(material);
            materials.Clear(); relicVisuals.Clear();
            lens.farClipPlane = previousFarClip;
            lens.clearFlags = previousClearFlags;
            lens.backgroundColor = previousBackground;
            RenderSettings.fog = previousFog;
            RenderSettings.fogMode = previousFogMode;
            RenderSettings.fogColor = previousFogColor;
            RenderSettings.fogDensity = previousFogDensity;
            chase.enabled = true;
            world.Player.FlightDisabled = false;
            Message = "ORBIT RESTORED / " + name;
            if (persist) world.Session?.SaveNow();
        }

        public SurfaceSave Snapshot() => new()
        {
            activePlanet = PlanetId,
            explorerPosition = Active ? explorerLocal : Vector3.zero,
            yaw = Active ? rover.heading : 0,
            pitch = Active ? pitch : 0,
            collected = new List<string>(collected),
            sandbox = sandbox.Snapshot()
        };

        public void Restore(SurfaceSave save)
        {
            if (Active) Launch(false);
            collected.Clear();
            sandbox.Restore(save?.sandbox);
            if (save == null) return;
            foreach (string key in save.collected) collected.Add(key);
            if (string.IsNullOrEmpty(save.activePlanet)) return;
            var destination = world.Sector.Planet(save.activePlanet);
            Enter(destination);
            SetExplorerPose(save.explorerPosition, save.yaw, save.pitch);
        }

        public void SetExplorerPose(Vector3 position, float heading, float tilt)
        {
            if (!Active) return;
            float x = Mathf.Clamp(position.x, -SurfaceRules.HalfWidth + 3, SurfaceRules.HalfWidth - 3);
            float z = Mathf.Clamp(position.z, -SurfaceRules.HalfWidth + 3, SurfaceRules.HalfWidth - 3);
            explorerLocal = new Vector3(x, DriveGround(x, z), z);
            rover = new RoverMotion(heading, 0); cameraYawOffset = 0;
            pitch = Mathf.Clamp(tilt, -45, 65);
            UpdateCamera(true);
        }

        public bool TryCollect()
        {
            if (!Active || world.Player.Paused) return false;
            float best = 3.8f; int index = -1;
            for (int i = 0; i < relicPositions.Length; i++)
            {
                if (collected.Contains(RelicKey(i))) continue;
                float distance = Vector2.Distance(new Vector2(explorerLocal.x, explorerLocal.z), relicPositions[i]);
                if (distance < best) { best = distance; index = i; }
            }
            if (index < 0) { Message = "RELIC SIGNAL / Move closer, then press F"; return false; }
            collected.Add(RelicKey(index));
            if (relicVisuals[index] != null) relicVisuals[index].SetActive(false);
            world.Empire.State.Reward(60, 40);
            Message = "ANCIENT RELIC RECOVERED / +60 minerals · +40 energy · " + collected.Count + " total";
            world.Session?.SaveNow();
            return true;
        }

        public float NearestRelicDistance
        {
            get
            {
                if (!Active) return -1;
                float best = float.MaxValue;
                for (int i = 0; i < relicPositions.Length; i++) if (!collected.Contains(RelicKey(i)))
                    best = Mathf.Min(best, Vector2.Distance(new Vector2(explorerLocal.x, explorerLocal.z), relicPositions[i]));
                return best == float.MaxValue ? -1 : best;
            }
        }

        public string SignalHint => !Active ? "" : NearestRelicDistance < 0 ? "ALL LOCAL RELICS RECOVERED" :
            pingRemaining > 0 || NearestRelicDistance < 16 ? $"RELIC SIGNAL / {NearestRelicDistance:0} m · {RelicBearing} · F within 4 m" : "I / PULSE SCANNER FOR RELICS";

        public string BuildHint
        {
            get
            {
                if (!Active) return "";
                if (!buildMode) return "B / OPEN PLANET BUILDER";
                Vector2 cost = SurfaceSandboxRules.Cost(selectedBuild);
                return $"BUILD / {selectedBuild.ToString().ToUpperInvariant()} · {cost.x:0} M {cost.y:0} E · LMB PLACE · RMB RECYCLE · Q/E LEVEL · R ROTATE\n" +
                    "1 FLOOR · 2 WALL · 3 RAMP · 4 HAB · 5 BEACON · 6 ROAD · 7 ARCOLOGY · 8 INDUSTRY · 9 SPACEPORT";
            }
        }

        public string ActivityHint
        {
            get
            {
                if (!Active) return "";
                if (rallyActive) return $"ROVER RALLY / GATE {Mathf.Min(rallyCheckpoint + 1, SurfaceSandboxRules.RallyCheckpoints)}/{SurfaceSandboxRules.RallyCheckpoints} · {rallyElapsed:0.0}s";
                var record = sandbox.Rally(PlanetId);
                return record == null ? "ROVER RALLY / DRIVE TO THE CYAN GATE · T TO START" : $"ROVER RALLY / BEST {record.bestTime:0.00}s · T AT START GATE";
            }
        }

        string RelicBearing
        {
            get
            {
                float best = float.MaxValue; Vector2 chosen = Vector2.zero;
                for (int i = 0; i < relicPositions.Length; i++) if (!collected.Contains(RelicKey(i)))
                {
                    float distance = Vector2.Distance(new Vector2(explorerLocal.x, explorerLocal.z), relicPositions[i]);
                    if (distance < best) { best = distance; chosen = relicPositions[i]; }
                }
                float angle = Mathf.Atan2(chosen.x - explorerLocal.x, chosen.y - explorerLocal.z) * Mathf.Rad2Deg;
                float turn = Mathf.DeltaAngle(rover.heading, angle);
                return turn < -20 ? "LEFT" : turn > 20 ? "RIGHT" : "AHEAD";
            }
        }

        string RelicKey(int index) => planet.Definition.id + "/relic/" + index;
        float Terrain(float x, float z) => SurfaceRules.Height(Biome, SurfaceRules.StableSeed(planet.Definition.id, world.settings.galaxySeed), x, z);
        float Ground(float x, float z) => Mathf.Max(Terrain(x, z), SurfaceRules.WaterLevel(Biome) + .13f);
        float DriveGround(float x, float z) => SurfaceSandboxRules.BuiltFloor(sandbox.Builds, PlanetId, new Vector2(x, z), Ground(x, z));
        Color SkyColor() => SurfacePalette.For(Biome).horizon;

        public void ToggleBuildMode()
        {
            if (!Active) return;
            if (!world.Empire.State.Get(PlanetId).owned)
            {
                buildMode = false; Message = "PLANET BUILDER / Establish or capture this world before building"; return;
            }
            buildMode = !buildMode;
            if (buildCursor != null) buildCursor.SetActive(buildMode);
            if (buildMode) { rover = new RoverMotion(rover.heading, 0); Message = "PLANET BUILDER ONLINE / Grid construction linked to empire resources"; UpdateBuildCursor(); }
            else Message = "PLANET BUILDER CLOSED / Rover controls restored";
        }

        Vector3 BuildTarget()
        {
            Vector3 forward = Quaternion.Euler(0, rover.heading, 0) * Vector3.forward;
            Vector3 raw = explorerLocal + forward * 9;
            float x = Mathf.Round(raw.x / SurfaceSandboxRules.Grid) * SurfaceSandboxRules.Grid;
            float z = Mathf.Round(raw.z / SurfaceSandboxRules.Grid) * SurfaceSandboxRules.Grid;
            return SurfaceSandboxRules.Snap(new Vector3(x, 0, z), Ground(x, z), buildLayer);
        }

        public bool TryPlaceBuild(SurfaceBuildKind kind, Vector3 rawPosition, int rotation, int layer = 0)
        {
            if (!Active || !world.Empire.State.Get(PlanetId).owned) return false;
            float x = Mathf.Round(rawPosition.x / SurfaceSandboxRules.Grid) * SurfaceSandboxRules.Grid;
            float z = Mathf.Round(rawPosition.z / SurfaceSandboxRules.Grid) * SurfaceSandboxRules.Grid;
            Vector3 position = SurfaceSandboxRules.Snap(new Vector3(x, 0, z), Ground(x, z), layer);
            rotation = (rotation % 4 + 4) % 4;
            if (!sandbox.CanPlace(PlanetId, kind, position, rotation)) { Message = "PLANET BUILDER / Footprint occupied or outside build region"; return false; }
            Vector2 cost = SurfaceSandboxRules.Cost(kind);
            if (!world.Empire.State.Spend(cost.x, cost.y)) { Message = $"PLANET BUILDER / Need {cost.x:0} minerals and {cost.y:0} energy"; return false; }
            var placed = sandbox.Place(PlanetId, kind, position, rotation);
            if (placed == null) { world.Empire.State.Reward(cost.x, cost.y); return false; }
            CreateBuildVisual(placed);
            Message = $"{kind.ToString().ToUpperInvariant()} PLACED / {LocalBuildCount} modules on {PlanetName}";
            world.Session?.SaveNow(); return true;
        }

        public bool TryRecycleBuild(Vector3 position, float range = 5)
        {
            if (!Active) return false;
            var build = sandbox.Closest(PlanetId, position, range);
            if (build == null) { Message = "PLANET BUILDER / No module in recycle range"; return false; }
            Vector2 cost = SurfaceSandboxRules.Cost(build.kind);
            if (!sandbox.Remove(build.id)) return false;
            if (buildVisuals.TryGetValue(build.id, out var visual) && visual != null) Destroy(visual);
            buildVisuals.Remove(build.id); world.Empire.State.Reward(cost.x * .5f, cost.y * .5f);
            Message = $"{build.kind.ToString().ToUpperInvariant()} RECYCLED / 50% materials returned";
            world.Session?.SaveNow(); return true;
        }

        void HandleBuildInput(Keyboard keys)
        {
            if (keys.digit1Key.wasPressedThisFrame) selectedBuild = SurfaceBuildKind.Foundation;
            if (keys.digit2Key.wasPressedThisFrame) selectedBuild = SurfaceBuildKind.Wall;
            if (keys.digit3Key.wasPressedThisFrame) selectedBuild = SurfaceBuildKind.Ramp;
            if (keys.digit4Key.wasPressedThisFrame) selectedBuild = SurfaceBuildKind.Habitat;
            if (keys.digit5Key.wasPressedThisFrame) selectedBuild = SurfaceBuildKind.Beacon;
            if (keys.digit6Key.wasPressedThisFrame) selectedBuild = SurfaceBuildKind.Road;
            if (keys.digit7Key.wasPressedThisFrame) selectedBuild = SurfaceBuildKind.Arcology;
            if (keys.digit8Key.wasPressedThisFrame) selectedBuild = SurfaceBuildKind.Industry;
            if (keys.digit9Key.wasPressedThisFrame) selectedBuild = SurfaceBuildKind.Spaceport;
            if (keys.qKey.wasPressedThisFrame) buildLayer = Mathf.Max(0, buildLayer - 1);
            if (keys.eKey.wasPressedThisFrame) buildLayer = Mathf.Min(5, buildLayer + 1);
            if (keys.rKey.wasPressedThisFrame) buildRotation = (buildRotation + 1) % 4;
            UpdateBuildCursor();
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) TryPlaceBuild(selectedBuild, BuildTarget(), buildRotation, buildLayer);
            if (mouse != null && mouse.rightButton.wasPressedThisFrame) TryRecycleBuild(BuildTarget(), 6);
        }

        void UpdateBuildCursor()
        {
            if (buildCursor == null || !buildMode) return;
            Vector3 target = BuildTarget();
            Vector3 size = selectedBuild switch
            {
                SurfaceBuildKind.Wall => new Vector3(4.2f, 3.6f, .35f),
                SurfaceBuildKind.Ramp => new Vector3(4.2f, .35f, 4.5f),
                SurfaceBuildKind.Habitat => new Vector3(5.8f, 3, 5.8f),
                SurfaceBuildKind.Beacon => new Vector3(1.5f, 4.4f, 1.5f),
                SurfaceBuildKind.Road => new Vector3(4.05f, .18f, 4.05f),
                SurfaceBuildKind.Arcology => new Vector3(7.2f, 12.5f, 7.2f),
                SurfaceBuildKind.Industry => new Vector3(8, 6.5f, 8),
                SurfaceBuildKind.Spaceport => new Vector3(12, .35f, 12),
                _ => new Vector3(4.2f, .35f, 4.2f)
            };
            float y = selectedBuild switch
            {
                SurfaceBuildKind.Wall => 1.8f,
                SurfaceBuildKind.Habitat => 1.5f,
                SurfaceBuildKind.Beacon => 2.2f,
                SurfaceBuildKind.Arcology => 6.25f,
                SurfaceBuildKind.Industry => 3.25f,
                _ => .25f
            };
            buildCursor.transform.localPosition = target + Vector3.up * y;
            buildCursor.transform.localScale = size;
            buildCursor.transform.localRotation = Quaternion.Euler(selectedBuild == SurfaceBuildKind.Ramp ? -32 : 0, buildRotation * 90, 0);
        }

        public bool StartRally()
        {
            if (!Active || rallyRoute == null) return false;
            float range = Vector2.Distance(new Vector2(explorerLocal.x, explorerLocal.z), rallyRoute[0]);
            if (range > 10) { Message = $"ROVER RALLY / Reach the cyan start gate ({range:0} m)"; return false; }
            if (buildMode) ToggleBuildMode();
            rallyActive = true; rallyElapsed = 0; rallyCheckpoint = 0;
            Message = "ROVER RALLY STARTED / Pass every gate in sequence"; UpdateRallyGates(); return true;
        }

        void UpdateRally(float dt)
        {
            if (!rallyActive || rallyRoute == null) return;
            rallyElapsed += dt;
            if (rallyElapsed > 600) { rallyActive = false; Message = "ROVER RALLY EXPIRED / Return to the start gate"; UpdateRallyGates(); return; }
            Vector2 roverPoint = new(explorerLocal.x, explorerLocal.z);
            if (Vector2.Distance(roverPoint, rallyRoute[rallyCheckpoint]) > 7) return;
            rallyCheckpoint++;
            if (rallyCheckpoint < rallyRoute.Length) { Message = $"GATE CLEARED / {rallyCheckpoint}/{rallyRoute.Length}"; UpdateRallyGates(); return; }
            rallyActive = false;
            int result = sandbox.CompleteRally(PlanetId, rallyElapsed);
            if (result == 2) { world.Empire.State.Reward(90, 55); Message = $"ROVER RALLY COMPLETE / {rallyElapsed:0.00}s · FIRST WIN +90 M +55 E"; }
            else if (result == 1) { world.Empire.State.Reward(30, 20); Message = $"NEW RALLY RECORD / {rallyElapsed:0.00}s · +30 M +20 E"; }
            else Message = $"ROVER RALLY COMPLETE / {rallyElapsed:0.00}s · Best {sandbox.Rally(PlanetId).bestTime:0.00}s";
            UpdateRallyGates(); world.Session?.SaveNow();
        }

        void Update()
        {
            if (world == null) return;
            if (!Active) { if (world.Player.LandPressed) TryLand(); return; }
            if (world.Player.Paused) return;
            if (!world.Combat.Player.Vitals.Alive) { Launch(); return; }
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.lKey.wasPressedThisFrame) { Launch(); return; }
            if (keys.bKey.wasPressedThisFrame) ToggleBuildMode();
            if (keys.tKey.wasPressedThisFrame) StartRally();
            if (keys.fKey.wasPressedThisFrame) TryCollect();
            if (keys.iKey.wasPressedThisFrame) { pingRemaining = 8; Message = NearestRelicDistance < 0 ? "NO REMAINING SIGNALS" : $"PULSE RETURN / Nearest relic {NearestRelicDistance:0} m"; }
            pingRemaining = Mathf.Max(0, pingRemaining - Time.deltaTime);
            Vector2 look = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            cameraYawOffset = Mathf.Clamp(cameraYawOffset + look.x * .11f, -80, 80);
            if (Mathf.Abs(look.x) < .01f) cameraYawOffset = Mathf.MoveTowards(cameraYawOffset, 0, 22 * Time.deltaTime);
            pitch = Mathf.Clamp(pitch - look.y * .11f + (keys.downArrowKey.isPressed ? 80 * Time.deltaTime : 0) - (keys.upArrowKey.isPressed ? 80 * Time.deltaTime : 0), -45, 65);
            if (buildMode)
            {
                HandleBuildInput(keys);
                rover = SurfaceRoverRules.Step(rover, 0, 0, false, Time.deltaTime);
                UpdateCamera(false); UpdateRally(Time.deltaTime); return;
            }
            float throttle = (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0);
            float steering = (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) -
                (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
            rover = SurfaceRoverRules.Step(rover, throttle, steering, keys.leftShiftKey.isPressed, Time.deltaTime);
            Vector3 forward = Quaternion.Euler(0, rover.heading, 0) * Vector3.forward;
            Vector3 proposed = explorerLocal + forward * rover.speed * Time.deltaTime;
            proposed.x = Mathf.Clamp(proposed.x, -SurfaceRules.HalfWidth + 3, SurfaceRules.HalfWidth - 3);
            proposed.z = Mathf.Clamp(proposed.z, -SurfaceRules.HalfWidth + 3, SurfaceRules.HalfWidth - 3);
            if (!SurfaceSandboxRules.BlocksRover(sandbox.Builds, PlanetId, new Vector2(proposed.x, proposed.z))) explorerLocal = proposed;
            else rover = new RoverMotion(rover.heading, 0);
            explorerLocal.y = DriveGround(explorerLocal.x, explorerLocal.z);
            UpdateCamera(false); UpdateRally(Time.deltaTime);
        }

        void UpdateCamera(bool immediate)
        {
            if (!Active || explorer == null) return;
            explorer.localPosition = explorerLocal;
            Vector3 forward = Quaternion.Euler(0, rover.heading, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, rover.heading, 0) * Vector3.right;
            float front = DriveGround(explorerLocal.x + forward.x * 2.2f, explorerLocal.z + forward.z * 2.2f);
            float back = DriveGround(explorerLocal.x - forward.x * 2.2f, explorerLocal.z - forward.z * 2.2f);
            float starboard = DriveGround(explorerLocal.x + right.x * 1.35f, explorerLocal.z + right.z * 1.35f);
            float port = DriveGround(explorerLocal.x - right.x * 1.35f, explorerLocal.z - right.z * 1.35f);
            Vector3 normal = Vector3.Cross(forward * 4.4f + Vector3.up * (front - back),
                right * 2.7f + Vector3.up * (starboard - port)).normalized;
            Quaternion attitude = Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, normal), normal);
            explorer.localRotation = immediate ? attitude : Quaternion.Slerp(explorer.localRotation, attitude, 1 - Mathf.Exp(-9 * Time.deltaTime));
            if (!immediate) wheelAngle += rover.speed * Time.deltaTime / .67f * Mathf.Rad2Deg;
            for (int i = 0; i < wheels.Length; i++) if (wheels[i] != null)
                wheels[i].localRotation = Quaternion.Euler(0, 0, 90) * Quaternion.Euler(0, wheelAngle, 0);
            bool afloat = RoverAfloat;
            foreach (var pontoon in pontoons) if (pontoon != null) pontoon.SetActive(afloat);
            Vector3 focus = sceneRoot.TransformPoint(explorerLocal + Vector3.up * 1.85f);
            Vector3 direction = Quaternion.Euler(pitch, rover.heading + cameraYawOffset, 0) * Vector3.forward;
            Vector3 desired = focus - direction * 10 + Vector3.up * 2.1f;
            lens.transform.position = immediate ? desired : Vector3.Lerp(lens.transform.position, desired, 1 - Mathf.Exp(-9 * Time.deltaTime));
            lens.transform.rotation = Quaternion.LookRotation(focus + direction * 3.5f - lens.transform.position, Vector3.up);
            lens.fieldOfView = 70;
        }

        Material Make(Color baseColor, float metallic = 0, Color? emission = null, float smoothness = -1)
        {
            var result = VisualFactory.Lit(baseColor, metallic, emission, smoothness);
            materials.Add(result);
            return result;
        }

        GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 size, Material material)
            => VisualFactory.Shape(name, type, parent, position, size, material);

        void BuildSite()
        {
            SurfaceBiome biome = Biome;
            int seed = SurfaceRules.StableSeed(planet.Definition.id, world.settings.galaxySeed);
            var random = new System.Random(seed);
            relicPositions = SurfaceRules.RelicPositions(planet.Definition.id, world.settings.galaxySeed);
            SurfacePalette palette = SurfacePalette.For(biome);
            Color soil = biome switch
            {
                SurfaceBiome.Ocean => new Color(.035f, .14f, .2f),
                SurfaceBiome.Desert => new Color(.48f, .29f, .13f),
                SurfaceBiome.Frozen => new Color(.53f, .69f, .76f),
                SurfaceBiome.Volcanic => new Color(.13f, .075f, .07f),
                SurfaceBiome.Rocky => new Color(.31f, .26f, .22f),
                SurfaceBiome.Toxic => new Color(.23f, .28f, .1f),
                _ => new Color(.2f, .32f, .23f)
            };
            var ground = Make(soil);
            var accent = Make(Color.Lerp(soil, biome == SurfaceBiome.Volcanic ? new Color(1, .13f, .035f) : new Color(.72f, .75f, .62f), .42f));
            var dark = Make(Color.Lerp(soil, Color.black, .42f), .16f);
            var bark = Make(new Color(.28f, .22f, .15f));
            var foliage = Make(Color.Lerp(palette.lowland, new Color(.18f, .4f, .13f), .52f));
            var foliageLight = Make(Color.Lerp(palette.lowland, new Color(.45f, .58f, .21f), .5f));
            var glow = Make(biome == SurfaceBiome.Volcanic ? new Color(1, .28f, .08f) : new Color(.13f, .9f, .85f), .1f,
                biome == SurfaceBiome.Volcanic ? new Color(2.5f, .2f, .04f) : new Color(.1f, 1.3f, 1.1f));
            BuildSky(palette, seed);
            BuildTerrain(palette, seed);
            BuildWater(palette, seed);
            if (biome == SurfaceBiome.Ocean)
            {
                for (int i = 0; i < 24; i++)
                {
                    Vector3 p = Scatter(random, 25, 140); p.y = Ground(p.x, p.z) - .18f;
                    var reef = Shape("Bioluminescent pelagic organism", PrimitiveType.Sphere, sceneRoot, p, new Vector3(2, .45f, 4), i % 4 == 0 ? glow : accent);
                    reef.transform.rotation = Quaternion.Euler(0, i * 57, 0);
                }
                BuildLandmarks(random, dark, accent, glow);
                Shape("Floating landing raft", PrimitiveType.Cube, sceneRoot, new Vector3(0, Ground(0, -28), -28), new Vector3(13, .45f, 14), dark);
            }
            else
            {
                for (int i = 0; i < 75; i++)
                {
                    Vector3 p = Scatter(random, 18, 143); p.y = Ground(p.x, p.z);
                    if (Terrain(p.x, p.z) < SurfaceRules.WaterLevel(biome) + .4f) continue;
                    bool nearRelic = false;
                    foreach (var relic in relicPositions) if (Vector2.Distance(new Vector2(p.x, p.z), relic) < 8) nearRelic = true;
                    if (nearRelic) continue;
                    if (biome == SurfaceBiome.Temperate) BuildPlant(p, random, bark, foliage, foliageLight);
                    else if (biome == SurfaceBiome.Toxic) BuildFungus(p, random, dark, accent, glow);
                    else if (biome == SurfaceBiome.Frozen) BuildSpire(p, random, accent, glow, true);
                    else if (biome == SurfaceBiome.Volcanic) BuildSpire(p, random, dark, glow, false);
                    else BuildSpire(p, random, dark, accent, false);
                }
                BuildLandmarks(random, dark, biome == SurfaceBiome.Temperate ? foliage : accent, glow);
            }
            if (biome == SurfaceBiome.Temperate || biome == SurfaceBiome.Toxic || biome == SurfaceBiome.Ocean) BuildFauna(random, dark, accent, glow);
            int faction = world.Empire.State.Get(planet.Definition.id).faction;
            if (faction > 0) BuildCivilization(random, faction, dark, accent, glow);
            BuildLander(dark, accent, glow);
            BuildRelics(seed, glow, dark);
        }

        void BuildPlayerStructures()
        {
            SurfacePalette palette = SurfacePalette.For(Biome);
            buildAlloy = Make(new Color(.13f, .17f, .2f), .8f, null, .72f);
            buildPanel = Make(Color.Lerp(palette.stone, new Color(.48f, .56f, .58f), .42f), .38f, null, .34f);
            buildGlass = Make(new Color(.05f, .32f, .42f), .2f, new Color(.05f, .75f, 1.15f), .88f);
            buildPreview = Make(new Color(.08f, .7f, .72f), .1f, new Color(.08f, 1.6f, 1.8f));
            buildConcrete = Make(Color.Lerp(palette.stone, new Color(.31f, .34f, .35f), .62f), .04f, null, .17f);
            buildRoad = Make(new Color(.065f, .073f, .078f), .05f, null, .08f);
            buildMarking = Make(new Color(.78f, .72f, .43f), .02f, null, .22f);
            buildHazard = Make(new Color(.88f, .34f, .055f), .25f, new Color(.32f, .055f, .008f));
            foreach (var build in sandbox.Builds) if (build.planet == PlanetId) CreateBuildVisual(build);
            buildCursor = Shape("Planet builder / placement cursor", PrimitiveType.Cube, sceneRoot, Vector3.zero, Vector3.one, buildPreview);
            buildCursor.SetActive(false);
        }

        GameObject CreateBuildVisual(SurfaceBuildSave build)
        {
            var root = new GameObject("Player build / " + build.kind + " / " + build.id);
            root.transform.SetParent(sceneRoot, false);
            root.transform.localPosition = build.position;
            root.transform.localRotation = Quaternion.Euler(0, build.rotation * 90, 0);
            switch (build.kind)
            {
                case SurfaceBuildKind.Foundation:
                    Shape("Foundation block", PrimitiveType.Cube, root.transform, Vector3.up * .32f, new Vector3(4, .64f, 4), buildPanel);
                    Shape("Foundation inset", PrimitiveType.Cube, root.transform, Vector3.up * .67f, new Vector3(3.5f, .08f, 3.5f), buildAlloy);
                    for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
                        Shape("Foundation light", PrimitiveType.Cube, root.transform, new Vector3(x * 1.72f, .69f, z * 1.72f), new Vector3(.24f, .1f, .24f), buildGlass);
                    break;
                case SurfaceBuildKind.Wall:
                    Shape("Wall panel", PrimitiveType.Cube, root.transform, new Vector3(0, 1.8f, 0), new Vector3(3.55f, 3.25f, .32f), buildPanel);
                    Shape("Wall window", PrimitiveType.Cube, root.transform, new Vector3(0, 2.05f, -.19f), new Vector3(2.25f, 1.25f, .06f), buildGlass);
                    for (int x = -1; x <= 1; x += 2)
                        Shape("Wall frame", PrimitiveType.Cube, root.transform, new Vector3(x * 1.86f, 1.8f, 0), new Vector3(.28f, 3.65f, .52f), buildAlloy);
                    Shape("Wall header", PrimitiveType.Cube, root.transform, new Vector3(0, 3.57f, 0), new Vector3(4, .24f, .52f), buildAlloy);
                    break;
                case SurfaceBuildKind.Ramp:
                    var ramp = Shape("Driveable ramp", PrimitiveType.Cube, root.transform, new Vector3(0, 1.55f, 0), new Vector3(4, .42f, 5), buildPanel);
                    ramp.transform.localRotation = Quaternion.Euler(-32, 0, 0);
                    for (int side = -1; side <= 1; side += 2)
                        Shape("Ramp rail", PrimitiveType.Cube, root.transform, new Vector3(side * 1.88f, 1.65f, 0), new Vector3(.16f, .25f, 5), buildGlass).transform.localRotation = Quaternion.Euler(-32, 0, 0);
                    break;
                case SurfaceBuildKind.Habitat:
                    Shape("Habitat base", PrimitiveType.Cylinder, root.transform, Vector3.up * .45f, new Vector3(3.25f, .45f, 3.25f), buildAlloy);
                    Shape("Habitat pressure dome", PrimitiveType.Sphere, root.transform, Vector3.up * 1.85f, new Vector3(5.4f, 3.1f, 5.4f), buildPanel);
                    Shape("Habitat panorama", PrimitiveType.Sphere, root.transform, new Vector3(0, 2.2f, 1.95f), new Vector3(3.5f, 1.35f, .28f), buildGlass);
                    Shape("Habitat airlock", PrimitiveType.Cube, root.transform, new Vector3(0, 1.05f, 2.75f), new Vector3(1.55f, 2.05f, 1.35f), buildAlloy);
                    VisualFactory.Ring("Habitat luminous band", root.transform, 2.72f, .12f, buildGlass, 72).transform.localPosition = Vector3.up * 1.65f;
                    break;
                case SurfaceBuildKind.Beacon:
                    Shape("Beacon footing", PrimitiveType.Cylinder, root.transform, Vector3.up * .25f, new Vector3(1.35f, .25f, 1.35f), buildAlloy);
                    Shape("Beacon mast", PrimitiveType.Cylinder, root.transform, Vector3.up * 2.15f, new Vector3(.24f, 2.1f, .24f), buildPanel);
                    Shape("Beacon core", PrimitiveType.Sphere, root.transform, Vector3.up * 4.3f, Vector3.one * .75f, buildGlass);
                    for (int i = 0; i < 2; i++)
                    {
                        var ring = VisualFactory.Ring("Beacon transmission ring", root.transform, 1.2f + i * .55f, .1f, buildGlass, 48);
                        ring.transform.localPosition = Vector3.up * (3.7f + i * .45f); ring.transform.localRotation = Quaternion.Euler(65 - i * 35, i * 45, 0);
                    }
                    break;
                case SurfaceBuildKind.Road:
                    BuildRoadTile(root.transform);
                    break;
                case SurfaceBuildKind.Arcology:
                    BuildArcology(root.transform);
                    break;
                case SurfaceBuildKind.Industry:
                    BuildIndustry(root.transform);
                    break;
                case SurfaceBuildKind.Spaceport:
                    BuildSpaceport(root.transform);
                    break;
            }
            buildVisuals[build.id] = root; return root;
        }

        void BuildRoadTile(Transform root)
        {
            Shape("Road / compacted subbase", PrimitiveType.Cube, root, new Vector3(0, .08f, 0), new Vector3(4, .16f, 4), buildConcrete);
            Shape("Road / sealed driving surface", PrimitiveType.Cube, root, new Vector3(0, .19f, 0), new Vector3(3.45f, .12f, 4.02f), buildRoad);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape("Road / raised curb", PrimitiveType.Cube, root, new Vector3(side * 1.86f, .29f, 0), new Vector3(.25f, .25f, 4), buildConcrete);
                for (int z = -1; z <= 1; z += 2)
                    Shape("Road / edge reflector", PrimitiveType.Cube, root, new Vector3(side * 1.68f, .29f, z * 1.38f), new Vector3(.13f, .07f, .22f), buildGlass);
            }
            for (int z = -1; z <= 1; z += 2)
                Shape("Road / center marking", PrimitiveType.Cube, root, new Vector3(0, .265f, z * 1.18f), new Vector3(.1f, .025f, 1.08f), buildMarking);
        }

        void BuildArcology(Transform root)
        {
            Shape("Arcology / reinforced foundation", PrimitiveType.Cube, root, new Vector3(0, .25f, 0), new Vector3(7.2f, .5f, 7.2f), buildConcrete);
            Shape("Arcology / public podium", PrimitiveType.Cylinder, root, new Vector3(0, .85f, 0), new Vector3(3.35f, .6f, 3.35f), buildAlloy);
            Shape("Arcology / inhabited tower", PrimitiveType.Cylinder, root, new Vector3(0, 5.9f, 0), new Vector3(2.45f, 4.55f, 2.45f), buildPanel);
            for (int floor = 0; floor < 7; floor++)
            {
                var band = VisualFactory.Ring("Arcology / illuminated floor", root, 2.5f, .09f, buildGlass, 72);
                band.transform.localPosition = Vector3.up * (2.2f + floor * 1.18f);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Shape("Arcology / structural buttress", PrimitiveType.Cube, root, new Vector3(side * 2.55f, 3.35f, 0), new Vector3(.38f, 5.5f, 1.1f), buildAlloy);
                Shape("Arcology / sky garden", PrimitiveType.Cylinder, root, new Vector3(side * 2.8f, 6.4f, 0), new Vector3(1.05f, .25f, 1.05f), buildConcrete);
                Shape("Arcology / garden canopy", PrimitiveType.Sphere, root, new Vector3(side * 2.8f, 6.82f, 0), new Vector3(1.4f, .6f, 1.4f), buildGlass);
            }
            Shape("Arcology / crown", PrimitiveType.Sphere, root, new Vector3(0, 10.65f, 0), new Vector3(4.5f, 1.15f, 4.5f), buildAlloy);
            Shape("Arcology / entry airlock", PrimitiveType.Cube, root, new Vector3(0, 1.15f, 3.25f), new Vector3(1.8f, 2.1f, .9f), buildGlass);
            BuildBeam("Arcology / communications spire", root, new Vector3(0, 11.15f, 0), new Vector3(0, 13.2f, 0), .1f, buildAlloy);
            Shape("Arcology / aviation beacon", PrimitiveType.Sphere, root, new Vector3(0, 13.28f, 0), Vector3.one * .24f, buildHazard);
        }

        void BuildIndustry(Transform root)
        {
            Shape("Industry / concrete apron", PrimitiveType.Cube, root, new Vector3(0, .2f, 0), new Vector3(8, .4f, 8), buildConcrete);
            Shape("Industry / fabrication hall", PrimitiveType.Cube, root, new Vector3(-.8f, 2.25f, .4f), new Vector3(5.9f, 4.1f, 5.6f), buildPanel);
            Shape("Industry / roof cap", PrimitiveType.Cube, root, new Vector3(-.8f, 4.42f, .4f), new Vector3(6.25f, .25f, 5.95f), buildAlloy);
            for (int i = -1; i <= 1; i++)
                Shape("Industry / clerestory window", PrimitiveType.Cube, root, new Vector3(i * 1.65f - .8f, 3.15f, 3.24f), new Vector3(1.15f, .65f, .08f), buildGlass);
            for (int tank = 0; tank < 2; tank++)
            {
                float z = -2.15f + tank * 3.9f;
                Shape("Industry / process tank", PrimitiveType.Cylinder, root, new Vector3(2.85f, 1.72f, z), new Vector3(.82f, 1.5f, .82f), buildAlloy);
                Shape("Industry / tank dome", PrimitiveType.Sphere, root, new Vector3(2.85f, 3.25f, z), new Vector3(1.65f, .55f, 1.65f), buildPanel);
                BuildBeam("Industry / transfer pipe", root, new Vector3(2.85f, 2.4f, z), new Vector3(1.5f, 2.4f, z), .11f, buildHazard);
            }
            for (int stack = -1; stack <= 1; stack += 2)
            {
                Shape("Industry / filtered exhaust", PrimitiveType.Cylinder, root, new Vector3(stack * 1.65f - .8f, 5.4f, -.9f), new Vector3(.32f, 1.4f, .32f), buildAlloy);
                Shape("Industry / exhaust status", PrimitiveType.Cylinder, root, new Vector3(stack * 1.65f - .8f, 6.78f, -.9f), new Vector3(.38f, .08f, .38f), buildHazard);
            }
            for (int row = 0; row < 2; row++) for (int column = 0; column < 3; column++)
                Shape("Industry / rooftop collector", PrimitiveType.Cube, root, new Vector3(-2.25f + column * 1.45f, 4.63f, -.8f + row * 1.55f), new Vector3(1.2f, .08f, 1.25f), buildGlass);
        }

        void BuildSpaceport(Transform root)
        {
            Shape("Spaceport / load-bearing slab", PrimitiveType.Cylinder, root, new Vector3(0, .16f, 0), new Vector3(6, .18f, 6), buildConcrete);
            Shape("Spaceport / landing surface", PrimitiveType.Cylinder, root, new Vector3(0, .36f, 0), new Vector3(5.65f, .08f, 5.65f), buildRoad);
            var perimeter = VisualFactory.Ring("Spaceport / perimeter guidance", root, 5.05f, .16f, buildMarking, 96);
            perimeter.transform.localPosition = Vector3.up * .47f;
            var inner = VisualFactory.Ring("Spaceport / touchdown ring", root, 2.45f, .12f, buildGlass, 72);
            inner.transform.localPosition = Vector3.up * .49f;
            for (int arm = 0; arm < 4; arm++)
            {
                float angle = arm * 90;
                Vector3 outward = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                var stripe = Shape("Spaceport / approach vector", PrimitiveType.Cube, root, outward * 3.75f + Vector3.up * .49f, new Vector3(.18f, .025f, 2.15f), buildMarking);
                stripe.transform.localRotation = Quaternion.Euler(0, angle, 0);
                Shape("Spaceport / landing beacon", PrimitiveType.Cylinder, root, outward * 5.2f + Vector3.up * .65f, new Vector3(.18f, .65f, .18f), buildAlloy);
                Shape("Spaceport / beacon lens", PrimitiveType.Sphere, root, outward * 5.2f + Vector3.up * 1.35f, Vector3.one * .24f, buildGlass);
            }
            Shape("Spaceport / service island", PrimitiveType.Cube, root, new Vector3(-4.4f, 1.05f, -1.4f), new Vector3(1.3f, 1.7f, 2.7f), buildAlloy);
            Shape("Spaceport / operations glass", PrimitiveType.Cube, root, new Vector3(-3.71f, 1.3f, -1.4f), new Vector3(.08f, .72f, 1.9f), buildGlass);
        }

        void BuildBeam(string name, Transform parent, Vector3 from, Vector3 to, float thickness, Material material)
        {
            var beam = Shape(name, PrimitiveType.Cylinder, parent, (from + to) * .5f,
                new Vector3(thickness, Vector3.Distance(from, to) * .5f, thickness), material);
            beam.transform.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
        }

        void BuildRallyCourse()
        {
            rallyRoute = SurfaceSandboxRules.RallyRoute(PlanetId, world.settings.galaxySeed);
            rallyIdle = Make(new Color(.17f, .2f, .23f), .7f, new Color(.1f, .18f, .22f));
            rallyCurrent = Make(new Color(.05f, .65f, .72f), .2f, new Color(.05f, 1.7f, 2.1f));
            for (int i = 0; i < rallyRoute.Length; i++)
            {
                Vector2 point = rallyRoute[i], next = rallyRoute[(i + 1) % rallyRoute.Length];
                Vector2 direction = (next - point).normalized;
                var gate = new GameObject("Rover rally gate / " + i);
                gate.transform.SetParent(sceneRoot, false);
                gate.transform.localPosition = new Vector3(point.x, Ground(point.x, point.y), point.y);
                gate.transform.localRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y));
                for (int side = -1; side <= 1; side += 2)
                {
                    Shape("Rally pylon", PrimitiveType.Cube, gate.transform, new Vector3(side * 3.1f, 2.3f, 0), new Vector3(.35f, 4.6f, .35f), rallyIdle);
                    Shape("Rally foot", PrimitiveType.Cylinder, gate.transform, new Vector3(side * 3.1f, .22f, 0), new Vector3(.72f, .22f, .72f), buildAlloy);
                }
                Shape("Rally header", PrimitiveType.Cube, gate.transform, new Vector3(0, 4.55f, 0), new Vector3(6.55f, .35f, .35f), rallyIdle);
                VisualFactory.Ring("Rally marker", gate.transform, .75f, .11f, rallyIdle, 40).transform.localPosition = Vector3.up * 5.2f;
                rallyGates.Add(gate);
            }
            UpdateRallyGates();
        }

        void UpdateRallyGates()
        {
            for (int i = 0; i < rallyGates.Count; i++)
            {
                bool highlighted = rallyActive ? i == rallyCheckpoint : !rallyActive && i == 0;
                foreach (var renderer in rallyGates[i].GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = highlighted ? rallyCurrent : rallyIdle;
            }
        }

        Vector3 Scatter(System.Random random, float min, float max)
        {
            float angle = (float)random.NextDouble() * Mathf.PI * 2;
            float radius = min + (float)random.NextDouble() * (max - min);
            return new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
        }

        void BuildSky(SurfacePalette palette, int seed)
        {
            Shader shader = Shader.Find("Orbit Dominion/SurfaceSky");
            if (shader == null) return;
            var material = new Material(shader);
            material.SetColor("_Horizon", palette.horizon);
            material.SetColor("_Zenith", palette.zenith);
            material.SetColor("_Cloud", palette.cloud);
            material.SetFloat("_Seed", (seed & 65535) * .001f);
            material.SetFloat("_CloudAmount", Biome == SurfaceBiome.Volcanic ? .42f : Biome == SurfaceBiome.Desert ? .16f : .32f);
            materials.Add(material);
            var dome = Shape("Atmosphere and cloud layers", PrimitiveType.Sphere, sceneRoot, Vector3.zero, Vector3.one * 1750, material);
            var collider = dome.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }

        void BuildTerrain(SurfacePalette palette, int seed)
        {
            const int cells = 150;
            float step = SurfaceRules.HalfWidth * 2f / cells;
            var vertices = new Vector3[(cells + 1) * (cells + 1)];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[cells * cells * 6];
            for (int z = 0; z <= cells; z++) for (int x = 0; x <= cells; x++)
            {
                int index = z * (cells + 1) + x;
                float px = -SurfaceRules.HalfWidth + x * step, pz = -SurfaceRules.HalfWidth + z * step;
                float height = SurfaceRules.Height(Biome, seed, px, pz);
                vertices[index] = new Vector3(px, height, pz);
                uv[index] = new Vector2(px, pz);
                float slope = Mathf.Abs(SurfaceRules.Height(Biome, seed, px + 1, pz) - height) +
                    Mathf.Abs(SurfaceRules.Height(Biome, seed, px, pz + 1) - height);
                float noise = Mathf.PerlinNoise(px * .055f + seed * .001f, pz * .055f) * .1f;
                Color tint = Color.Lerp(palette.lowland, palette.highland, Mathf.Clamp01((height - 3) / 14f + noise));
                tint = Color.Lerp(tint, palette.stone, Mathf.Clamp01((slope - .38f) * 1.25f));
                tint = Color.Lerp(tint, palette.shore, 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((height - SurfaceRules.WaterLevel(Biome) - .1f) / 3.4f)));
                colors[index] = tint;
            }
            for (int z = 0; z < cells; z++) for (int x = 0; x < cells; x++)
            {
                int t = (z * cells + x) * 6, a = z * (cells + 1) + x, b = a + cells + 1;
                triangles[t] = a; triangles[t + 1] = b; triangles[t + 2] = a + 1;
                triangles[t + 3] = a + 1; triangles[t + 4] = b; triangles[t + 5] = b + 1;
            }
            var mesh = new Mesh { name = "Deterministic surface / " + planet.Definition.id, vertices = vertices, triangles = triangles, uv = uv, colors = colors };
            mesh.RecalculateNormals();
            var objectRoot = new GameObject("Traversable landscape");
            objectRoot.transform.SetParent(sceneRoot, false);
            objectRoot.AddComponent<MeshFilter>().sharedMesh = mesh;
            Shader shader = Shader.Find("Orbit Dominion/SurfaceTerrain");
            Material material = shader != null ? new Material(shader) : Make(palette.lowland);
            if (shader != null)
            {
                material.SetFloat("_Seed", (seed & 65535) * .001f);
                material.SetFloat("_WaterLevel", SurfaceRules.WaterLevel(Biome));
                material.SetFloat("_Wetness", Biome == SurfaceBiome.Desert ? .08f : Biome == SurfaceBiome.Frozen ? .24f : Biome == SurfaceBiome.Volcanic ? .14f : .68f);
                materials.Add(material);
            }
            objectRoot.AddComponent<MeshRenderer>().sharedMaterial = material;
            objectRoot.AddComponent<OwnedMesh>().mesh = mesh;
        }

        void BuildWater(SurfacePalette palette, int seed)
        {
            bool ocean = Biome == SurfaceBiome.Ocean;
            const int cells = 160;
            float width = 700;
            float step = width * 2 / cells;
            float level = SurfaceRules.WaterLevel(Biome);
            var vertices = new Vector3[(cells + 1) * (cells + 1)];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[cells * cells * 6];
            for (int z = 0; z <= cells; z++) for (int x = 0; x <= cells; x++)
            {
                int index = z * (cells + 1) + x;
                float px = -width + x * step, pz = -width + z * step;
                vertices[index] = new Vector3(px, level, pz);
                uv[index] = new Vector2(px, pz);
                colors[index] = new Color(1, 1, 1, Mathf.Clamp01((SurfaceRules.Height(Biome, seed, px, pz) - level + 3.5f) / 5f));
            }
            for (int z = 0; z < cells; z++) for (int x = 0; x < cells; x++)
            {
                int t = (z * cells + x) * 6, a = z * (cells + 1) + x, b = a + cells + 1;
                triangles[t] = a; triangles[t + 1] = b; triangles[t + 2] = a + 1;
                triangles[t + 3] = a + 1; triangles[t + 4] = b; triangles[t + 5] = b + 1;
            }
            var mesh = new Mesh { name = "Surface water / " + planet.Definition.id, vertices = vertices, triangles = triangles, uv = uv, colors = colors };
            mesh.RecalculateNormals();
            var water = new GameObject(ocean ? "Open ocean" : "Coastal water");
            water.transform.SetParent(sceneRoot, false);
            water.AddComponent<MeshFilter>().sharedMesh = mesh;
            Shader shader = Shader.Find("Orbit Dominion/SurfaceWater");
            Material material = shader != null ? new Material(shader) : Make(palette.water);
            if (shader != null)
            {
                material.SetColor("_Deep", palette.deepWater);
                material.SetColor("_Shallow", palette.water);
                material.SetFloat("_Seed", (seed & 65535) * .001f);
                material.SetFloat("_WaveHeight", Biome == SurfaceBiome.Frozen ? .025f : Biome == SurfaceBiome.Volcanic ? .07f : .17f);
                materials.Add(material);
            }
            water.AddComponent<MeshRenderer>().sharedMaterial = material;
            water.AddComponent<OwnedMesh>().mesh = mesh;
        }

        void BuildPlant(Vector3 p, System.Random random, Material trunk, Material leaf, Material light)
        {
            float height = 4 + (float)random.NextDouble() * 5;
            Shape("Grove / trunk", PrimitiveType.Cylinder, sceneRoot, p + Vector3.up * height * .5f, new Vector3(.36f, height * .5f, .36f), trunk);
            for (int j = 0; j < 5; j++)
            {
                float angle = j * Mathf.PI * 2 / 5 + (float)random.NextDouble() * .35f;
                float reach = 1.2f + (float)random.NextDouble() * 1.1f;
                Vector3 crown = p + new Vector3(Mathf.Cos(angle) * reach, height + (float)random.NextDouble() * 1.6f, Mathf.Sin(angle) * reach);
                Shape("Grove / leafy crown", PrimitiveType.Sphere, sceneRoot, crown,
                    new Vector3(2.5f + reach * .5f, 2.2f + reach * .3f, 2.7f + reach * .5f), j % 3 == 0 ? light : leaf);
            }
        }

        void BuildFungus(Vector3 p, System.Random random, Material stem, Material cap, Material light)
        {
            float height = 2 + (float)random.NextDouble() * 5;
            Shape("Alien mycelium / stem", PrimitiveType.Cylinder, sceneRoot, p + Vector3.up * height * .5f, new Vector3(.55f, height * .5f, .55f), stem);
            Shape("Alien mycelium / cap", PrimitiveType.Sphere, sceneRoot, p + Vector3.up * height, new Vector3(4, .7f, 4), random.NextDouble() > .65 ? light : cap);
        }

        void BuildSpire(Vector3 p, System.Random random, Material baseMaterial, Material tipMaterial, bool crystalline)
        {
            float height = 2 + (float)random.NextDouble() * (crystalline ? 9 : 13);
            var pillar = Shape(crystalline ? "Ice needle" : "Eroded mineral spire", PrimitiveType.Cylinder, sceneRoot,
                p + Vector3.up * height * .5f, new Vector3(1.2f, height * .5f, 1.2f), baseMaterial);
            pillar.transform.rotation = Quaternion.Euler((float)random.NextDouble() * 22 - 11, (float)random.NextDouble() * 180, (float)random.NextDouble() * 22 - 11);
            Shape("Crystal crown", PrimitiveType.Sphere, sceneRoot, p + Vector3.up * height, new Vector3(1.4f, 1.9f, 1.4f), tipMaterial);
        }

        void BuildLandmarks(System.Random random, Material dark, Material accent, Material glow)
        {
            SurfaceBiome biome = Biome;
            Material foam = biome == SurfaceBiome.Ocean ? Make(new Color(.37f, .72f, .77f), .25f) : null;
            int count = biome == SurfaceBiome.Ocean ? 40 : biome == SurfaceBiome.Temperate ? 130 : biome == SurfaceBiome.Toxic ? 95 : 20;
            for (int i = 0; i < count; i++)
            {
                Vector3 p = Scatter(random, biome == SurfaceBiome.Ocean ? 22 : 13,
                    biome == SurfaceBiome.Ocean ? 175 : biome == SurfaceBiome.Temperate || biome == SurfaceBiome.Toxic ? 92 : 140);
                p.y = Ground(p.x, p.z);
                if (biome != SurfaceBiome.Ocean && Terrain(p.x, p.z) < SurfaceRules.WaterLevel(biome) + .15f) continue;
                bool nearRelic = false;
                foreach (var relic in relicPositions) if (Vector2.Distance(new Vector2(p.x, p.z), relic) < 6) nearRelic = true;
                if (nearRelic) continue;
                if (biome == SurfaceBiome.Ocean)
                {
                    var wave = Shape("Foam on open water", PrimitiveType.Cube, sceneRoot, new Vector3(p.x, 1.22f, p.z),
                        new Vector3(3 + (float)random.NextDouble() * 9, .06f, .18f), foam);
                    wave.transform.rotation = Quaternion.Euler(0, i * 41, 0);
                    if (i % 8 == 0)
                    {
                        Shape("Leviathan back", PrimitiveType.Sphere, sceneRoot, new Vector3(p.x, .95f, p.z), new Vector3(9, 2, 3.5f), dark);
                        Shape("Leviathan luminous crest", PrimitiveType.Sphere, sceneRoot, new Vector3(p.x, 1.8f, p.z), new Vector3(6, .25f, .3f), glow);
                    }
                }
                else if (biome == SurfaceBiome.Volcanic)
                {
                    var flow = Shape("Lava fissure", PrimitiveType.Cube, sceneRoot, p + Vector3.up * .13f,
                        new Vector3(2 + (float)random.NextDouble() * 8, .1f, .35f), glow);
                    flow.transform.rotation = Quaternion.Euler(0, i * 71, 0);
                }
                else if (biome == SurfaceBiome.Rocky)
                {
                    var boulder = Shape("Fractured outcrop", PrimitiveType.Cube, sceneRoot, p + Vector3.up * 1.1f,
                        new Vector3(3, 2.3f, 4.5f), i % 3 == 0 ? accent : dark);
                    boulder.transform.rotation = Quaternion.Euler(i * 17, i * 47, i * 13);
                }
                else if (biome == SurfaceBiome.Desert)
                {
                    if (i % 4 == 0)
                    {
                        Shape("Weathered gate / left", PrimitiveType.Cube, sceneRoot, p + new Vector3(-2, 3, 0), new Vector3(1.2f, 6, 1.5f), dark);
                        Shape("Weathered gate / right", PrimitiveType.Cube, sceneRoot, p + new Vector3(2, 3, 0), new Vector3(1.2f, 6, 1.5f), dark);
                        Shape("Weathered gate / lintel", PrimitiveType.Cube, sceneRoot, p + new Vector3(0, 6, 0), new Vector3(6, 1.2f, 1.5f), accent);
                    }
                }
                else if (biome == SurfaceBiome.Frozen)
                {
                    var shard = Shape("Glacial shard", PrimitiveType.Cube, sceneRoot, p + Vector3.up * 2.5f,
                        new Vector3(1.8f, 5, 3), i % 3 == 0 ? glow : accent);
                    shard.transform.rotation = Quaternion.Euler(15, i * 47, 28);
                }
                else
                {
                    if (biome == SurfaceBiome.Toxic)
                        Shape("Spore mat", PrimitiveType.Sphere, sceneRoot, p + Vector3.up * .18f,
                            new Vector3(1.4f + (i % 3), .25f, 1.1f + (i % 4)), i % 7 == 0 ? glow : accent);
                    else for (int blade = 0; blade < 3; blade++)
                    {
                        var grass = Shape("Meadow grass", PrimitiveType.Capsule, sceneRoot,
                            p + new Vector3((blade - 1) * .36f, .42f, (blade % 2) * .23f),
                            new Vector3(.13f, .46f + (float)random.NextDouble() * .25f, .13f), accent);
                        grass.transform.rotation = Quaternion.Euler(0, i * 31, (blade - 1) * 19);
                    }
                }
            }
        }

        void BuildFauna(System.Random random, Material body, Material accent, Material eye)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector3 p = Scatter(random, 20, 125); p.y = Ground(p.x, p.z) + (Biome == SurfaceBiome.Ocean ? .7f : 1.5f);
                var creature = new GameObject(Biome == SurfaceBiome.Ocean ? "Pelagic glider" : "Native grazing life").transform;
                creature.SetParent(sceneRoot, false); creature.localPosition = p;
                creature.gameObject.AddComponent<SurfaceWanderer>().Initialize(p, i * 31, Biome == SurfaceBiome.Ocean ? .55f : .24f);
                Shape("Body", PrimitiveType.Sphere, creature, Vector3.zero, new Vector3(2.2f, 1.1f, 1.5f), body);
                Shape("Carapace", PrimitiveType.Sphere, creature, new Vector3(0, .55f, -.25f), new Vector3(1.2f, .6f, 1), accent);
                for (int side = -1; side <= 1; side += 2)
                {
                    Shape("Sensory eye", PrimitiveType.Sphere, creature, new Vector3(side * .65f, .35f, .75f), Vector3.one * .2f, eye);
                    if (Biome != SurfaceBiome.Ocean) for (int leg = -1; leg <= 1; leg += 2)
                        Shape("Jointed limb", PrimitiveType.Cylinder, creature, new Vector3(side * .7f, -.7f, leg * .45f), new Vector3(.15f, .9f, .15f), body);
                    else Shape("Gliding fin", PrimitiveType.Cube, creature, new Vector3(side * 1.6f, 0, -.2f), new Vector3(2.2f, .15f, .8f), accent);
                }
            }
        }

        void BuildCivilization(System.Random random, int faction, Material structure, Material accent, Material light)
        {
            Vector3 center = new(62, Ground(62, 45), 45);
            if (Biome == SurfaceBiome.Ocean) center.y = 1.5f;
            Shape("Civilization / landing platform", PrimitiveType.Cylinder, sceneRoot, center + Vector3.up * .3f, new Vector3(19, .4f, 19), structure);
            Shape("Civilization / central dome", PrimitiveType.Sphere, sceneRoot, center + Vector3.up * 2.7f, new Vector3(8, 4.8f, 8), accent);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2 / 9;
                Vector3 p = center + new Vector3(Mathf.Cos(a) * 13, 0, Mathf.Sin(a) * 13);
                p.y = Ground(p.x, p.z);
                BuildSurfaceLink("Civilization / radial transit road", center + Vector3.up * .36f, p + Vector3.up * .18f, 2.2f, structure);
                float height = 4 + (float)random.NextDouble() * 4;
                Shape("Occupied habitat", PrimitiveType.Cylinder, sceneRoot, p + Vector3.up * (height * .5f + .4f), new Vector3(2.4f, height * .5f, 2.4f), structure);
                Shape("Habitat roof", PrimitiveType.Sphere, sceneRoot, p + Vector3.up * (height + .45f), new Vector3(5.2f, 1.3f, 5.2f), accent);
                for (int w = 0; w < 4; w++)
                {
                    float wa = w * Mathf.PI * .5f;
                    Shape("Lit habitat window", PrimitiveType.Sphere, sceneRoot,
                        p + new Vector3(Mathf.Sin(wa) * 2.37f, height * .58f, Mathf.Cos(wa) * 2.37f),
                        new Vector3(.65f, .52f, .22f), light);
                }
                Vector3 lamp = Vector3.Lerp(center, p, .63f); lamp.y = Ground(lamp.x, lamp.z);
                BuildBeam("Civilization / street light mast", sceneRoot, lamp + Vector3.up * .15f, lamp + Vector3.up * 2.6f, .07f, structure);
                Shape("Civilization / street light", PrimitiveType.Sphere, sceneRoot, lamp + Vector3.up * 2.7f, Vector3.one * .25f, light);
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2 / 6;
                Vector3 p = center + new Vector3(Mathf.Sin(a) * 9, .9f, Mathf.Cos(a) * 9);
                var citizen = new GameObject(faction == 2 ? "Ithacan citizen" : faction == 3 ? "Dominion colonist" : "Union explorer").transform;
                citizen.SetParent(sceneRoot, false); citizen.localPosition = p;
                citizen.gameObject.AddComponent<SurfaceWanderer>().Initialize(p, i * 77, .12f);
                Shape("Suit", PrimitiveType.Capsule, citizen, new Vector3(0, .9f, 0), new Vector3(.6f, 1, .45f), accent);
                Shape("Visor", PrimitiveType.Sphere, citizen, new Vector3(0, 1.9f, 0), Vector3.one * .58f, light);
            }
        }

        void BuildSurfaceLink(string name, Vector3 from, Vector3 to, float width, Material material)
        {
            Vector3 delta = to - from;
            var road = Shape(name, PrimitiveType.Cube, sceneRoot, (from + to) * .5f,
                new Vector3(width, .12f, delta.magnitude), material);
            road.transform.localRotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }

        void BuildLander(Material hull, Material accent, Material glow)
        {
            Vector3 p = new(0, Ground(0, -28) + (Biome == SurfaceBiome.Ocean ? 1.8f : 2.8f), -28);
            var lander = new GameObject("Wayfarer landing craft").transform;
            lander.SetParent(sceneRoot, false); lander.localPosition = p;
            VisualFactory.Ship(lander, hull, accent, 2.1f);
            for (int side = -1; side <= 1; side += 2)
                Shape("Landing thruster", PrimitiveType.Cylinder, lander, new Vector3(side * 5, -2.2f, -3), new Vector3(.6f, 2, .6f), glow);
        }

        void RoverStrut(string name, Vector3 from, Vector3 to, float thickness, Material material)
        {
            var strut = Shape(name, PrimitiveType.Cylinder, explorer, (from + to) * .5f,
                new Vector3(thickness, Vector3.Distance(from, to) * .5f, thickness), material);
            strut.transform.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
        }

        void BuildRover()
        {
            var hull = Make(new Color(.65f, .7f, .66f), .28f);
            var frame = Make(new Color(.16f, .24f, .27f), .65f);
            var tire = Make(new Color(.045f, .055f, .062f));
            var solar = Make(new Color(.035f, .2f, .34f), .55f);
            var glass = Make(new Color(.1f, .36f, .44f), .45f);
            var ochre = Make(new Color(.8f, .37f, .12f), .25f);
            var beacon = Make(new Color(.45f, .85f, .96f), .15f, new Color(.15f, 1.1f, 1.4f));

            Shape("Rover / armored undercarriage", PrimitiveType.Cube, explorer, new Vector3(0, 1.19f, 0), new Vector3(2.65f, .38f, 4.25f), frame);
            Shape("Rover / white chassis", PrimitiveType.Cube, explorer, new Vector3(0, 1.55f, 0), new Vector3(2.8f, .4f, 4.35f), hull);
            Shape("Rover / front bumper", PrimitiveType.Cube, explorer, new Vector3(0, 1.08f, 2.2f), new Vector3(3.1f, .24f, .28f), ochre);
            Shape("Rover / pressure cabin", PrimitiveType.Cube, explorer, new Vector3(0, 2.21f, -.45f), new Vector3(2.05f, 1.05f, 2.55f), hull);
            Shape("Rover / panoramic windscreen", PrimitiveType.Cube, explorer, new Vector3(0, 2.38f, .87f), new Vector3(1.88f, .68f, .1f), glass);
            Shape("Rover / roof", PrimitiveType.Cube, explorer, new Vector3(0, 2.83f, -.45f), new Vector3(2.27f, .16f, 2.8f), ochre);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape("Rover / solar wing", PrimitiveType.Cube, explorer,
                    new Vector3(side * 2.12f, 1.9f, -.42f), new Vector3(1.48f, .13f, 2.78f), frame);
                for (int row = 0; row < 4; row++) for (int column = 0; column < 2; column++)
                    Shape("Rover / photovoltaic cell", PrimitiveType.Cube, explorer,
                        new Vector3(side * (1.75f + column * .72f), 1.99f, -.42f + (row - 1.5f) * .62f),
                        new Vector3(.56f, .04f, .54f), solar);
                RoverStrut("Rover / wing support", new Vector3(side * 1.15f, 1.45f, -.5f),
                    new Vector3(side * 2.1f, 1.86f, -.5f), .12f, frame);
                Shape("Rover / side stripe", PrimitiveType.Cube, explorer, new Vector3(side * 1.41f, 1.64f, .6f),
                    new Vector3(.035f, .11f, 2.35f), ochre);
                for (int axle = 0; axle < 3; axle++)
                {
                    float z = (axle - 1) * 1.64f;
                    int index = (side < 0 ? 0 : 3) + axle;
                    var wheel = Shape("Rover wheel / " + index, PrimitiveType.Cylinder, explorer,
                        new Vector3(side * 1.63f, .68f, z), new Vector3(1.34f, .28f, 1.34f), tire);
                    wheels[index] = wheel.transform;
                    Shape("Wheel hub", PrimitiveType.Cylinder, wheel.transform, Vector3.zero,
                        new Vector3(.48f, 1.08f, .48f), ochre);
                    RoverStrut("Rover / rocker arm", new Vector3(side * 1.05f, 1.24f, z * .65f),
                        new Vector3(side * 1.62f, .7f, z), .16f, frame);
                }
                pontoons[side < 0 ? 0 : 1] = Shape("Rover flotation pontoon", PrimitiveType.Capsule, explorer,
                    new Vector3(side * 1.42f, .34f, 0), new Vector3(.7f, 2.2f, .7f), hull);
                pontoons[side < 0 ? 0 : 1].transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            RoverStrut("Rover / sensor mast", new Vector3(0, 2.85f, .7f), new Vector3(0, 3.84f, 1.15f), .13f, frame);
            Shape("Rover / stereo camera", PrimitiveType.Cube, explorer, new Vector3(0, 3.88f, 1.17f),
                new Vector3(1.22f, .36f, .42f), hull);
            for (int side = -1; side <= 1; side += 2)
                Shape("Rover / optical sensor", PrimitiveType.Sphere, explorer, new Vector3(side * .42f, 3.89f, 1.4f),
                    new Vector3(.22f, .2f, .12f), beacon);
            RoverStrut("Rover / antenna", new Vector3(-.7f, 2.9f, -1.28f), new Vector3(-1.12f, 4.2f, -1.48f), .05f, frame);
            Shape("Rover / radio beacon", PrimitiveType.Sphere, explorer, new Vector3(-1.12f, 4.2f, -1.48f), Vector3.one * .19f, beacon);
            RoverStrut("Rover / manipulator", new Vector3(.5f, 1.56f, 1.6f), new Vector3(.7f, 1.1f, 2.73f), .12f, frame);
            Shape("Rover / relic gripper", PrimitiveType.Cube, explorer, new Vector3(.7f, 1.08f, 2.79f),
                new Vector3(.48f, .14f, .3f), ochre);
        }

        void BuildRelics(int seed, Material emissive, Material plinth)
        {
            for (int i = 0; i < relicPositions.Length; i++)
            {
                Vector2 point = relicPositions[i];
                var site = new GameObject("Hidden relic / " + i);
                site.transform.SetParent(sceneRoot, false);
                site.transform.localPosition = new Vector3(point.x, Ground(point.x, point.y) + .7f, point.y);
                Shape("Ancient plinth", PrimitiveType.Cylinder, site.transform, Vector3.zero, new Vector3(1.7f, .35f, 1.7f), plinth);
                var crystal = Shape("Resonant artifact", PrimitiveType.Cube, site.transform, Vector3.up * 1.9f, new Vector3(.8f, 2.2f, .8f), emissive);
                crystal.transform.localRotation = Quaternion.Euler(25, i * 37, 25);
                VisualFactory.Ring("Signal halo", site.transform, 1.8f, .12f, emissive, 36).transform.localPosition = Vector3.up * 1.5f;
                site.SetActive(!collected.Contains(RelicKey(i)));
                relicVisuals.Add(site);
            }
        }

        void OnDestroy()
        {
            foreach (var material in materials) if (material != null) Destroy(material);
            materials.Clear();
            if (Active && lens != null)
            {
                RenderSettings.fog = previousFog;
                RenderSettings.fogMode = previousFogMode;
                RenderSettings.fogColor = previousFogColor;
                RenderSettings.fogDensity = previousFogDensity;
            }
        }
    }

    // A shared palette keeps land, shoreline, water and atmosphere coherent per world.
    readonly struct SurfacePalette
    {
        public readonly Color lowland, highland, stone, shore, water, deepWater, horizon, zenith, cloud;
        SurfacePalette(Color lowland, Color highland, Color stone, Color shore, Color water, Color deepWater, Color horizon, Color zenith, Color cloud)
        { this.lowland = lowland; this.highland = highland; this.stone = stone; this.shore = shore; this.water = water; this.deepWater = deepWater; this.horizon = horizon; this.zenith = zenith; this.cloud = cloud; }

        public static SurfacePalette For(SurfaceBiome biome) => biome switch
        {
            SurfaceBiome.Ocean => new(new(.35f,.48f,.27f),new(.48f,.5f,.35f),new(.29f,.35f,.32f),new(.78f,.75f,.55f),new(.08f,.42f,.47f),new(.015f,.12f,.26f),new(.53f,.73f,.78f),new(.12f,.36f,.65f),new(.92f,.94f,.88f)),
            SurfaceBiome.Desert => new(new(.57f,.39f,.2f),new(.69f,.49f,.25f),new(.4f,.32f,.23f),new(.78f,.61f,.38f),new(.14f,.39f,.41f),new(.02f,.14f,.27f),new(.75f,.57f,.39f),new(.26f,.36f,.54f),new(.96f,.78f,.58f)),
            SurfaceBiome.Frozen => new(new(.56f,.67f,.66f),new(.8f,.86f,.84f),new(.36f,.47f,.54f),new(.7f,.81f,.83f),new(.29f,.59f,.68f),new(.07f,.28f,.42f),new(.59f,.75f,.82f),new(.2f,.4f,.62f),new(.94f,.97f,.98f)),
            SurfaceBiome.Volcanic => new(new(.29f,.21f,.18f),new(.42f,.32f,.28f),new(.18f,.15f,.16f),new(.34f,.27f,.22f),new(.64f,.2f,.07f),new(.21f,.045f,.03f),new(.53f,.37f,.29f),new(.22f,.19f,.27f),new(.7f,.48f,.37f)),
            SurfaceBiome.Rocky => new(new(.36f,.39f,.29f),new(.48f,.49f,.4f),new(.31f,.33f,.34f),new(.62f,.59f,.46f),new(.11f,.38f,.47f),new(.025f,.13f,.26f),new(.59f,.67f,.69f),new(.24f,.4f,.58f),new(.86f,.86f,.82f)),
            SurfaceBiome.Toxic => new(new(.34f,.41f,.21f),new(.49f,.5f,.26f),new(.26f,.31f,.23f),new(.63f,.59f,.36f),new(.21f,.42f,.31f),new(.045f,.15f,.14f),new(.59f,.67f,.45f),new(.26f,.42f,.43f),new(.86f,.87f,.64f)),
            _ => new(new(.23f,.43f,.23f),new(.43f,.5f,.3f),new(.37f,.4f,.37f),new(.76f,.7f,.48f),new(.11f,.46f,.54f),new(.025f,.16f,.32f),new(.6f,.75f,.78f),new(.16f,.39f,.67f),new(.93f,.95f,.91f))
        };
    }

    public sealed class SurfaceWanderer : MonoBehaviour
    {
        Vector3 center; float phase, speed;
        public void Initialize(Vector3 origin, float angle, float movementSpeed) { center = origin; phase = angle; speed = movementSpeed; }
        void Update()
        {
            if (Time.timeScale == 0) return;
            phase += Time.deltaTime * speed;
            transform.localPosition = center + new Vector3(Mathf.Sin(phase) * 1.6f, Mathf.Sin(phase * 2) * .13f, Mathf.Cos(phase) * 1.6f);
            transform.localRotation = Quaternion.Euler(0, phase * Mathf.Rad2Deg + 90, 0);
        }
    }
}
