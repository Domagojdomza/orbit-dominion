using UnityEngine;
using UnityEngine.UIElements;

namespace OrbitDominion
{
    public sealed class FlightHud : MonoBehaviour
    {
        public PrototypeBootstrap world;
        public Camera lens;
        Label speed,drive,objective,distance,marker,notice,sectorName;
        VisualElement menu, root;
        public void Initialize(UIDocument document)
        {
            root=document.rootVisualElement;
            root.styleSheets.Add(Resources.Load<StyleSheet>("FlightHud"));
            root.pickingMode=PickingMode.Ignore;
            var top=Box(root,"top");
            Label(top,"ORBIT  /  DOMINION","brand");sectorName=Label(top,"SOLACE SYSTEM     /     01","sector");
            var mission=Box(root,"mission");Label(mission,"FLIGHT OPERATIONS  /  001","eyebrow");
            objective=Label(mission,"Leave home orbit","objective");distance=Label(mission,"Reach the navigation beacon","detail");
            Label(root,"+","reticle");marker=Label(root,"◇","marker");
            var telemetry=Box(root,"telemetry");Label(telemetry,"WAYFARER   /   EXPLORER CLASS","eyebrow");
            speed=Label(telemetry,"000","speed");Label(telemetry,"METRES / SECOND","eyebrow");drive=Label(telemetry,"FLIGHT ASSIST ONLINE","drive");
            var bottom=Box(root,"bottom");Label(bottom,"W S   THRUST     ·     MOUSE / ARROWS   STEER     ·     Q E   ROLL","controls");
            Label(bottom,"SHIFT  BOOST · SPACE  BRAKE · P  PULSE · F  SCAN · TAB  TARGET · J  LOG · C  COLONIZE · B  BUILD","controls");
            notice=Label(root,"PHASE 02   /   DISCOVERY","notice");
            menu=Box(root,"menu");Label(menu,"FLIGHT PAUSED","objective");
            Label(menu,"Fly toward unknown signals and hold F to scan a world. Approach a scanned planet slowly and press L to land, explore on foot and recover relics.\n\nB opens colony management; J opens your exploration log. F5 saves the full flight, including surface progress.","menu-copy");
            menu.Add(new Button(()=>world.Player.SetPause(false)){text="RESUME FLIGHT"});
            menu.Add(new Button(()=>{world.Player.ReturnHome();world.Player.SetPause(false);}){text="RETURN TO HOME ORBIT"});
            menu.Add(new Button(()=>world.Discovery.ToggleJournal()){text="EXPLORATION LOG"});
            menu.Add(new Button(()=>world.Empire.ToggleManagement()){text="MANAGE SELECTED COLONY / B"});
            menu.Add(new Button(()=>Application.Quit()){text="QUIT"});
            gameObject.AddComponent<SurveyPanel>().Initialize(world,root);
            gameObject.AddComponent<ColonyPanel>().Initialize(world,root);
            gameObject.AddComponent<CombatPanel>().Initialize(world,lens,root);
            gameObject.AddComponent<SectorPanel>().Initialize(world,lens,root);
            gameObject.AddComponent<SurfaceHud>().Initialize(world,root);
        }
        static VisualElement Box(VisualElement p,string cls){var e=new VisualElement();e.AddToClassList(cls);p.Add(e);return e;}
        static Label Label(VisualElement p,string text,string cls){var l=new Label(text);l.AddToClassList(cls);l.pickingMode=PickingMode.Ignore;p.Add(l);return l;}
        void Update()
        {
            if(world.Player==null||root==null)return;
            var player=world.Player;
            var discovery=world.Discovery;
            sectorName.text=GalaxyCatalog.Systems[world.Sector.CurrentSystem]+" / SECTOR "+(world.Sector.CurrentSystem+1);
            menu.style.display=player.Paused&&!discovery.JournalOpen&&!world.Empire.ManagementOpen&&!world.Sector.MapOpen?DisplayStyle.Flex:DisplayStyle.None;
            speed.text=player.Speed.ToString("000");drive.text=player.Pulse?"PULSE DRIVE  /  ACTIVE":player.Boosting?"BOOST  /  ACTIVE":player.Status;
            bool acquired=discovery.Target!=null;
            var first=discovery.Catalog.State(world.Worlds[1].Definition.id);
            objective.text=first>=DiscoveryState.Scanned?"The frontier is opening":acquired?"Investigate the unknown world":"Leave home orbit";
            if(acquired&&discovery.Target==world.Worlds[0])objective.text="Home orbit / Meridian";
            var colony=world.Empire.State.Get(world.Worlds[1].Definition.id);
            if(first>=DiscoveryState.Scanned)objective.text=colony.owned?(colony.levels[1]>0?"Develop your civilization":"Build an Energy Complex"):"Establish your first colony";
            Vector3 target=acquired?discovery.Target.transform.position:world.Beacon;
            float meters=acquired?discovery.TargetRange:Vector3.Distance(player.transform.position,target);
            distance.text=first>=DiscoveryState.Scanned?"J / Review discoveries. Explore another signal.":acquired?discovery.ScanHint:"NAVIGATION BEACON     ·     "+meters.ToString("N0")+" m";
            if(first>=DiscoveryState.Scanned)distance.text=colony.owned?"B / Manage an owned world. Resume flight to build.":"Approach Aurelia II within 220 m. Press C to deploy.";
            if(world.Combat.ActiveThreat){objective.text="Neutralize the Corsair patrol";distance.text="LMB / Fire energy weapon · R / Select hostile · Keep moving";}
            if(!world.Combat.ActiveThreat){objective.text=world.Sector.Objective;distance.text=$"{GalaxyCatalog.Systems[world.Sector.CurrentSystem]} · M / Sector chart · I / Investigate · G / Capture";}
            Vector3 point=lens.WorldToViewportPoint(target);
            bool behind=point.z<0;if(behind){point.x=1-point.x;point.y=1-point.y;}
            float width=root.resolvedStyle.width,height=root.resolvedStyle.height;
            marker.style.left=Mathf.Clamp(point.x*width,35,width-210);marker.style.top=Mathf.Clamp((1-point.y)*height,120,height-160);
            marker.text=(behind?"◁  ":"◇  ")+(acquired?discovery.TargetLabel:"NAV BEACON");
            notice.text=world.Surface.LandingHint.Length>0?world.Surface.LandingHint:player.Clearance<world.settings.pulseExclusion?"GRAVITY WELL   /   PULSE DRIVE INHIBITED":world.Session.Status;
        }
    }
}
