using System;
using System.Collections.Generic;
using UnityEngine;
namespace OrbitDominion
{
    [DefaultExecutionOrder(90)]
    public sealed class SectorController : MonoBehaviour
    {
        public SectorState State {get;private set;}
        public string Message {get;private set;}="M / Sector chart · G / Capture · H / Protection · I / Investigate";
        public bool MapOpen {get;private set;}
        public int CurrentSystem=>GalaxyCatalog.SystemAt(world.Player.transform.position);
        public static readonly string[] AnomalyIds={"ruins","wreck","rift"};
        public static readonly Vector3[] AnomalyPositions={new(15700,1300,7800),new(21300,1000,12600),new(33700,-500,14700)};
        PrototypeBootstrap world;
        float aiTick,repairTick;
        public void Initialize(PrototypeBootstrap world)
        {
            this.world=world;var ids=new List<string>();foreach(var p in world.Worlds)ids.Add(p.Definition.id);State=new SectorState(ids,world.sectorSettings);
            foreach(var p in world.Worlds)
            {
                var colony=world.Empire.State.Get(p.Definition.id);
                if(colony.faction<2)continue;
                var command=world.Combat.SpawnHostile(p.Definition.id+"/command",CombatRole.Command,p.transform.position+Vector3.back*(p.Definition.radius+110));command.HomePlanet=p.Definition.id;
                if(colony.faction==2)command.SetFaction(CombatFaction.Neutral);
                if(colony.faction==3)for(int i=0;i<2;i++)
                {
                    var fighter=world.Combat.SpawnHostile(p.Definition.id+"/patrol/"+i,CombatRole.Fighter,command.transform.position+new Vector3(i==0?-90:90,35,-70));fighter.HomePlanet=p.Definition.id;
                }
            }
        }
        public PlanetView Planet(string id)=>Array.Find(world.Worlds,p=>p.Definition.id==id);
        public bool FleetTargetsPlanet(string fleetId,string planetId)
        {
            if(string.IsNullOrEmpty(fleetId))return false;
            if(fleetId=="contact")return planetId=="veil-03";
            foreach(var order in State.Data.fleets)if(order.id==fleetId)return order.target==planetId;
            return false;
        }
        public void RefreshFleetDestinations()
        {
            foreach(var order in State.Data.fleets)
            {
                if(order.finished)continue;
                var target=Planet(order.target);
                if(target==null)continue;
                for(int i=0;i<order.ships.Count;i++)
                {
                    var actor=world.Combat.Find(order.ships[i]);
                    if(actor==null||!actor.Traveling)continue;
                    Vector3 direction=(target.transform.position-actor.transform.position).normalized;
                    actor.Destination=target.transform.position-direction*(target.Definition.radius+130)+Vector3.up*(i*35);
                }
            }
            var contact=Planet("veil-03");
            if(contact==null)return;
            for(int i=0;i<2;i++)
            {
                var actor=world.Combat.Find("contact/"+i);
                if(actor!=null&&actor.Traveling)actor.Destination=contact.transform.position+Vector3.back*650+Vector3.up*i*40;
            }
        }
        public bool IsProtected(string id)=>id!=null&&State.Get(id)?.protection>0;
        public int OwnedCount {get {int count=0;foreach(var c in world.Empire.State.Colonies)if(c.owned)count++;return count;}}
        public void ToggleMap()
        {
            if(!MapOpen){if(world.Empire.ManagementOpen)world.Empire.CloseManagement();if(world.Discovery.JournalOpen)world.Discovery.ToggleJournal();}
            MapOpen=!MapOpen;world.Player.SetPause(MapOpen);
        }
        public void Navigate(string id){State.Data.waypoint=id;State.Data.waypointSystem=-1;if(Planet(id)!=null)world.Discovery.SelectTarget(Planet(id));}
        public void NavigateSystem(int system){State.Data.waypoint=null;State.Data.waypointSystem=system;}
        public void ProtectSelected()
        {
            string id=world.Empire.ManagementOpen?world.Empire.ManagedId:world.Discovery.Target?.Definition.id;
            State.Data.protectionReviewed=true;
            string block=State.ProtectionBlock(id,world.Empire.State);Message=block??"PROTECTION ACTIVE / 45 seconds; defense weapons suspended";
            if(block==null)State.Protect(id,world.Empire.State);
            world.Session?.SaveNow();
        }
        public string CaptureHint(string id)
        {
            var c=world.Empire.State.Get(id);if(c==null||c.faction<2)return "";
            if(!world.Discovery.Catalog.HasDetails(id))return "";
            int fighters=0,satellites=0,commands=0;
            foreach(var a in world.Combat.Actors)if(a!=null&&a.Vitals.Alive&&a.HomePlanet==id&&a.Faction!=CombatFaction.Union)
            {if(a.Role==CombatRole.Fighter)fighters++;else if(a.Role==CombatRole.Satellite)satellites++;else if(a.Role==CombatRole.Command)commands++;}
            if(IsProtected(id))return "PLANET SHIELDED / Wait for protection to expire";
            if(world.Combat.LocalDefenders(id,CombatFaction.Union))return $"1 / CLEAR ORBITAL DEFENSES · FIGHTERS {fighters} · SATELLITES {satellites} · COMMAND {commands}\nR selects an enemy · LMB fires · Keep your target in weapon range";
            float range=Mathf.Max(0,Vector3.Distance(world.Player.transform.position,Planet(id).transform.position)-Planet(id).Definition.radius);
            if(world.Player.Pulse)return "2 / DISENGAGE PULSE · P · Then approach the planet";
            if(range>world.sectorSettings.captureRange)return $"2 / APPROACH TO {world.sectorSettings.captureRange:0} m · CURRENT {range:0} m";
            return $"3 / HOLD G TO TAKE CONTROL · {100*State.Get(id).capture/world.sectorSettings.captureSeconds:0}%\nOr L to land, defeat the surface guards and capture their terminal";
        }
        public bool SurfaceCaptureTick(string id,bool held,bool inRange,bool groundDefenders,float dt)
        {
            if(world.Player.Paused||world.Surface?.Active!=true||!world.Surface.OnFoot||world.Surface.PlanetId!=id||
                !world.Surface.GroundBattle.Suit.Alive||world.Empire.State.Get(id)?.faction!=3)return false;
            bool ready=State.Capture(id,held,world.Discovery.Catalog.HasDetails(id),inRange,
                groundDefenders||world.Combat.LocalDefenders(id,CombatFaction.Union),false,dt);
            if(!ready)return false;
            world.Combat.DisableMilitary(id);world.Empire.State.ChangeOwner(id,1);State.Get(id).provoked=false;
            Message=Planet(id).Definition.title+" SECURED / UNION CONTROL · B to develop";
            world.Session?.SaveNow();return true;
        }
        public bool CaptureTick(string id,bool held,float dt)
        {
            if(world.Player.Paused||world.Surface?.Active==true)return false;
            var colony=world.Empire.State.Get(id);var planet=Planet(id);if(colony==null||colony.faction<2||planet==null)return false;
            bool ready=State.Capture(id,held,world.Discovery.Catalog.HasDetails(id),Vector3.Distance(world.Player.transform.position,planet.transform.position)-planet.Definition.radius<=world.sectorSettings.captureRange,world.Combat.LocalDefenders(id,CombatFaction.Union),world.Player.Pulse,dt);
            if(!ready)return false;
            world.Combat.DisableMilitary(id);world.Empire.State.ChangeOwner(id,1);State.Get(id).provoked=false;Message=planet.Definition.title+" JOINED THE UNION / Civilian infrastructure preserved";world.Session?.SaveNow();return true;
        }
        public FleetOrder LaunchFleet(string source,string target)
        {
            if(world.Empire.State.Get(source)?.faction!=3||Planet(target)==null||target==world.Worlds[0].Definition.id)return null;
            var data=State.Data;int active=0;foreach(var f in data.fleets)if(!f.finished)active++;
            if(active>=world.sectorSettings.fleetLimit||data.enemyMinerals<world.sectorSettings.fleetMinerals||data.enemyEnergy<world.sectorSettings.fleetEnergy)return null;
            data.enemyMinerals-=world.sectorSettings.fleetMinerals;data.enemyEnergy-=world.sectorSettings.fleetEnergy;
            var fleet=new FleetOrder{id="fleet-"+(data.nextFleet++),source=source,target=target};data.fleets.Add(fleet);
            var from=Planet(source);var to=Planet(target);
            Vector3 direction=(to.transform.position-from.transform.position).normalized;
            for(int i=0;i<2;i++)
            {
                string id=fleet.id+"/"+i;fleet.ships.Add(id);
                var ship=world.Combat.SpawnHostile(id,CombatRole.Fighter,from.transform.position+direction*(from.Definition.radius+180)+Vector3.up*(i*35));
                ship.FleetId=fleet.id;ship.Traveling=true;ship.Destination=to.transform.position-direction*(to.Definition.radius+130)+Vector3.up*(i*35);
            }
            Message="FLEET LAUNCH DETECTED / "+(world.Discovery.Catalog.HasDetails(target)?to.Definition.title:"Unidentified world")+" under threat";return fleet;
        }
        public void TickFleets(float dt)
        {
            foreach(var f in State.Data.fleets)
            {
                if(f.finished)continue;int alive=0;bool traveling=false;
                foreach(string id in f.ships){var a=world.Combat.Find(id);if(a!=null&&a.Vitals.Alive){alive++;traveling|=a.Traveling;}}
                if(alive==0){f.finished=true;if(f.target=="solace-02")State.Data.raidDefeated=true;Message="HOSTILE FLEET DESTROYED / Colony approaches secure";continue;}
                f.arrived=!traveling;if(!f.arrived)continue;
                var c=world.Empire.State.Get(f.target);
                if(c.faction==3){f.finished=true;foreach(string id in f.ships){var actor=world.Combat.Find(id);if(actor!=null)actor.HomePlanet=f.target;}continue;}
                if(!IsProtected(f.target))State.Attacked(f.target);
                if(IsProtected(f.target)||world.Combat.LocalDefenders(f.target,CombatFaction.Hostile)){f.capture=0;continue;}
                var planet=Planet(f.target);
                if(world.Combat.Player.Vitals.Alive&&Vector3.Distance(world.Player.transform.position,planet.transform.position)<planet.Definition.radius+800){f.capture=0;continue;}
                State.Attacked(f.target);f.capture+=dt;
                if(f.capture>=world.sectorSettings.enemyCaptureSeconds)
                {
                    world.Combat.DisableMilitary(f.target);world.Empire.State.ChangeOwner(f.target,3);f.finished=true;
                    var command=world.Combat.SpawnHostile(f.id+"/occupation",CombatRole.Command,planet.transform.position+Vector3.back*(planet.Definition.radius+110));command.HomePlanet=f.target;
                    foreach(string id in f.ships){var actor=world.Combat.Find(id);if(actor!=null)actor.HomePlanet=f.target;}
                    Message="DOMINION OCCUPATION / Recover your colony by clearing its defenses";
                }
            }
            for(int i=State.Data.fleets.Count-1;i>=0;i--)
            {
                var f=State.Data.fleets[i];if(!f.finished)continue;bool dead=true;foreach(string id in f.ships)if(world.Combat.Find(id)?.Vitals.Alive==true)dead=false;
                if(dead){foreach(string id in f.ships)world.Combat.RemoveActor(id);State.Data.fleets.RemoveAt(i);}
            }
        }
        void Decide()
        {
            var data=State.Data;data.decision=world.sectorSettings.aiDecisionSeconds;
            string source=null;foreach(var c in world.Empire.State.Colonies)if(c.faction==3){source=c.id;break;}
            if(source==null)return;
            foreach(var c in world.Empire.State.Colonies)if(c.faction==3&&c.construction<0)
            {
                var kind=c.levels[3]==0?BuildingKind.Defense:c.levels[0]<2?BuildingKind.Mining:c.levels[1]<2?BuildingKind.Energy:c.Development<3?BuildingKind.Settlement:c.levels[4]==0?BuildingKind.Station:c.levels[0]<3?BuildingKind.Mining:BuildingKind.Energy;
                if(c.levels[(int)kind]==0&&c.UsedSlots>=c.Slots)kind=BuildingKind.Settlement;
                var cost=EmpireState.Cost(kind,c.levels[(int)kind]+1);
                if(data.enemyMinerals>=cost.x&&data.enemyEnergy>=cost.y&&world.Empire.State.QueueForeignBuilding(c.id,kind)){data.enemyMinerals-=cost.x;data.enemyEnergy-=cost.y;break;}
            }
            string target=null;
            foreach(var p in world.Worlds)
            {
                var c=world.Empire.State.Get(p.Definition.id);if(p.Definition.id=="solace-01"||c.faction==3||c.faction==2||p.Definition.survey.habitability<25||c.owned&&data.tutorial<16)continue;
                bool underway=false;foreach(var f in data.fleets)if(!f.finished&&f.target==c.id)underway=true;if(underway)continue;
                if(target==null||c.owned){target=c.id;if(c.owned)break;}
            }
            // Let the guided first colony develop before the first attack; expansion continues elsewhere.
            if(target!=null)LaunchFleet(source,target);
        }
        public void InvestigateTick(float dt,bool held)
        {
            string nearby=null;
            for(int i=0;i<3;i++)if(Vector3.Distance(world.Player.transform.position,AnomalyPositions[i])<world.sectorSettings.anomalyRange&&!State.Data.anomalies.Contains(AnomalyIds[i]))nearby=AnomalyIds[i];
            if(nearby==null||!held||world.Player.Pulse){State.Data.investigating=null;State.Data.investigation=0;return;}
            if(State.Data.investigating!=nearby){State.Data.investigating=nearby;State.Data.investigation=0;}
            State.Data.investigation+=dt;Message=$"ANALYSING SIGNAL / {100*State.Data.investigation/world.sectorSettings.anomalySeconds:0}%";
            if(State.Data.investigation>=world.sectorSettings.anomalySeconds){State.Data.anomalies.Add(nearby);State.Data.investigation=0;State.Data.investigating=null;world.Empire.State.Reward(220,180);Message="ANCIENT CACHE RECOVERED / +220 minerals · +180 energy";world.Session?.SaveNow();}
        }
        void Update()
        {
            bool onSurface=world.Surface?.Active==true;
            if(!onSurface&&world.Player.MapPressed)ToggleMap();
            if(MapOpen&&(!world.Player.Paused||world.Empire.ManagementOpen||world.Discovery.JournalOpen))MapOpen=false;
            if(!onSurface&&world.Player.ProtectPressed)ProtectSelected();
            if(world.Player.Paused)return;
            float dt=Time.deltaTime;var data=State.Data;
            State.Tick(dt,OwnedCount);data.hadFlight|=world.Player.DistanceTravelled>40;data.hadPulse|=world.Player.HasPulsed;
            data.frontierVisited|=CurrentSystem==1;data.hostileVisited|=CurrentSystem==2;
            string target=world.Discovery.Target?.Definition.id;
            foreach(var p in data.planets)if(p.id!=target)p.capture=0;
            if(!onSurface&&target!=null)CaptureTick(target,world.Player.CaptureHeld,dt);
            InvestigateTick(dt,!onSurface&&world.Player.InvestigateHeld);TickFleets(dt);
            aiTick+=dt;if(aiTick>=1)
            {
                float tick=aiTick;aiTick=0;
                foreach(var c in world.Empire.State.Colonies)if(c.faction==3){var income=world.Empire.State.WorldProduction(c.id);data.enemyMinerals=Mathf.Min(10000000,data.enemyMinerals+income.x*tick);data.enemyEnergy=Mathf.Min(10000000,data.enemyEnergy+income.y*tick);}
                data.decision=Mathf.Max(0,data.decision-tick);if(data.decision<=0)Decide();
                TickTutorial();
            }
            var neutral=Planet("veil-03");
            if(Vector3.Distance(world.Player.transform.position,neutral.transform.position)<neutral.Definition.radius+650&&world.Empire.State.Get("veil-03").faction==2)
            {data.contactSeen=true;Message=(world.Discovery.Catalog.HasDetails("veil-03")?"ITHACAN ASSEMBLY":"UNIDENTIFIED CIVILIZATION")+" / Respect our borders. We defend ourselves; we do not seek war.";}
            bool repair=world.Empire.State.Get("solace-01").owned&&Vector3.Distance(world.Player.transform.position,world.Worlds[0].transform.position)<world.Worlds[0].Definition.radius+300&&!world.Combat.ActiveThreat;
            repairTick=repair?repairTick+dt:0;if(repairTick>=3&&world.Combat.Player.Vitals.Alive){world.Combat.Player.Vitals.Reset();data.offensiveReady=true;repairTick=0;}
        }
        public void TickTutorial()
        {
            var d=State.Data;var a=world.Empire.State.Get("solace-02");var catalog=world.Discovery.Catalog;
            PlanetView enemySource=null;foreach(var p in world.Worlds)if(world.Empire.State.Get(p.Definition.id).faction==3){enemySource=p;break;}
            bool firstFight=true;foreach(var actor in world.Combat.Actors)if(actor!=null&&actor.Id.StartsWith("contact/")&&actor.Vitals.Alive)firstFight=false;
            if(d.contactSeen&&catalog.HasDetails("veil-03")&&world.Combat.Find("contact/0")==null&&enemySource!=null)
            {
                // Interceptors launch from an actual enemy world and travel into the frontier.
                var source=enemySource;for(int i=0;i<2;i++){var ship=world.Combat.SpawnHostile("contact/"+i,CombatRole.Fighter,source.transform.position+Vector3.back*(source.Definition.radius+180)+Vector3.up*i*40);ship.FleetId="contact";ship.Traveling=true;ship.Destination=Planet("veil-03").transform.position+Vector3.back*650+Vector3.up*i*40;}firstFight=false;
            }
            bool contactLaunched=world.Combat.Find("contact/0")!=null||enemySource==null;
            if(contactLaunched&&firstFight&&!d.firstRaid)
            {
                var raid=enemySource==null?null:LaunchFleet(enemySource.Definition.id,"solace-02");if(raid!=null){d.firstRaid=true;Navigate("solace-02");}
                else if(enemySource==null){d.firstRaid=true;d.raidDefeated=true;}
            }
            if(world.Empire.ManagementOpen&&world.Empire.ManagedId=="solace-02")d.protectionReviewed=true;
            bool[] done={d.hadFlight||catalog.HasDetails("solace-02"),catalog.BeaconReached,catalog.State("solace-02")>=DiscoveryState.Detected,d.hadPulse||catalog.HasDetails("solace-02"),catalog.HasDetails("solace-02")||world.Discovery.TargetRange<=world.discoverySettings.scanRange,catalog.HasDetails("solace-02"),a.owned,a.levels[1]>0,a.Development>=2,a.levels[3]>0,a.levels[3]>0,d.frontierVisited,d.anomalies.Count>0,catalog.HasDetails("veil-03"),d.contactSeen,contactLaunched&&firstFight,d.firstRaid,d.raidDefeated,d.protectionReviewed,d.firstRaid,d.offensiveReady||d.hostileVisited,d.hostileVisited,catalog.HasDetails("cinder-01"),!world.Combat.LocalDefenders("cinder-01",CombatFaction.Union),d.firstCaptured,d.firstCaptured,d.firstCaptured};
            while(d.tutorial<27&&done[d.tutorial])d.tutorial++;
        }
        public Vector3? Guidance
        {
            get
            {
                var d=State.Data;if(d.waypointSystem>=0)return GalaxyCatalog.Centers[d.waypointSystem];
                if(d.waypoint!=null){var p=Planet(d.waypoint);if(p!=null)return p.transform.position;int index=Array.IndexOf(AnomalyIds,d.waypoint);if(index>=0)return AnomalyPositions[index];}
                if(d.tutorial<2)return world.Beacon;if(d.tutorial<10)return Planet("solace-02").transform.position;
                if(d.tutorial<13)return AnomalyPositions[0];if(d.tutorial<16)return Planet("veil-03").transform.position;
                if(d.tutorial<19)return Planet("solace-02").transform.position;if(d.tutorial==20)return world.Worlds[0].transform.position;
                if(d.tutorial<27)return Planet("cinder-01").transform.position;return null;
            }
        }
        public static readonly string[] Objectives={"Test your ship / W S, mouse, Q E","Reach the navigation beacon","Investigate the unknown signal","Engage pulse / P","Enter scanner range / 650m","Hold F / Scan the unknown world","Establish Aurelia II / C within 220m","Build an Energy Complex / B","Upgrade Settlement to level 2","Build an orbital defense satellite","A distant star calls / M for sector chart","Travel to the Veil Frontier","Hold I near the anomalous signal","Scan the unidentified civilization","Observe first contact / respect their borders","Defeat the arriving Corsair interceptors","Hostile fleet inbound / return to Aurelia II","Defend Aurelia II alongside its satellites","Review planetary protection / B or H","Locate the source / Cinder Reach","Repair at Meridian before the offensive","Fly into Cinder Reach","Scan the enemy colony / F","Neutralize local fighters, satellites and command","Hold G in orbit / capture the enemy colony","Your empire grows / review captured production","THE FRONTIER IS OPEN / Continue exploring"};
        public string Objective=>State.Data.tutorial<27?Objectives[State.Data.tutorial]:"THE FRONTIER IS OPEN / Explore · build · defend · conquer";
    }
}
