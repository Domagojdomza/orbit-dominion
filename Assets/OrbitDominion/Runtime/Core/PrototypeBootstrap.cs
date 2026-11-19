using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace OrbitDominion
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        public FlightSettings settings;
        public InputActionAsset input;
        public PanelSettings panelSettings;
        public VolumeProfile cinematicProfile;
        public DiscoverySettings discoverySettings;
        public EmpireSettings empireSettings;
        public CombatSettings combatSettings;
        public CombatController Combat {get;private set;}
        public SectorSettings sectorSettings;
        public SectorController Sector {get;private set;}
        public GameSession Session {get;private set;}
        public SurfaceExpedition Surface {get;private set;}
        public GalaxyOrbitController Orbits {get;private set;}
        public EmpireController Empire {get;private set;}
        public DiscoveryController Discovery {get;private set;}
        public static bool SuppressPersistence {get;set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()=>SuppressPersistence=false;
        public PlayerFlight Player { get; private set; }
        public PlanetView[] Worlds { get; private set; }
        public static readonly Vector3 StartPosition = new(0,30,-470);
        public static readonly Quaternion StartRotation = Quaternion.Euler(2,23,0);
        public Vector3 Beacon => new(360,45,-300);
        void Start()
        {
            Application.targetFrameRate=120;
            var definitions = new[] {
                new WorldDefinition("solace-01","MERIDIAN",new Vector3(-220,0,140),310,new Color(.01f,.055f,.12f),new Color(.13f,.27f,.18f),new Color(.4f,.42f,.3f),new Color(.09f,.45f,1),.49f,.8f,true)
                    {survey=new PlanetSurvey("TEMPERATE WORLD","Nitrogen / oxygen",18,91,62,72,"Complex biosphere","Interplanetary","Wayfarer Union",80,3,"Home of the Wayfarer fleet")},
                new WorldDefinition("solace-02","AURELIA II",new Vector3(1300,180,3200),260,new Color(.008f,.11f,.15f),new Color(.1f,.28f,.22f),new Color(.45f,.55f,.32f),new Color(.1f,.8f,.8f),.5f,.55f)
                    {survey=new PlanetSurvey("EARTH-LIKE WORLD","Nitrogen / oxygen",24,88,57,84,"Simple multicellular","None","Unclaimed",0,0,"Fertile biosphere")},
                new WorldDefinition("solace-03","NEREID IV",new Vector3(-2900,-280,5000),390,new Color(.2f,.28f,.35f),new Color(.42f,.58f,.66f),new Color(.8f,.88f,.91f),new Color(.25f,.5f,.9f),.38f,.25f,false,true)
                    {survey=new PlanetSurvey("FROZEN WORLD","Thin carbon dioxide",-87,19,94,43,"Microbial","None","Unclaimed",0,0,"Rare minerals")}
            };
            definitions=GalaxyCatalog.Expand(definitions,settings.galaxySeed);
            Worlds=new PlanetView[definitions.Length];
            for(int i=0;i<definitions.Length;i++) { Worlds[i]=new GameObject(definitions[i].id).AddComponent<PlanetView>(); Worlds[i].transform.SetParent(transform); Worlds[i].Initialize(definitions[i],settings.galaxySeed+i*17); }
            RenderSettings.skybox=new Material(Shader.Find("Orbit Dominion/Space"));
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.09f,.13f,.22f);
            var sun=new GameObject("Solace / directional starlight").AddComponent<Light>(); sun.transform.SetParent(transform); sun.type=LightType.Directional;
            sun.transform.rotation=Quaternion.LookRotation(-new Vector3(-.65f,.45f,-.55f)); sun.color=new Color(1,.92f,.83f);sun.intensity=1.8f;
            sun.shadows=LightShadows.Soft;sun.shadowStrength=.82f;
            sun.shadowBias=.035f;sun.shadowNormalBias=.28f;
            var volume=new GameObject("Global cinematic volume").AddComponent<Volume>(); volume.transform.SetParent(transform);volume.isGlobal=true;
            volume.sharedProfile=cinematicProfile;
            var metal=VisualFactory.TexturedLit("PbrConstruction",new Color(.46f,.54f,.62f),.75f,.66f);
            var cyan=VisualFactory.Lit(new Color(.02f,.4f,.6f),.5f,new Color(.06f,.7f,1.5f));
            Player=new GameObject("Player / Wayfarer").AddComponent<PlayerFlight>();Player.transform.SetParent(transform);
            Player.transform.SetPositionAndRotation(StartPosition,StartRotation);Player.settings=settings;Player.worlds=Worlds;Player.Initialize(input);
            var ship=VisualFactory.Ship(Player.transform,metal,cyan);
            var effects=Player.gameObject.AddComponent<EngineEffects>();effects.player=Player;effects.Initialize(ship,cyan);
            var camera=new GameObject("Flight camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.SetParent(transform);
            camera.transform.SetPositionAndRotation(StartPosition+StartRotation*new Vector3(0,4.2f,-18),StartRotation);
            camera.nearClipPlane=.2f;camera.farClipPlane=65000;camera.allowHDR=true;camera.fieldOfView=settings.baseFov;camera.gameObject.AddComponent<AudioListener>();
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var chase=camera.gameObject.AddComponent<ChaseCamera>();chase.player=Player;
            bool sectorCapture=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-sectorCapture")>=0;
            bool combatCapture=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-combatCapture")>=0;
            bool surfaceCapture=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-surfaceCapture")>=0;
            bool systemCapture=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-systemCapture")>=0;
            bool sandboxCapture=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-sandboxCapture")>=0;
            bool walkCapture=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-walkCapture")>=0;
            bool capture=walkCapture||surfaceCapture||sectorCapture||combatCapture||systemCapture||sandboxCapture||System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-orbitCapture")>=0;
            Discovery=gameObject.AddComponent<DiscoveryController>();
            Discovery.Initialize(Player,Worlds,discoverySettings,settings.galaxySeed,Beacon,null);
            var scanEffects=gameObject.AddComponent<ScanEffects>();scanEffects.Initialize(Discovery);
            Empire=gameObject.AddComponent<EmpireController>();
            Empire.Initialize(this,null);
            foreach(var planet in Worlds)planet.gameObject.AddComponent<ColonyView>().Initialize(this,planet);
            Combat=gameObject.AddComponent<CombatController>();Combat.Initialize(this);
            Sector=gameObject.AddComponent<SectorController>();Sector.Initialize(this);
            Surface=gameObject.AddComponent<SurfaceExpedition>();Surface.Initialize(this,camera,chase);
            Discovery.Surface=Surface;
            Orbits=gameObject.AddComponent<GalaxyOrbitController>();Orbits.Initialize(this);
            Session=gameObject.AddComponent<GameSession>();Session.Initialize(this,SuppressPersistence||capture?null:System.IO.Path.Combine(Application.persistentDataPath,"flight-v2.json"));
            gameObject.AddComponent<GalaxyPresentation>().Initialize(this,sun);
            var hud=gameObject.AddComponent<FlightHud>();hud.world=this;hud.lens=camera;
            var document=gameObject.AddComponent<UIDocument>();document.panelSettings=panelSettings;hud.Initialize(document);
            var beacon=VisualFactory.Shape("Navigation beacon",PrimitiveType.Sphere,transform,Beacon,Vector3.one*2,cyan);
            VisualFactory.Ring("Beacon orbit",beacon.transform,8,.16f,cyan);
            CreateAsteroids(metal);
            if(capture) {gameObject.AddComponent<RuntimeMetrics>();Player.enabled=false;StartCoroutine(walkCapture?CaptureWalking():systemCapture?CaptureSystems():sandboxCapture?CaptureSandbox():surfaceCapture?CaptureSurface():sectorCapture?CaptureSector():combatCapture?CaptureCombat():Capture());}
        }

        IEnumerator CaptureWalking()
        {
            // Isolated QA: no player archive is opened, and injected input is scoped to this run.
            bool previousBackground = Application.runInBackground;
            var previousInput = InputSystem.settings.backgroundBehavior;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return new WaitForSeconds(2);
                Surface.Enter(Worlds[0], true);
                yield return new WaitForSeconds(2);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../walk-airlock.png"));
                yield return new WaitForSeconds(1);
                bool entered = Surface.TryInteract();
                yield return new WaitForSeconds(1);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../walk-rover.png"));
                yield return new WaitForSeconds(1);
                bool exited = Surface.TryInteract();
                Surface.SetExplorerPose(new Vector3(0, 0, 4), 0, 12);
                Vector3 before = Surface.ExplorerLocal;
                InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.W, Key.LeftShift));
                yield return new WaitForSeconds(.8f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../walk-exploration.png"));
                yield return new WaitForSeconds(.3f);
                InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
                yield return new WaitForSeconds(.4f);
                float walked = Vector3.Distance(before, Surface.ExplorerLocal);
                var archive = new GameArchive(System.IO.Path.Combine(Application.dataPath, "../qa-walking-v2.json"), Session.Validate);
                archive.Save(Session.Snapshot()); var loaded = archive.Load(out _);
                bool restored = Session.Validate(loaded) && Session.Restore(loaded) && Surface.OnFoot;
                Surface.SetExplorerPose(Surface.ShipEntrance, 270, 12);
                yield return new WaitForSeconds(1);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../walk-boarding.png"));
                yield return new WaitForSeconds(1);
                bool boarded = Surface.TryInteract() && !Surface.Active;
                Surface.Enter(Sector.Planet("veil-02"), true);
                yield return new WaitForSeconds(2);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../walk-ocean-deck.png"));
                yield return new WaitForSeconds(1);
                Debug.Log($"ORBIT_WALK_CAPTURE: enteredRover={entered}, exitedRover={exited}, walkedMetres={walked:0.00}, restoredOnFoot={restored}, boardedShip={boarded}, oceanOnFoot={Surface.OnFoot}");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard); InputSystem.settings.backgroundBehavior = previousInput;
                Application.runInBackground = previousBackground;
            }
            Application.Quit();
        }
        IEnumerator CaptureSandbox()
        {
            yield return new WaitForSeconds(2);
            Surface.Enter(Worlds[0]);
            Empire.State.Reward(600,400);
            for(int z=4;z<=16;z+=4)Surface.TryPlaceBuild(SurfaceBuildKind.Road,new Vector3(0,0,z),0);
            Surface.TryPlaceBuild(SurfaceBuildKind.Road,new Vector3(-4,0,16),1);
            Surface.TryPlaceBuild(SurfaceBuildKind.Road,new Vector3(4,0,16),1);
            Surface.TryPlaceBuild(SurfaceBuildKind.Foundation,new Vector3(-8,0,20),0);
            Surface.TryPlaceBuild(SurfaceBuildKind.Foundation,new Vector3(8,0,20),0);
            Surface.TryPlaceBuild(SurfaceBuildKind.Habitat,new Vector3(-8,0,28),0);
            Surface.TryPlaceBuild(SurfaceBuildKind.Arcology,new Vector3(8,0,28),0);
            Surface.TryPlaceBuild(SurfaceBuildKind.Industry,new Vector3(-12,0,40),0);
            Surface.TryPlaceBuild(SurfaceBuildKind.Spaceport,new Vector3(12,0,44),0);
            Surface.TryPlaceBuild(SurfaceBuildKind.Beacon,new Vector3(0,0,32),0);
            Surface.SetExplorerPose(new Vector3(0,0,8),0,9);Surface.ToggleBuildMode();
            yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../sandbox-builder.png"));
            yield return new WaitForSeconds(1);Surface.ToggleBuildMode();
            // Isolated art inspection; ordinary play retains the rover follow camera and HUD.
            Surface.enabled=false;GetComponent<UIDocument>().enabled=false;
            var camera=Camera.main;camera.fieldOfView=52;
            var rover=GameObject.Find("Wayfarer rover").transform;
            Vector3 roverView=rover.position+new Vector3(6.8f,4.3f,8.2f);
            camera.transform.SetPositionAndRotation(roverView,Quaternion.LookRotation(rover.position+Vector3.up*1.8f-roverView));
            yield return new WaitForSeconds(1);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../model-rover-detail.png"));
            yield return new WaitForSeconds(1);
            Vector3 districtView=rover.parent.TransformPoint(new Vector3(0,18,0));
            Vector3 districtTarget=rover.parent.TransformPoint(new Vector3(0,4,30));
            camera.transform.SetPositionAndRotation(districtView,Quaternion.LookRotation(districtTarget-districtView));
            yield return new WaitForSeconds(1);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../model-settlement-detail.png"));
            yield return new WaitForSeconds(1);
            Surface.enabled=true;GetComponent<UIDocument>().enabled=true;
            var route=Surface.RallyRoute;Vector2 direction=(route[1]-route[0]).normalized;
            float heading=Mathf.Atan2(direction.x,direction.y)*Mathf.Rad2Deg;
            Vector2 staging=route[0]-direction*15;
            Surface.SetExplorerPose(new Vector3(staging.x,0,staging.y),heading,8);
            yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../sandbox-rally.png"));
            var archive=new GameArchive(System.IO.Path.Combine(Application.dataPath,"../qa-sandbox-v2.json"),Session.Validate);archive.Save(Session.Snapshot());
            Debug.Log($"ORBIT_SANDBOX_CAPTURE: builds={Surface.LocalBuildCount}, rallyGates={route.Count}, saveValid={Session.Validate(archive.Load(out _))}");
            yield return new WaitForSeconds(1);Application.Quit();
        }
        IEnumerator CaptureSystems()
        {
            yield return new WaitForSeconds(2);
            Combat.enabled=false;Sector.enabled=false;
            var camera=Camera.main;camera.GetComponent<ChaseCamera>().enabled=false;
            GetComponent<UIDocument>().enabled=false;
            camera.fieldOfView=85;
            for(int system=0;system<GalaxyCatalog.Systems.Length;system++)
            {
                Vector3 star=GalaxyCatalog.StarCenters[system];
                Vector3 viewpoint=star+new Vector3(0,3500,-6500);
                Player.transform.position=viewpoint+(viewpoint-star).normalized*2000;
                camera.transform.SetPositionAndRotation(viewpoint,Quaternion.LookRotation(star-viewpoint));
                Orbits.SetTime(0);
                yield return new WaitForSeconds(1);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../system-"+system+"-epoch.png"));
                yield return new WaitForSeconds(1);
                Orbits.SetTime(1200);
                yield return new WaitForSeconds(1);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../system-"+system+"-advanced.png"));
                yield return new WaitForSeconds(1);
                camera.fieldOfView=55;
                Vector3 stellarView=star+new Vector3(0,.35f,-1)*GalaxyCatalog.StarRadius*3.6f;
                camera.transform.SetPositionAndRotation(stellarView,Quaternion.LookRotation(star-stellarView));
                yield return new WaitForSeconds(1);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../system-"+system+"-star.png"));
                yield return new WaitForSeconds(1);camera.fieldOfView=85;
            }
            Debug.Log($"ORBIT_SYSTEM_CAPTURE: systems={GalaxyCatalog.Systems.Length}, worlds={Worlds.Length}, finalPhase={Orbits.Elapsed:0}");
            Application.Quit();
        }
        IEnumerator CaptureSurface()
        {
            yield return new WaitForSeconds(2);
            foreach(string id in new[]{"solace-01","veil-02","veil-03","cinder-02","veil-04","solace-04","veil-01","solace-03"})
            {
                var destination=Sector.Planet(id);
                if(Discovery.Catalog.State(id)==DiscoveryState.Unknown)Discovery.Catalog.Detect(id);
                if(Discovery.Catalog.State(id)==DiscoveryState.Detected)Discovery.Catalog.CompleteScan(id);
                Surface.Enter(destination);
                if(id=="veil-03")Surface.SetExplorerPose(new Vector3(48,0,32),30,10);
                yield return new WaitForSeconds(2);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../surface-"+id+".png"));
                yield return new WaitForSeconds(1);
                if(id=="solace-01")
                {
                    Vector2 relic=SurfaceRules.RelicPositions(id,settings.galaxySeed)[0];
                    Surface.SetExplorerPose(new Vector3(relic.x,0,relic.y-3),0,14);
                    yield return new WaitForSeconds(1);
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../surface-relic.png"));
                    Surface.TryCollect();
                    yield return new WaitForSeconds(1);
                }
                Surface.Launch();
            }
            var archive=new GameArchive(System.IO.Path.Combine(Application.dataPath,"../qa-surface-v2.json"),Session.Validate);
            archive.Save(Session.Snapshot());
            Debug.Log($"ORBIT_SURFACE_CAPTURE: worlds={Worlds.Length}, relics={Surface.RelicsFound}, saveValid={Session.Validate(archive.Load(out _))}, returnedToOrbit={!Surface.Active}");
            Application.Quit();
        }
        IEnumerator CaptureSector()
        {
            yield return new WaitForSeconds(2);
            var frontier=Sector.Planet("veil-01");Player.transform.SetPositionAndRotation(frontier.transform.position+new Vector3(230,100,-700),Quaternion.LookRotation(new Vector3(-230,-100,700)));
            yield return new WaitForSeconds(3);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../sector-frontier.png"));yield return new WaitForSeconds(1);
            Player.transform.SetPositionAndRotation(SectorController.AnomalyPositions[0]+Vector3.back*120,Quaternion.identity);yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../sector-anomaly.png"));
            Sector.enabled=false;
            float elapsed=0;while(elapsed<sectorSettings.anomalySeconds+.1f){Sector.InvestigateTick(Time.deltaTime,true);elapsed+=Time.deltaTime;yield return null;}
            Sector.enabled=true;
            var home=Worlds[0];Discovery.SelectTarget(home);Sector.ProtectSelected();Player.transform.SetPositionAndRotation(home.transform.position+new Vector3(450,150,-1100),Quaternion.LookRotation(new Vector3(-450,-150,1100)));
            yield return new WaitForSeconds(3);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../sector-protection.png"));yield return new WaitForSeconds(1);
            Sector.ToggleMap();yield return new WaitForSecondsRealtime(1);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../sector-map.png"));yield return new WaitForSecondsRealtime(1);Sector.ToggleMap();
            Combat.enabled=false;Sector.enabled=false;var enemy=Sector.Planet("cinder-01");Discovery.Catalog.Detect(enemy.Definition.id);Discovery.Catalog.CompleteScan(enemy.Definition.id);Discovery.SelectTarget(enemy);
            Player.transform.SetPositionAndRotation(enemy.transform.position+Vector3.back*(enemy.Definition.radius+170),Quaternion.identity);
            foreach(var actor in Combat.Actors)if(actor!=null&&actor.HomePlanet==enemy.Definition.id)Combat.ApplyDamage(actor,10000,CombatFaction.Union);
            yield return new WaitForSeconds(2);elapsed=0;
            while(!Empire.State.Get(enemy.Definition.id).owned&&elapsed<sectorSettings.captureSeconds+1)
            {
                Sector.CaptureTick(enemy.Definition.id,true,Time.deltaTime);elapsed+=Time.deltaTime;
                if(elapsed>5&&elapsed-Time.deltaTime<=5)ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../sector-capture.png"));yield return null;
            }
            yield return new WaitForSeconds(2);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../sector-conquest.png"));yield return new WaitForSeconds(1);
            var snapshot=Session.Snapshot();var archive=new GameArchive(System.IO.Path.Combine(Application.dataPath,"../qa-flight-v2.json"),Session.Validate);archive.Save(snapshot);
            Debug.Log($"ORBIT_SECTOR_CAPTURE: planets={Worlds.Length}, captured={Empire.State.Get("cinder-01").owned}, anomaly={Sector.State.Data.anomalies.Contains("ruins")}, fullSaveValid={Session.Validate(archive.Load(out _))}");
            foreach(string id in new[]{"veil-03","cinder-02","veil-04","solace-04"})
            {
                var p=Sector.Planet(id);Discovery.Catalog.Detect(id);Discovery.Catalog.CompleteScan(id);Discovery.SelectTarget(p);
                Vector3 offset=new Vector3(p.Definition.radius*.7f,p.Definition.radius*.25f,-p.Definition.radius*3);
                Player.transform.SetPositionAndRotation(p.transform.position+offset,Quaternion.LookRotation(-offset));
                yield return new WaitForSeconds(3);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../sector-"+id+".png"));yield return new WaitForSeconds(1);
            }
            Application.Quit();
        }
        IEnumerator CaptureCombat()
        {
            Player.transform.SetPositionAndRotation(Combat.OutpostPosition+new Vector3(0,25,-420),Quaternion.identity);
            yield return new WaitForSeconds(3);Combat.SelectTarget();
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../combat-contact.png"));
            yield return new WaitForSeconds(2);
            float elapsed=0;bool engagedCapture=false,explosionCapture=false;
            while(elapsed<18&&Combat.OutpostRemaining>0&&Combat.Player.Vitals.Alive)
            {
                if(Combat.Target==null)Combat.SelectTarget();
                if(Combat.Target!=null)Player.transform.rotation=Quaternion.LookRotation(Combat.Target.transform.position-Player.transform.position);
                int before=Combat.OutpostRemaining;bool fired=Combat.FirePlayer();elapsed+=Time.deltaTime;
                if(elapsed>2&&fired&&!engagedCapture){engagedCapture=true;ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../combat-engaged.png"));}
                if(Combat.OutpostRemaining<before&&!explosionCapture){explosionCapture=true;ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../combat-explosion.png"));}
                yield return null;
            }
            yield return new WaitForSeconds(1);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../combat-result.png"));
            Debug.Log($"ORBIT_COMBAT_CAPTURE: hostiles={Combat.OutpostRemaining}, playerAlive={Combat.Player.Vitals.Alive}, hull={Combat.Player.Vitals.Hull}");
            yield return new WaitForSeconds(1);Application.Quit();
        }
        void CreateAsteroids(Material material)
        {
            var random=new System.Random(settings.galaxySeed);
            for(int i=0;i<110;i++)
            {
                float a=(float)random.NextDouble()*Mathf.PI*2,r=550+(float)random.NextDouble()*250;
                Vector3 position=new Vector3(1700+Mathf.Cos(a)*r,-180+(float)random.NextDouble()*160,4500+Mathf.Sin(a)*r);
                float size=3+(float)random.NextDouble()*13;
                var rock=VisualFactory.Shape("Distant asteroid",PrimitiveType.Sphere,transform,position,new Vector3(size,size*.65f,size*.85f),material);
                rock.transform.rotation=Quaternion.Euler(i*37,i*19,i*11);
                rock.AddComponent<SphereCollider>().radius=.55f;
            }
        }
        IEnumerator Capture()
        {
            yield return new WaitForSeconds(4);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../home-orbit.png"));
            yield return new WaitForSeconds(2);
            for(int i=1;i<Worlds.Length;i++)
            {
                var target=Worlds[i].Definition;
                // Screenshot-only staging; ordinary gameplay always travels through space.
                Vector3 center=Worlds[i].transform.position;
                Player.transform.position=center+new Vector3(target.radius*.7f,target.radius*.3f,-target.radius*(i==1?2.2f:3.5f));
                Player.transform.rotation=Quaternion.LookRotation(center-Player.transform.position);
                yield return new WaitForSeconds(2);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../world-"+i+".png"));
                yield return new WaitForSeconds(1);
                if(i==1)
                {
                    Discovery.SelectTarget(Worlds[i]);Discovery.enabled=false;
                    float elapsed=0;
                    while(!Discovery.Catalog.HasDetails(target.id))
                    {
                        Discovery.Tick(Time.deltaTime,true);elapsed+=Time.deltaTime;
                        if(elapsed>=discoverySettings.scanSeconds*.5f&&elapsed-Time.deltaTime<discoverySettings.scanSeconds*.5f)
                            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../scan-in-progress.png"));
                        yield return null;
                    }
                    Discovery.enabled=true;yield return new WaitForSeconds(1);
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../discovery-card.png"));yield return new WaitForSeconds(1);
                    Player.transform.position=Worlds[i].transform.position+(Player.transform.position-Worlds[i].transform.position).normalized*(target.radius+150);
                    yield return new WaitForSeconds(2);
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../visited-card.png"));yield return new WaitForSeconds(1);
                    Discovery.ToggleJournal();yield return new WaitForSecondsRealtime(1);
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../exploration-log.png"));yield return new WaitForSecondsRealtime(1);
                    Discovery.ToggleJournal();
                    Empire.Colonize();
                    yield return new WaitForSeconds(4);
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../colony-deployment.png"));
                    yield return new WaitForSeconds(9);
                    Empire.ToggleManagement();yield return new WaitForSecondsRealtime(1);
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../colony-management.png"));yield return new WaitForSecondsRealtime(1);
                    Empire.Build(BuildingKind.Energy);Empire.CloseManagement();
                    yield return new WaitForSeconds(19);
                    Empire.ToggleManagement();Empire.Build(BuildingKind.Defense);Empire.CloseManagement();
                    yield return new WaitForSeconds(19);
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../colony-developed.png"));yield return new WaitForSeconds(1);
                    Empire.ToggleManagement();yield return new WaitForSecondsRealtime(1);
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../colony-infrastructure.png"));yield return new WaitForSecondsRealtime(1);
                    Empire.CloseManagement();
                }
            }
            Application.Quit();
        }
    }
}

