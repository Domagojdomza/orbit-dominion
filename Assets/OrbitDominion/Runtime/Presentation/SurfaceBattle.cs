using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OrbitDominion
{
    public sealed partial class SurfaceExpedition
    {
        public sealed class SurfaceContact
        {
            public Transform root;
            public SurfaceAstronaut model;
            public VitalState vitals;
            public string title;
            public bool hostile, training;
            public Vector3 home;
            public float attack, alert, reset;
        }
        readonly SurfaceBattleState battle = new();
        readonly List<SurfaceContact> contacts = new();
        readonly List<(LineRenderer line, float until)> groundBeams = new();
        AudioSource rifleAudio;
        AudioClip rifleClip;
        Material shotMaterial, hostileShotMaterial;
        Transform commandConsole;
        float hitMarker, incomingHit, rescue, firingPose;
        int groundShots, practiceHits;
        bool aiming, sprinting;
        public SurfaceBattleState GroundBattle => battle;
        public IReadOnlyList<SurfaceContact> GroundContacts => contacts;
        public bool Aiming => OnFoot && aiming && !buildMode;
        public int GroundShots => groundShots;
        public bool HitConfirmed => hitMarker > 0;
        public bool UnderFire => incomingHit > 0;
        public bool Recovering => rescue > 0 || !battle.Suit.Alive;
        public int GroundDefenders { get { int n = 0; foreach (var c in contacts) if (c.hostile && c.vitals.Alive) n++; return n; } }
        public Vector3 CommandLocal => commandConsole != null ? commandConsole.localPosition : Vector3.zero;
        public float GroundCaptureProgress => Active ? world.Sector.State.Get(PlanetId).capture / world.sectorSettings.captureSeconds : 0;
        public string GroundWeaponHint => Recovering ? $"SUIT RESCUE / {rescue:0.0}s" :
            battle.ReloadRemaining > 0 ? $"ARC-18 / RECHARGING {battle.ReloadRemaining:0.0}s" : $"ARC-18  {battle.Rounds}/{SurfaceBattleState.Magazine}  ·  R RECHARGE";
        public string GroundObjective
        {
            get
            {
                if (!Active) return "";
                int faction = world.Empire.State.Get(PlanetId).faction;
                if (faction == 3)
                {
                    if (world.Sector.IsProtected(PlanetId)) return "PLANET SHIELD ACTIVE / Capture unavailable";
                    if (GroundDefenders > 0) return $"SECURE THE COMMAND POST / {GroundDefenders} Varkesh guards remaining\nRMB aim · LMB fire · Use buildings as cover";
                    if (world.Combat.LocalDefenders(PlanetId, CombatFaction.Union)) return "ORBITAL DEFENSES REMAIN / Return to your ship and clear them";
                    return $"TAKE CONTROL / {(commandConsole == null ? 0 : FlatDistance(explorerLocal, CommandLocal)):0} m to command terminal\nOn foot, hold G within 4 m · {GroundCaptureProgress * 100:0}%";
                }
                if (faction == 2) return "ITHACAN ASSEMBLY / Neutral civilization\nCivilians are not targets · Explore and recover relics";
                if (PlanetId == "solace-01" && practiceHits < 3) return "TRY YOUR RIFLE / Orange practice targets near the ship\nRMB aim · LMB fire · R recharge · No resource cost";
                if (faction == 1) return "UNION WORLD / Build and explore\nB construct · I locate a relic · F recover · E board";
                return "UNCLAIMED WORLD / Survey and explore\nRecover relics here; establish a colony from orbit with C";
            }
        }

        void BuildSurfaceBattle()
        {
            aiming = sprinting = false; hitMarker = incomingHit = rescue = firingPose = 0; practiceHits = 0;
            shotMaterial = Make(new Color(.15f, .75f, 1), .1f, new Color(.2f, 2.6f, 4));
            hostileShotMaterial = Make(new Color(1, .2f, .07f), .1f, new Color(3, .25f, .05f));
            int faction = world.Empire.State.Get(PlanetId).faction;
            if (faction == 3)
            {
                var root = new GameObject("Surface / command terminal").transform; root.SetParent(sceneRoot, false);
                root.localPosition = new Vector3(54, Ground(54, 29), 29); commandConsole = root;
                var mesh = new SurfaceMeshBuilder();
                mesh.Box(Vector3.up * .6f, new Vector3(1.6f, 1.2f, 1), buildAlloy, .1f);
                mesh.Box(new Vector3(0, 1.3f, .1f), new Vector3(1.3f, .2f, .85f), buildGlass, .06f, Quaternion.Euler(-20, 0, 0));
                mesh.Box(new Vector3(0, 1.43f, .12f), new Vector3(.95f, .02f, .45f), hostileShotMaterial, .005f);
                mesh.Finish(root, "Command / uplink console");
                VisualFactory.Ring("Command / capture zone", root, 4, .045f, shotMaterial, 64);
                for (int i = 0; i < SurfaceBattleState.GuardsPerWorld; i++)
                {
                    Vector3 position = new Vector3(43 + i * 9, 0, 22 + (i % 2) * 5); position.y = Ground(position.x, position.z);
                    var guard = new GameObject("Varkesh sentry / " + i).transform; guard.SetParent(sceneRoot, false); guard.localPosition = position;
                    var model = guard.gameObject.AddComponent<SurfaceAstronaut>();
                    model.Build(Make(new Color(.29f, .32f, .34f), .55f), buildAlloy, buildGlass,
                        Make(new Color(.48f, .20f, .12f), .1f), hostileShotMaterial, true, SurfaceSpecies.Varkesh);
                    var vital = battle.Guard(PlanetId, i);
                    guard.gameObject.SetActive(vital.Alive);
                    contacts.Add(new SurfaceContact { root = guard, model = model, vitals = vital, hostile = true, home = position,
                        title = "VARKESH SENTRY", attack = .7f + i * .35f });
                }
            }
            if (PlanetId == "solace-01")
            {
                var orange = Make(new Color(.9f, .38f, .08f), .3f, new Color(.5f, .12f, .01f));
                for (int i = 0; i < 3; i++)
                {
                    Vector3 p = new(-13 + i * 4, 0, -8); p.y = Ground(p.x, p.z);
                    var target = new GameObject("Training / target " + i).transform; target.SetParent(sceneRoot, false); target.localPosition = p;
                    var mesh = new SurfaceMeshBuilder();
                    mesh.Cylinder(Vector3.up * .65f, .05f, 1.3f, buildAlloy);
                    mesh.Ellipsoid(Vector3.up * 1.3f, new Vector3(.8f, 1.1f, .55f), orange);
                    mesh.Ellipsoid(new Vector3(0, 1.33f, -.29f), new Vector3(.28f, .28f, .03f), buildAlloy);
                    mesh.Finish(target, "Practice / target");
                    contacts.Add(new SurfaceContact { root = target, vitals = new VitalState(68, 0), training = true, home = p, title = "PRACTICE TARGET" });
                }
            }
            rifleAudio = sceneRoot.gameObject.AddComponent<AudioSource>(); rifleAudio.spatialBlend = 0; rifleAudio.volume = .24f;
            const int samples = 5000; var data = new float[samples]; var random = new System.Random(718);
            for (int i = 0; i < samples; i++)
            {
                float t = i / 44100f; data[i] = ((float)random.NextDouble() * 2 - 1) * Mathf.Exp(-t * 60) * .5f +
                    Mathf.Sin(t * (2400 - t * 7000)) * Mathf.Exp(-t * 38) * .4f;
            }
            rifleClip = AudioClip.Create("ARC-18 / discharge", samples, 1, 44100, false); rifleClip.SetData(data, 0);
        }

        void ClearSurfaceBattle()
        {
            contacts.Clear(); groundBeams.Clear(); commandConsole = null;
            if (rifleClip != null) Destroy(rifleClip);
            rifleClip = null; rifleAudio = null; aiming = sprinting = false;
        }

        void TickSurfaceBattle(float dt)
        {
            battle.Tick(dt, world.Player.Paused);
            if (world.Player.Paused) return;
            hitMarker = Mathf.Max(0, hitMarker - dt); incomingHit = Mathf.Max(0, incomingHit - dt); firingPose = Mathf.Max(0, firingPose - dt);
            for (int i = 0; i < groundBeams.Count; i++)
                if (groundBeams[i].line != null && Time.time >= groundBeams[i].until) groundBeams[i].line.enabled = false;
            if (!battle.Suit.Alive)
            {
                if (rescue <= 0) rescue = 3;
                rescue -= dt; Message = "SUIT CRITICAL / Emergency extraction to the landing craft";
                if (rescue <= 0) { battle.Suit.Reset(); BeginWalking(new Vector3(4.4f, 0, -24), 0); world.Session?.SaveNow(); Message = "RESCUED AT SHIP / Progress preserved · Use cover and let your shield recharge"; }
                return;
            }
            int faction = world.Empire.State.Get(PlanetId).faction;
            foreach (var actor in contacts)
            {
                if (actor.training)
                {
                    if (!actor.vitals.Alive) { actor.reset += dt; if (actor.reset > 4) { actor.vitals.Reset(); actor.reset = 0; actor.root.gameObject.SetActive(true); } }
                    continue;
                }
                if (!actor.hostile || !actor.vitals.Alive) continue;
                if (faction != 3) { actor.hostile = false; actor.root.gameObject.SetActive(false); continue; }
                float range = FlatDistance(actor.root.localPosition, explorerLocal);
                bool safeLanding = FlatDistance(explorerLocal, new Vector3(0, 0, -28)) < 14;
                bool sees = OnFoot && !safeLanding && range < 42 &&
                    TraceGroundCover(actor.root.localPosition + Vector3.up * 1.5f, explorerLocal + Vector3.up * 1.1f) >= .99f;
                actor.alert = Mathf.MoveTowards(actor.alert, sees ? 1 : 0, dt * 1.7f);
                Vector3 before = actor.root.localPosition;
                if (sees)
                {
                    Vector3 direction = explorerLocal - before; direction.y = 0;
                    if (direction.sqrMagnitude > .001f) actor.root.localRotation = Quaternion.Slerp(actor.root.localRotation, Quaternion.LookRotation(direction), 1 - Mathf.Exp(-8 * dt));
                    actor.attack -= dt;
                    if (actor.alert >= .95f && actor.attack <= 0)
                    {
                        actor.attack = 1.2f;
                        Vector3 from = sceneRoot.InverseTransformPoint(actor.model.Muzzle), to = explorerLocal + Vector3.up * 1.1f;
                        if (TraceGroundCover(from, to) >= .99f)
                        {
                            ShowGroundBeam(from, to, true); actor.model.Kick();
                            battle.Suit.Damage(12, CombatFaction.Hostile, CombatFaction.Union); incomingHit = .35f;
                        }
                    }
                }
                else
                {
                    Vector3 goal = actor.home + new Vector3(Mathf.Sin(Time.time * .3f + actor.home.x), 0, Mathf.Cos(Time.time * .3f + actor.home.x)) * 1.5f;
                    Vector3 next = Vector3.MoveTowards(before, goal, dt * .8f); next.y = Ground(next.x, next.z);
                    if (!WalkObstructed(next + Vector3.up, false) && next.y > SurfaceRules.WaterLevel(Biome) + .2f) actor.root.localPosition = next;
                    Vector3 delta = actor.root.localPosition - before; delta.y = 0;
                    if (delta.sqrMagnitude > .00001f) actor.root.localRotation = Quaternion.Slerp(actor.root.localRotation, Quaternion.LookRotation(delta), dt * 5);
                }
                actor.model.Pose(new Vector3(0, 0, dt > 0 ? FlatDistance(before, actor.root.localPosition) / dt : 0), dt, sees, 0, false, 0, 0);
            }
            if (faction == 3 && commandConsole != null)
            {
                bool held = Keyboard.current?.gKey.isPressed == true && OnFoot && !buildMode;
                if (world.Sector.SurfaceCaptureTick(PlanetId, held, FlatDistance(explorerLocal, CommandLocal) <= 4, GroundDefenders > 0, dt))
                { Message = "PLANET SECURED / UNION CONTROL · Buildings preserved · B to develop your new world"; world.Session?.SaveNow(); }
            }
        }

        public bool ReloadRifle() => battle.Reload(Active && OnFoot && !world.Player.Paused && !buildMode);
        public bool TryFireGround(Vector3 worldAimPoint)
        {
            if (!Active || astronaut == null || !battle.Fire(OnFoot, world.Player.Paused, buildMode, sprinting)) return false;
            Vector3 from = sceneRoot.InverseTransformPoint(astronaut.Muzzle);
            Vector3 goal = sceneRoot.InverseTransformPoint(worldAimPoint);
            Vector3 direction = (goal - from).normalized;
            if (direction.sqrMagnitude < .5f) direction = astronaut.transform.forward;
            goal = from + direction * Mathf.Min(SurfaceBattleState.Range, Mathf.Max(1, Vector3.Distance(from, goal)));
            float cover = TraceGroundCover(from, goal), range = Vector3.Distance(from, goal) * cover;
            SurfaceContact hit = null;
            foreach (var actor in contacts)
            {
                if (!actor.vitals.Alive) continue;
                float d = CombatRules.SphereHit(from, direction, actor.root.localPosition + Vector3.up * 1.25f, actor.training ? .55f : .43f);
                d = Mathf.Min(d, CombatRules.SphereHit(from, direction, actor.root.localPosition + Vector3.up * 1.82f, .22f));
                if (d < range) { range = d; hit = actor; }
            }
            Vector3 end = from + direction * range;
            ShowGroundBeam(from, end, false); astronaut.Kick(); firingPose = .3f; groundShots++;
            if (rifleAudio != null) rifleAudio.PlayOneShot(rifleClip);
            if (hit != null)
            {
                if (hit.hostile || hit.training)
                {
                    hit.vitals.Damage(SurfaceBattleState.ShotDamage, CombatFaction.Union, CombatFaction.Hostile);
                    hitMarker = .18f;
                    if (hit.training) practiceHits++;
                    if (!hit.vitals.Alive) { hit.root.gameObject.SetActive(false); Message = hit.training ? "TARGET CLEARED / Try moving, aiming and recharging" : $"GUARD NEUTRALIZED / {GroundDefenders} remain"; }
                    world.Session?.SaveNow();
                }
                else Message = "HOLD FIRE / Neutral civilians are protected";
            }
            if (battle.Rounds == 0) battle.Reload(true);
            return true;
        }

        void HandleRifleInput(Keyboard keys)
        {
            if (keys.rKey.wasPressedThisFrame) ReloadRifle();
            if (Mouse.current?.leftButton.isPressed != true) return;
            var ray = lens.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            Vector3 from = sceneRoot.InverseTransformPoint(ray.origin), goal = from + ray.direction * SurfaceBattleState.Range;
            float range = SurfaceBattleState.Range * TraceGroundCover(from, goal);
            bool aimedContact = false;
            foreach (var actor in contacts) if (actor.vitals.Alive)
            {
                float hit = CombatRules.SphereHit(from, ray.direction, actor.root.localPosition + Vector3.up * 1.25f, actor.training ? .55f : .43f);
                hit = Mathf.Min(hit, CombatRules.SphereHit(from, ray.direction, actor.root.localPosition + Vector3.up * 1.82f, .22f));
                if (hit < range) { range = hit; aimedContact = true; }
            }
            // Aim just inside the selected silhouette; the muzzle performs its own occlusion test.
            if (aimedContact) range += .12f;
            TryFireGround(sceneRoot.TransformPoint(from + ray.direction * range));
        }

        // Sampling covers terrain, lots and native geometry; hull rays catch thin ship surfaces.
        public float TraceGroundCover(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from; float length = delta.magnitude;
            if (length < .01f) return 1;
            float allowed = length;
            foreach (var hull in cameraHull)
            {
                if (hull == null) continue;
                if (hull.Raycast(new Ray(sceneRoot.TransformPoint(from), delta / length), out var hit, allowed)) allowed = hit.distance;
                if (hull.Raycast(new Ray(sceneRoot.TransformPoint(to), -delta / length), out hit, length)) allowed = Mathf.Min(allowed, length - hit.distance);
            }
            int steps = Mathf.Max(1, Mathf.CeilToInt(allowed / .2f));
            for (int i = 0; i <= steps; i++)
            {
                float distance = allowed * i / steps; Vector3 sample = from + delta / length * distance;
                if (sample.y <= Terrain(sample.x, sample.z) + .05f || WalkObstructed(sample, OnFoot))
                    return Mathf.Max(0, distance - .12f) / length;
            }
            return allowed / length;
        }

        void ShowGroundBeam(Vector3 from, Vector3 to, bool hostile)
        {
            int index = groundBeams.FindIndex(item => item.line == null || !item.line.enabled);
            LineRenderer line;
            if (index >= 0) line = groundBeams[index].line;
            else if (groundBeams.Count >= 16) { index = 0; line = groundBeams[0].line; }
            else
            {
                line = new GameObject("Surface / rifle tracer").AddComponent<LineRenderer>(); line.transform.SetParent(sceneRoot, false);
                line.useWorldSpace = false; line.positionCount = 2; line.widthMultiplier = .025f; index = groundBeams.Count;
                groundBeams.Add((line, 0));
            }
            line.sharedMaterial = hostile ? hostileShotMaterial : shotMaterial; line.SetPosition(0, from); line.SetPosition(1, to); line.enabled = true;
            groundBeams[index] = (line, Time.time + .065f);
        }
    }
}
