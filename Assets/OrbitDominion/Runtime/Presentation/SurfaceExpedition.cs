using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OrbitDominion
{
    [DefaultExecutionOrder(60)]
    public sealed partial class SurfaceExpedition : MonoBehaviour
    {
        static readonly Vector3 SurfaceOrigin = new(0, -80000, 0);
        readonly HashSet<string> collected = new();
        readonly List<Material> materials = new();
        readonly List<GameObject> relicVisuals = new();
        readonly Dictionary<string, GameObject> buildVisuals = new();
        readonly List<GameObject> rallyGates = new();
        readonly List<(Transform root, float radius, float height)> scenery = new();
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
        Material buildAlloy, buildPanel, buildGlass, buildPreview, buildConcrete, buildRoad, buildMarking, buildHazard, foliageCard, rallyIdle, rallyCurrent;
        bool previousFog;
        FogMode previousFogMode;
        Color previousFogColor;
        float previousFogDensity, previousFarClip;
        UnityEngine.Rendering.AmbientMode previousAmbientMode;
        UnityEngine.Rendering.SphericalHarmonicsL2 previousAmbientProbe;
        Color previousAmbientSky,previousAmbientEquator,previousAmbientGround;
        float previousAmbientIntensity;
        CameraClearFlags previousClearFlags;
        Color previousBackground;

        public bool Active => planet != null;
        public string PlanetId => planet?.Definition.id;
        public string PlanetName => planet?.Definition.title;
        public SurfaceBiome Biome => planet == null ? SurfaceBiome.Temperate : SurfaceRules.Biome(planet.Definition);
        public Vector3 ExplorerLocal => explorerLocal;
        public float RoverSpeed => rover.speed;
        public float RoverHeading => rover.heading;
        float ExplorerHeading => OnFoot ? walk.heading : rover.heading;
        public bool RoverAfloat => Active && Terrain(ParkedRoverLocal.x, ParkedRoverLocal.z) < SurfaceRules.WaterLevel(Biome) + .3f;
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
            Enter(candidate, true);
            world.Session?.SaveNow();
            return true;
        }

        public void Enter(PlanetView destination, bool startOnFoot = false)
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
            previousAmbientMode=RenderSettings.ambientMode;previousAmbientSky=RenderSettings.ambientSkyColor;
            previousAmbientEquator=RenderSettings.ambientEquatorColor;previousAmbientGround=RenderSettings.ambientGroundColor;
            previousAmbientIntensity=RenderSettings.ambientIntensity;
            previousAmbientProbe=RenderSettings.ambientProbe;
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
            BuildAstronaut();
            BuildSurfaceBattle();
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = SkyColor();
            lens.farClipPlane = 950;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = SkyColor();
            RenderSettings.fogDensity = Biome == SurfaceBiome.Ocean ? .0018f : .0028f;
            SurfacePalette lightingPalette=SurfacePalette.For(Biome);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Custom;
            RenderSettings.ambientSkyColor=lightingPalette.zenith*.45f;RenderSettings.ambientEquatorColor=lightingPalette.horizon*.34f;
            RenderSettings.ambientGroundColor=lightingPalette.stone*.18f;RenderSettings.ambientIntensity=1;
            // Custom mode keeps the daylight SH; gradient mode replaces it during the next render.
            var daylight = new UnityEngine.Rendering.SphericalHarmonicsL2();
            daylight.AddAmbientLight(lightingPalette.horizon * .35f);
            daylight.AddDirectionalLight(Vector3.up, lightingPalette.cloud, .24f);
            RenderSettings.ambientProbe = daylight;
            // This remote local region has no baked probe volume; use its own ambient sky.
            foreach (var renderer in sceneRoot.GetComponentsInChildren<Renderer>(true))
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            // Broad skylight bounce for the local region; no competing hard shadows.
            var fill = new GameObject("Surface / diffuse sky bounce").AddComponent<Light>();
            fill.transform.SetParent(sceneRoot, false); fill.type = LightType.Directional;
            fill.transform.localRotation = Quaternion.LookRotation(new Vector3(.4f, -.65f, .65f));
            fill.color = lightingPalette.horizon; fill.intensity = .65f; fill.shadows = LightShadows.None;
            BuildSurfaceReflection();
            Message = "ROVER DEPLOYED / " + destination.Definition.title + " · Drive and recover hidden relics";
            UpdateCamera(true);
            if (startOnFoot) ArriveOnFoot();
        }

        public void Launch(bool persist = true)
        {
            if (!Active) return;
            string name = planet.Definition.title;
            ClearSurfaceBattle();
            planet = null;
            if (sceneRoot != null) Destroy(sceneRoot.gameObject);
            sceneRoot = null; explorer = null; astronaut = null;
            OnFoot = false; walkObstacles.Clear(); cameraHull.Clear(); walk = new WalkMotion(Vector3.zero, 0);
            buildCursor = null; buildVisuals.Clear(); rallyGates.Clear(); scenery.Clear(); rallyRoute = null;
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
            RenderSettings.ambientMode=previousAmbientMode;RenderSettings.ambientSkyColor=previousAmbientSky;
            RenderSettings.ambientEquatorColor=previousAmbientEquator;RenderSettings.ambientGroundColor=previousAmbientGround;
            RenderSettings.ambientIntensity=previousAmbientIntensity;
            RenderSettings.ambientProbe=previousAmbientProbe;
            chase.enabled = true;
            world.Player.FlightDisabled = false;
            Message = "ORBIT RESTORED / " + name;
            if (persist) world.Session?.SaveNow();
        }

        public SurfaceSave Snapshot() => new()
        {
            activePlanet = PlanetId,
            explorerPosition = Active ? explorerLocal : Vector3.zero,
            yaw = Active ? ExplorerHeading : 0,
            pitch = Active ? pitch : 0,
            onFoot = Active && OnFoot,
            parkedRoverPosition = Active && OnFoot ? parkedRover : Vector3.zero,
            parkedRoverYaw = Active && OnFoot ? parkedHeading : 0,
            battle = battle.Snapshot(),
            collected = new List<string>(collected),
            sandbox = sandbox.Snapshot()
        };

        public void Restore(SurfaceSave save)
        {
            if (Active) Launch(false);
            collected.Clear();
            battle.Restore(save?.battle);
            sandbox.Restore(save?.sandbox);
            if (save == null) return;
            foreach (string key in save.collected) collected.Add(key);
            if (string.IsNullOrEmpty(save.activePlanet)) return;
            var destination = world.Sector.Planet(save.activePlanet);
            Enter(destination);
            if (save.onFoot && Active)
            {
                SetExplorerPose(save.parkedRoverPosition, save.parkedRoverYaw, save.pitch);
                parkedRover = explorerLocal; parkedHeading = rover.heading;
                BeginWalking(save.explorerPosition, save.yaw);
                Message = "EXPEDITION RESTORED / On foot · Your rover is parked nearby";
            }
            SetExplorerPose(save.explorerPosition, save.yaw, save.pitch);
        }

        public void SetExplorerPose(Vector3 position, float heading, float tilt)
        {
            if (!Active) return;
            float x = Mathf.Clamp(position.x, -SurfaceRules.HalfWidth + 3, SurfaceRules.HalfWidth - 3);
            float z = Mathf.Clamp(position.z, -SurfaceRules.HalfWidth + 3, SurfaceRules.HalfWidth - 3);
            explorerLocal = new Vector3(x, OnFoot ? FootFloor(x, z) : DriveGround(x, z), z);
            if (OnFoot) { walk = new WalkMotion(Vector3.zero, heading); walkCameraYaw = heading; }
            else rover = new RoverMotion(heading, 0);
            cameraYawOffset = 0;
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
                float turn = Mathf.DeltaAngle(ExplorerHeading, angle);
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
            else Message = "PLANET BUILDER CLOSED / Exploration controls restored";
        }

        Vector3 BuildTarget()
        {
            Vector3 forward = Quaternion.Euler(0, ExplorerHeading, 0) * Vector3.forward;
            Vector3 raw = explorerLocal + forward * 9;
            float x = Mathf.Round(raw.x / SurfaceSandboxRules.Grid) * SurfaceSandboxRules.Grid;
            float z = Mathf.Round(raw.z / SurfaceSandboxRules.Grid) * SurfaceSandboxRules.Grid;
            return SurfaceSandboxRules.Snap(new Vector3(x, 0, z), Ground(x, z), buildLayer);
        }

        public bool TryPlaceBuild(SurfaceBuildKind kind, Vector3 rawPosition, int rotation, int layer = 0)
        {
            if (!Active || world.Player.Paused || !world.Empire.State.Get(PlanetId).owned) return false;
            float x = Mathf.Round(rawPosition.x / SurfaceSandboxRules.Grid) * SurfaceSandboxRules.Grid;
            float z = Mathf.Round(rawPosition.z / SurfaceSandboxRules.Grid) * SurfaceSandboxRules.Grid;
            Vector3 position = SurfaceSandboxRules.Snap(new Vector3(x, 0, z), Ground(x, z), layer);
            rotation = (rotation % 4 + 4) % 4;
            Vector2 footprint = SurfaceSandboxRules.Footprint(kind, rotation);
            if (Mathf.Abs(x) < 10 + footprint.x * .5f && Mathf.Abs(z + 28) < 11 + footprint.y * .5f)
            { Message = "LANDING ZONE / Keep the ship airlock and rover access clear"; return false; }
            if (SurfaceWalkRules.InsideBox(explorerLocal + Vector3.up, position + Vector3.up * 2,
                new Vector3(footprint.x, 4, footprint.y), 0, 1))
            { Message = "PLANET BUILDER / Move clear of this module's footprint"; return false; }
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
            UpdateSceneryVisibility();
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
            if (!Active || rallyRoute == null || world.Player.Paused) return false;
            if (OnFoot) { Message = "ROVER RALLY / Board your rover to race"; return false; }
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
            TickSurfaceBattle(Time.deltaTime);
            if (Recovering) return;
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.lKey.wasPressedThisFrame) { Launch(); return; }
            if (keys.bKey.wasPressedThisFrame) ToggleBuildMode();
            if (!buildMode && keys.eKey.wasPressedThisFrame) { TryInteract(); if (!Active) return; }
            if (keys.tKey.wasPressedThisFrame) StartRally();
            if (keys.fKey.wasPressedThisFrame) TryCollect();
            if (keys.iKey.wasPressedThisFrame) { pingRemaining = 8; Message = NearestRelicDistance < 0 ? "NO REMAINING SIGNALS" : $"PULSE RETURN / Nearest relic {NearestRelicDistance:0} m"; }
            pingRemaining = Mathf.Max(0, pingRemaining - Time.deltaTime);
            Vector2 look = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            if (OnFoot)
            {
                pitch = Mathf.Clamp(pitch - look.y * .14f, -25, 55);
                UpdateWalking(keys, look); return;
            }
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
            if (OnFoot) { UpdateWalkingCamera(immediate); return; }
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

        Material MakeTextured(string resource,Color color,float metallic,float smoothness)
        {
            var result=VisualFactory.TexturedLit(resource,color,metallic,smoothness);materials.Add(result);return result;
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
            var accent = biome == SurfaceBiome.Frozen ? Make(new Color(.64f, .8f, .87f), .04f, null, .48f) :
                MakeTextured("PbrTerrain", Color.Lerp(Color.white, soil, .3f), .02f, .2f);
            var dark = MakeTextured("PbrTerrain", Color.Lerp(Color.white, palette.stone, .22f), .02f, .16f);
            var bark = MakeTextured("PbrTerrain", new Color(.52f, .4f, .28f), 0, .15f);
            var foliage = Make(Color.Lerp(palette.lowland, new Color(.18f, .4f, .13f), .52f));
            foliageCard=biome==SurfaceBiome.Temperate?MakeTextured("FoliageCutout",Color.white,0,.22f):null;
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
            }
            else
            {
                for (int i = 0; i < 75; i++)
                {
                    Vector3 p = Scatter(random, 18, 143); p.y = Ground(p.x, p.z);
                    if (Mathf.Abs(p.x) < 12 && Mathf.Abs(p.z + 28) < 13) continue;
                    if (Terrain(p.x, p.z) < SurfaceRules.WaterLevel(biome) + .4f) continue;
                    bool nearRelic = false;
                    foreach (var relic in relicPositions) if (Vector2.Distance(new Vector2(p.x, p.z), relic) < 8) nearRelic = true;
                    if (nearRelic) continue;
                    if (biome == SurfaceBiome.Temperate) BuildPlant(p, random, bark);
                    else if (biome == SurfaceBiome.Toxic) BuildFungus(p, random, dark, accent);
                    else if (biome == SurfaceBiome.Frozen) BuildSpire(p, random, accent, true);
                    else BuildSpire(p, random, dark, false);
                }
                BuildLandmarks(random, dark, biome == SurfaceBiome.Temperate ? foliage : accent, glow);
            }
            if (biome == SurfaceBiome.Temperate || biome == SurfaceBiome.Toxic || biome == SurfaceBiome.Ocean) BuildFauna(random, dark, accent, glow);
            int faction = world.Empire.State.Get(planet.Definition.id).faction;
            if (faction > 0) BuildCivilization(random, faction, MakeTextured("PbrConstruction",new Color(.3f,.34f,.36f),.68f,.55f), accent, glow);
            BuildLander(MakeTextured("PbrConstruction",new Color(.38f,.42f,.45f),.72f,.62f), accent, glow);
            BuildRelics(seed, glow, dark);
        }

        void BuildPlayerStructures()
        {
            SurfacePalette palette = SurfacePalette.For(Biome);
            buildAlloy = MakeTextured("PbrConstruction",new Color(.27f,.32f,.35f),.62f,.43f);
            buildPanel = MakeTextured("PbrConstruction",Color.Lerp(palette.stone,new Color(.82f,.86f,.87f),.7f),.38f,.4f);
            buildGlass = Make(new Color(.075f, .14f, .18f), .55f, null, .88f);
            buildPreview = Make(new Color(.08f, .7f, .72f), .1f, new Color(.08f, 1.6f, 1.8f));
            buildConcrete = MakeTextured("PbrTerrain",Color.Lerp(palette.stone,Color.white,.56f),.04f,.17f);
            buildRoad = MakeTextured("PbrRoad",Color.white,.05f,.12f);
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
            SurfaceArchitecture.Build(build.kind, root.transform, buildPanel, buildAlloy, buildGlass,
                buildConcrete, buildRoad, buildMarking, buildHazard);
            buildVisuals[build.id] = root; UpdateSceneryVisibility(); return root;
        }

        Transform CreateSceneryRoot(string name, Vector3 position, float radius, float height)
        {
            var root = new GameObject("Surface scenery / " + name).transform;
            root.SetParent(sceneRoot, false); root.localPosition = position;
            scenery.Add((root, radius, height)); return root;
        }

        void UpdateSceneryVisibility()
        {
            // Scenery is a projection of saved lots; clearing a lot never edits terrain or save data.
            foreach (var item in scenery)
            {
                Vector3 p = item.root.localPosition; bool clear = true;
                foreach (var build in sandbox.Builds)
                {
                    if (build.planet != PlanetId || build.position.y > p.y + item.height ||
                        build.position.y + SurfaceSandboxRules.StructureHeight(build.kind) < p.y - .5f) continue;
                    Vector2 size = SurfaceSandboxRules.Footprint(build.kind, build.rotation);
                    if (Mathf.Abs(p.x - build.position.x) < size.x * .5f + item.radius &&
                        Mathf.Abs(p.z - build.position.z) < size.y * .5f + item.radius) { clear = false; break; }
                }
                item.root.gameObject.SetActive(clear);
            }
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
                material.SetTexture("_DetailAlbedo",Resources.Load<Texture2D>("Textures/TerrainMicroAlbedo"));
                material.SetTexture("_DetailNormal",Resources.Load<Texture2D>("Textures/TerrainMicroNormal"));
                material.SetFloat("_DetailScale",Biome==SurfaceBiome.Desert?5.5f:Biome==SurfaceBiome.Frozen?4.2f:3.25f);
                material.SetFloat("_DetailStrength",Biome==SurfaceBiome.Ocean?.35f:.7f);
                material.SetFloat("_NormalStrength",Biome==SurfaceBiome.Frozen?.25f:.62f);
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

        void BuildPlant(Vector3 p, System.Random random, Material trunk)
        {
            float height = 4.5f + (float)random.NextDouble() * 4;
            var root = CreateSceneryRoot("Grove", p, 2.8f, height + 3);
            p = Vector3.zero;
            var tree = new SurfaceMeshBuilder();
            Vector3 top = p + new Vector3(.35f, height, -.15f);
            tree.Beam(p - Vector3.up * .15f, top, .27f, trunk, .055f);
            for (int j = 0; j < 5; j++)
            {
                float angle = j * 2.4f + (float)random.NextDouble() * .35f;
                Vector3 direction = new(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                tree.Beam(p + direction * .9f, p + Vector3.up * .65f, .06f, trunk, .16f);
                float reach = 1.3f + (float)random.NextDouble() * 1.15f;
                Vector3 branchBase = p + Vector3.up * (height * (.43f + j * .085f));
                Vector3 crown = p + direction * reach + Vector3.up * (height * .77f + j * .35f);
                tree.Beam(branchBase, crown, .1f, trunk, .025f);
                tree.Beam(Vector3.Lerp(branchBase, crown, .55f), crown + new Vector3(.5f, .8f, -.4f), .045f, trunk, .012f);
                if (foliageCard != null) for (int card = 0; card < 2; card++)
                    VisualFactory.FoliageCard("Grove / photoreal leaf cluster", root, crown,
                        new Vector2(4.1f, 3.6f), Quaternion.Euler(card * 24 - 8, angle * Mathf.Rad2Deg + card * 85, 0), foliageCard);
            }
            tree.Finish(root, "Grove / tapered trunk and roots");
            if (foliageCard != null) VisualFactory.FoliageCard("Grove / crown", root, top + Vector3.up * .5f,
                new Vector2(3.7f, 3.3f), Quaternion.Euler(0, (float)random.NextDouble() * 180, 0), foliageCard);
        }

        void BuildFungus(Vector3 p, System.Random random, Material stem, Material cap)
        {
            float height = 2 + (float)random.NextDouble() * 4;
            var root = CreateSceneryRoot("Fungal grove", p, 2.3f, height + 1);
            p = Vector3.zero;
            var m = new SurfaceMeshBuilder();
            m.Lathe(p, new[] { Vector2.zero, new Vector2(.65f, 0), new Vector2(.3f, height * .3f),
                new Vector2(.19f, height * .8f), new Vector2(.65f, height), new Vector2(0, height) }, stem);
            m.Lathe(p + Vector3.up * height, new[] { Vector2.zero, new Vector2(2.1f, -.18f), new Vector2(2.25f, 0),
                new Vector2(1.9f, .25f), new Vector2(1.1f, .65f), new Vector2(0, .85f) }, cap, 32);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6;
                m.Beam(p + Vector3.up * height, p + new Vector3(Mathf.Cos(a) * 1.95f, height - .18f, Mathf.Sin(a) * 1.95f), .025f, stem);
            }
            m.Finish(root, "Alien mycelium / ribbed fruiting body");
        }

        void BuildSpire(Vector3 p, System.Random random, Material baseMaterial, bool crystalline)
        {
            var m = new SurfaceMeshBuilder();
            float height = 2.5f + (float)random.NextDouble() * (crystalline ? 6 : 8);
            float width = crystalline ? height * .35f : height * .65f;
            var root = CreateSceneryRoot("Outcrop", p, width, height);
            p = Vector3.zero;
            m.Rock(p, new Vector3(width, height, width * .85f), random.Next(), baseMaterial, crystalline);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 2.1f;
                Vector3 offset = new(Mathf.Cos(angle) * width * .4f, -.12f, Mathf.Sin(angle) * width * .4f);
                m.Rock(p + offset, new Vector3(width * .65f, height * (.2f + i * .11f), width * .6f),
                    random.Next(), baseMaterial, crystalline);
            }
            m.Finish(root, crystalline ? "Fractured glacial formation" : "Weathered bedrock formation");
        }

        void BuildLandmarks(System.Random random, Material dark, Material accent, Material glow)
        {
            SurfaceBiome biome = Biome;
            var detail = new SurfaceMeshBuilder();
            int count = biome == SurfaceBiome.Ocean ? 24 : biome == SurfaceBiome.Temperate ? 190 : biome == SurfaceBiome.Toxic ? 95 : 48;
            for (int i = 0; i < count; i++)
            {
                Vector3 p = Scatter(random, biome == SurfaceBiome.Ocean ? 22 : 13, biome == SurfaceBiome.Ocean ? 175 : 140);
                p.y = Ground(p.x, p.z);
                if (biome != SurfaceBiome.Ocean && Terrain(p.x, p.z) < SurfaceRules.WaterLevel(biome) + .15f) continue;
                bool nearRelic = false;
                foreach (var relic in relicPositions) if (Vector2.Distance(new Vector2(p.x, p.z), relic) < 6) nearRelic = true;
                if (nearRelic) continue;
                if (biome == SurfaceBiome.Ocean)
                {
                    if (i % 6 == 0) detail.Rock(new Vector3(p.x, .3f, p.z), new Vector3(7, 1.3f, 2.8f), random.Next(), dark);
                }
                else if (biome == SurfaceBiome.Temperate)
                {
                    detail.Grass(p, random.Next(), accent);
                    if (i % 9 == 0) detail.Rock(p, new Vector3(1.8f, .7f, 1.4f), random.Next(), dark);
                }
                else if (biome == SurfaceBiome.Toxic)
                {
                    detail.Rock(p, new Vector3(1.4f + i % 3, .35f, 1.1f + i % 4), random.Next(), accent);
                    detail.Grass(p, random.Next(), accent);
                }
                else
                {
                    float width = 1.2f + (float)random.NextDouble() * 3.2f;
                    detail.Rock(p, new Vector3(width, width * (biome == SurfaceBiome.Frozen ? 1.4f : .55f), width * 1.1f),
                        random.Next(), i % 3 == 0 ? accent : dark, biome == SurfaceBiome.Frozen);
                    if (biome == SurfaceBiome.Volcanic)
                    {
                        Vector3 start = p;
                        for (int j = 0; j < 4; j++)
                        {
                            Vector3 end = start + new Vector3(1.2f, 0, (float)random.NextDouble() - .5f);
                            start.y = Terrain(start.x, start.z) + .06f; end.y = Terrain(end.x, end.z) + .06f;
                            detail.Beam(start, end, .035f, glow); start = end;
                        }
                    }
                }
            }
            detail.Finish(sceneRoot, "Surface / scattered stone and ground cover");
        }

        void BuildFauna(System.Random random, Material body, Material accent, Material eye)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector3 p = Scatter(random, 20, 125); p.y = Ground(p.x, p.z) + (Biome == SurfaceBiome.Ocean ? .7f : 1.5f);
                if (Biome != SurfaceBiome.Ocean)
                {
                    bool dry = true;
                    for (int sample = 0; sample < 8; sample++)
                    {
                        float a = sample * Mathf.PI / 4;
                        if (Terrain(p.x + Mathf.Cos(a) * 2.5f, p.z + Mathf.Sin(a) * 2.5f) < SurfaceRules.WaterLevel(Biome) + .4f) dry = false;
                    }
                    if (!dry) continue;
                }
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
            Material glazing = Make(new Color(.075f, .14f, .18f), .55f, null, .85f);
            Material concrete = MakeTextured("PbrTerrain", new Color(.48f, .47f, .42f), 0, .15f);
            var hub = new GameObject("Civilization / community hub").transform;
            hub.SetParent(sceneRoot, false); hub.localPosition = center + Vector3.up * .55f;
            SurfaceArchitecture.Build(SurfaceBuildKind.Habitat, hub, accent, structure, glazing, concrete, concrete, accent, light);
            walkObstacles.Add((hub, new Vector3(6, 4.2f, 6), Vector3.up * 2.1f));
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2 / 9;
                Vector3 p = center + new Vector3(Mathf.Cos(a) * 13, 0, Mathf.Sin(a) * 13);
                p.y = Ground(p.x, p.z);
                BuildSurfaceLink("Civilization / radial transit road", center + Vector3.up * .36f, p + Vector3.up * .18f, 2.2f, concrete);
                var habitat = new GameObject("Occupied habitat").transform;
                habitat.SetParent(sceneRoot, false); habitat.localPosition = p;
                habitat.localRotation = Quaternion.LookRotation(new Vector3(center.x - p.x, 0, center.z - p.z));
                SurfaceArchitecture.Build(SurfaceBuildKind.Habitat, habitat, accent, structure, glazing, concrete, concrete, accent, light);
                walkObstacles.Add((habitat, new Vector3(6, 4.2f, 6), Vector3.up * 2.1f));
                Vector3 lamp = Vector3.Lerp(center, p, .63f); lamp.y = Ground(lamp.x, lamp.z);
                BuildBeam("Civilization / street light mast", sceneRoot, lamp + Vector3.up * .15f, lamp + Vector3.up * 2.6f, .07f, structure);
                Shape("Civilization / street light", PrimitiveType.Sphere, sceneRoot, lamp + Vector3.up * 2.7f, Vector3.one * .15f, light);
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2 / 6;
                Vector3 p = center + new Vector3(Mathf.Sin(a) * 9, .9f, Mathf.Cos(a) * 9);
                var citizen = new GameObject(faction == 2 ? "Ithacan citizen" : faction == 3 ? "Dominion colonist" : "Union explorer").transform;
                citizen.SetParent(sceneRoot, false); citizen.localPosition = p;
                p.y = Ground(p.x, p.z); citizen.localPosition = p;
                var inhabitant = citizen.gameObject.AddComponent<SurfaceAstronaut>();
                SurfaceSpecies species = PlanetId == "veil-03" ? SurfaceSpecies.Ithacan : PlanetId.StartsWith("cinder-") ? SurfaceSpecies.Varkesh : SurfaceSpecies.Human;
                inhabitant.Build(accent, structure, glazing, Make(species == SurfaceSpecies.Ithacan ? new Color(.25f,.5f,.42f) : new Color(.5f,.27f,.17f)), light, false, species);
                inhabitant.Pose(0, .1f);
                contacts.Add(new SurfaceContact {root=citizen,model=inhabitant,vitals=new VitalState(100,0),home=p,
                    title=species==SurfaceSpecies.Ithacan?"ITHACAN / NEUTRAL CIVILIAN":species==SurfaceSpecies.Varkesh?"VARKESH / CIVILIAN":"UNION / CIVILIAN"});
            }
        }

        void BuildSurfaceLink(string name, Vector3 from, Vector3 to, float width, Material material)
        {
            Vector3 delta = to - from;
            var road = Shape(name, PrimitiveType.Cube, sceneRoot, (from + to) * .5f,
                new Vector3(width, .12f, delta.magnitude), material);
            road.transform.localRotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }

        void BuildSurfaceReflection()
        {
            if(Application.isBatchMode)return;
            var root=new GameObject("Surface environment reflection");root.transform.SetParent(sceneRoot,false);root.transform.localPosition=new Vector3(0,24,0);
            var probe=root.AddComponent<ReflectionProbe>();probe.mode=UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode=UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode=UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            // Include the 875 m sky dome as well as terrain in all cubemap faces.
            probe.size=Vector3.one*310;probe.nearClipPlane=.3f;probe.farClipPlane=1000;probe.resolution=128;
            probe.clearFlags=UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=SkyColor();
            probe.hdr=true;probe.intensity=1.05f;probe.boxProjection=false;StartCoroutine(CaptureReflection(probe));
        }
        System.Collections.IEnumerator CaptureReflection(ReflectionProbe probe)
        {
            // Let the local daylight and camera settle before the one-shot capture.
            yield return new WaitForSeconds(.3f);
            if(probe!=null)probe.RenderProbe();
        }

        void BuildLander(Material hull, Material accent, Material glow)
        {
            Shape(Biome == SurfaceBiome.Ocean ? "Floating landing raft" : "Ship landing apron", PrimitiveType.Cube,
                sceneRoot, new Vector3(0, Ground(0, -28), -28), new Vector3(13, .45f, 14), hull);
            Vector3 p = new(0, Ground(0, -28) + (Biome == SurfaceBiome.Ocean ? 1.8f : 2.8f), -28);
            var lander = new GameObject("Wayfarer landing craft").transform;
            lander.SetParent(sceneRoot, false); lander.localPosition = p;
            VisualFactory.Ship(lander, hull, accent, 2.1f);
            walkObstacles.Add((lander, new Vector3(4, 3, 12), new Vector3(0, -.4f, -1)));
            float deck = Ground(0, -28) + .225f;
            Shape("Ship / airlock frame", PrimitiveType.Cube, sceneRoot, new Vector3(2.6f, deck + 1.15f, -28), new Vector3(.25f, 2.3f, 1.6f), hull);
            Shape("Ship / airlock threshold", PrimitiveType.Cube, sceneRoot, new Vector3(3.5f, deck + .06f, -28), new Vector3(2, .12f, 1.8f), accent);
            Shape("Ship / airlock light", PrimitiveType.Cube, sceneRoot, new Vector3(2.8f, deck + 2.2f, -28), new Vector3(.12f, .1f, 1.4f), glow);
            for (int side = -1; side <= 1; side += 2)
                Shape("Landing thruster", PrimitiveType.Cylinder, lander, new Vector3(side * 5, -2.2f, -3), new Vector3(.6f, 2, .6f), glow);
            foreach (var filter in lander.GetComponentsInChildren<MeshFilter>())
            {
                var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh;
                cameraHull.Add(collider);
            }
            Physics.SyncTransforms();
        }


        void BuildRover()
        {
            var hull = MakeTextured("PbrConstruction",new Color(.82f,.86f,.83f),.34f,.54f);
            var frame = MakeTextured("PbrConstruction",new Color(.25f,.31f,.34f),.68f,.66f);
            var tire = MakeTextured("PbrRoad",new Color(.43f,.45f,.46f),0,.19f);
            var solar = Make(new Color(.035f, .2f, .34f), .55f);
            var glass = Make(new Color(.1f, .36f, .44f), .45f);
            var ochre = MakeTextured("PbrConstruction",new Color(.93f,.42f,.13f),.25f,.46f);
            var beacon = Make(new Color(.45f, .85f, .96f), .15f, new Color(.15f, 1.1f, 1.4f));

            SurfaceRoverModel.Build(explorer, wheels, pontoons, hull, frame, tire, solar, glass, ochre, beacon);
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
                RenderSettings.ambientMode=previousAmbientMode;RenderSettings.ambientSkyColor=previousAmbientSky;
                RenderSettings.ambientEquatorColor=previousAmbientEquator;RenderSettings.ambientGroundColor=previousAmbientGround;
                RenderSettings.ambientIntensity=previousAmbientIntensity;
                RenderSettings.ambientProbe=previousAmbientProbe;
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
