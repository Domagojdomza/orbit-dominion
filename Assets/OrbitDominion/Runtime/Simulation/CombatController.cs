using System.Collections.Generic;
using UnityEngine;
namespace OrbitDominion
{
    [DefaultExecutionOrder(80)]
    public sealed class CombatController : MonoBehaviour
    {
        readonly List<CombatActor> actors=new();
        readonly Dictionary<string,VitalState> satelliteStates=new();
        readonly Dictionary<string,float> satelliteRepairs=new();
        readonly Dictionary<string,ActorSave> pendingSatellites=new();
        PrototypeBootstrap world;
        CombatSettings settings;
        CombatEffects effects;
        Material metal,red;
        float warning,respawn,protection;
        public CombatActor Player {get;private set;}
        public CombatActor Target {get;private set;}
        public Vector3 OutpostPosition {get;private set;}
        public string Message {get;private set;}="LMB / Energy weapon     R / Select hostile";
        public bool Engaged {get;private set;}
        public bool ActiveThreat {get;private set;}
        public float RespawnRemaining=>respawn;
        public int ShotsFired {get;private set;}
        public int HostilesRemaining {get {int count=0;foreach(var actor in actors)if(actor!=null&&actor.Faction==CombatFaction.Hostile&&actor.Vitals.Alive)count++;return count;}}
        public int OutpostRemaining {get {int count=0;foreach(var a in actors)if(a!=null&&a.Id.StartsWith("Corsair ")&&a.Vitals.Alive)count++;return count;}}
        public IReadOnlyList<CombatActor> Actors=>actors;
        public CombatActor Find(string id)=>actors.Find(a=>a!=null&&a.Id==id);
        public void FollowPlanetOrbit(PlanetView planet,Vector3 shift)
        {
            string id=planet.Definition.id;
            Vector3 oldCenter=planet.transform.position-shift;
            Vector3 oldOutpost=OutpostPosition;
            if(id=="solace-03")OutpostPosition+=shift;
            foreach(var actor in actors)
            {
                if(actor==null||actor==Player||actor.transform.IsChildOf(planet.transform)||actor.Traveling)continue;
                bool stationed=actor.HomePlanet==id && (actor.transform.position-oldCenter).sqrMagnitude<2250000;
                bool corsair=id=="solace-03"&&actor.Id.StartsWith("Corsair ")&&(actor.transform.position-oldOutpost).sqrMagnitude<2250000;
                bool arrivedFleet=world.Sector?.FleetTargetsPlanet(actor.FleetId,id)==true && (actor.transform.position-oldCenter).sqrMagnitude<2250000;
                if(!stationed&&!corsair&&!arrivedFleet)continue;
                actor.transform.position+=shift;
                actor.Home+=shift;
            }
        }
        public void RemoveActor(string id){var actor=Find(id);if(actor==null||actor==Player)return;actors.Remove(actor);Destroy(actor.gameObject);}
        public bool LocalDefenders(string id,CombatFaction attacker)
        {
            foreach(var a in actors)if(a!=null&&a.Vitals.Alive&&a.Faction!=attacker&&a.Role!=CombatRole.Player&&(a.HomePlanet==id&&(a.Role!=CombatRole.Fighter||Vector3.Distance(a.transform.position,world.Sector.Planet(id).transform.position)<1500)||a.FleetId!=null&&Vector3.Distance(a.transform.position,world.Sector.Planet(id).transform.position)<900))return true;
            return false;
        }
        public void Provoke(string id)
        {
            if(world.Sector!=null)world.Sector.State.Get(id).provoked=true;
            foreach(var a in actors)if(a!=null&&a.HomePlanet==id&&a.Faction==CombatFaction.Neutral)a.SetFaction(CombatFaction.Hostile);
        }
        public void DisableMilitary(string id)
        {
            foreach(var a in actors)if(a!=null&&a.HomePlanet==id){a.Vitals.Damage(100000, a.Faction==CombatFaction.Union?CombatFaction.Hostile:CombatFaction.Union,a.Faction);a.RepairRemaining=float.MaxValue;}
        }
        public void Initialize(PrototypeBootstrap world)
        {
            this.world=world;settings=world.combatSettings;
            effects=gameObject.AddComponent<CombatEffects>();effects.Initialize();
            metal=VisualFactory.Lit(new Color(.25f,.08f,.08f),.75f);red=VisualFactory.Lit(new Color(.8f,.08f,.02f),.4f,new Color(2,.12f,.01f));
            Player=world.Player.gameObject.AddComponent<CombatActor>();Player.Initialize("wayfarer",CombatFaction.Union,CombatRole.Player,new VitalState(settings.playerHull,settings.playerShield),7);actors.Add(Player);
            OutpostPosition=world.Worlds[2].transform.position+new Vector3(700,120,-720);
            SpawnHostile("Corsair command",CombatRole.Command,OutpostPosition);
            SpawnHostile("Corsair sentry",CombatRole.Satellite,OutpostPosition+new Vector3(100,40,-90));
            SpawnHostile("Corsair interceptor 01",CombatRole.Fighter,OutpostPosition+new Vector3(-100,0,-160));
            SpawnHostile("Corsair interceptor 02",CombatRole.Fighter,OutpostPosition+new Vector3(130,-30,-220));
        }
        public CombatActor SpawnHostile(string id,CombatRole role,Vector3 position)
        {
            var root=new GameObject(id).transform;root.SetParent(transform);root.position=position;
            if(role==CombatRole.Fighter)VisualFactory.Ship(root,metal,red,2);
            else
            {
                VisualFactory.Shape("Armored core",PrimitiveType.Cube,root,Vector3.zero,role==CombatRole.Command?new Vector3(22,18,25):Vector3.one*9,metal);
                VisualFactory.Ring("Hostile power ring",root,role==CombatRole.Command?27:13,1.8f,red);
                VisualFactory.Shape("Weapon spine",PrimitiveType.Cylinder,root,Vector3.zero,new Vector3(4,role==CombatRole.Command?25:12,4),red);
            }
            root.gameObject.AddComponent<SphereCollider>().radius=role==CombatRole.Command?29:12;
            var actor=root.gameObject.AddComponent<CombatActor>();actor.Initialize(id,CombatFaction.Hostile,role,new VitalState(role==CombatRole.Command?240:role==CombatRole.Satellite?100:65,role==CombatRole.Command?100:30),role==CombatRole.Command?29:12);actors.Add(actor);return actor;
        }
        public void RegisterSatellite(GameObject satellite,string id,CombatFaction faction=CombatFaction.Union)
        {
            actors.RemoveAll(a=>a==null||a.Id==id);
            if(!satelliteStates.TryGetValue(id,out var vitals)){vitals=new VitalState(150,70);satelliteStates.Add(id,vitals);}
            var actor=satellite.AddComponent<CombatActor>();actor.Initialize(id,faction,CombatRole.Satellite,vitals,12);actor.HomePlanet=id.Split('/')[0];
            if(satelliteRepairs.TryGetValue(id,out float repair))actor.RepairRemaining=repair;
            actors.Add(actor);
            if(pendingSatellites.TryGetValue(id,out var saved)){ApplySaved(actor,saved);satelliteStates[id]=actor.Vitals;satelliteRepairs[id]=saved.repair;pendingSatellites.Remove(id);}
        }
        public CombatSave Snapshot()
        {
            var save=new CombatSave{respawn=Mathf.Max(0,respawn),protection=protection,warning=warning,engaged=Engaged,target=Target?.Id};
            foreach(var a in actors)if(a!=null)save.actors.Add(new ActorSave{id=a.Id,homePlanet=a.HomePlanet,fleetId=a.FleetId,faction=a.Faction,role=a.Role,position=a.transform.position,rotation=a.transform.rotation,destination=a.Destination,hull=a.Vitals.Hull,shield=a.Vitals.Shield,sinceHit=a.Vitals.SinceHit,maxHull=a.Vitals.MaxHull,maxShield=a.Vitals.MaxShield,cooldown=a.Cooldown,repair=Mathf.Min(100000,a.RepairRemaining),traveling=a.Traveling});
            foreach(var pair in pendingSatellites)if(!save.actors.Exists(a=>a.id==pair.Key))save.actors.Add(pair.Value);
            return save;
        }
        public void Restore(CombatSave save)
        {
            pendingSatellites.Clear();satelliteStates.Clear();satelliteRepairs.Clear();
            foreach(var a in actors)if(a!=null&&a!=Player&&!a.Id.Contains("/defense/")){a.gameObject.SetActive(false);Destroy(a.gameObject);}
            actors.RemoveAll(a=>a==null||a!=Player&&!a.Id.Contains("/defense/"));
            foreach(var s in save.actors)
            {
                CombatActor actor=s.id==Player.Id?Player:Find(s.id);
                if(actor==null&&s.id.Contains("/defense/")){pendingSatellites[s.id]=s;continue;}
                if(actor==null)actor=SpawnHostile(s.id,s.role,s.position);
                ApplySaved(actor,s);
                if(s.id.Contains("/defense/")){satelliteStates[s.id]=actor.Vitals;satelliteRepairs[s.id]=s.repair;}
            }
            respawn=save.respawn;protection=save.protection;warning=save.warning;Engaged=save.engaged;Target=Find(save.target);world.Player.FlightDisabled=!Player.Vitals.Alive;
        }
        void ApplySaved(CombatActor actor,ActorSave s)
        {
            actor.Initialize(s.id,s.faction,s.role,new VitalState(s.maxHull,s.maxShield),actor.Radius);
            actor.Vitals.Restore(s.hull,s.shield,s.sinceHit);actor.transform.SetPositionAndRotation(s.position,s.rotation);
            actor.HomePlanet=string.IsNullOrEmpty(s.homePlanet)?null:s.homePlanet;actor.FleetId=string.IsNullOrEmpty(s.fleetId)?null:s.fleetId;actor.Destination=s.destination;actor.Traveling=s.traveling;actor.Cooldown=s.cooldown;actor.RepairRemaining=s.repair;
        }
        public void SelectTarget()
        {
            int current=actors.IndexOf(Target);
            for(int i=1;i<=actors.Count;i++)
            {
                var candidate=actors[(current+i)%actors.Count];
                if(candidate!=null&&candidate.Faction==CombatFaction.Hostile&&candidate.Vitals.Alive&&Vector3.Distance(Player.transform.position,candidate.transform.position)<1800){Target=candidate;return;}
            }
            Target=null;
        }
        float Obstruction(Vector3 origin,Vector3 direction,float range,CombatActor source)
        {
            float nearest=range;
            foreach(var planet in world.Worlds)nearest=Mathf.Min(nearest,CombatRules.SphereHit(origin,direction,planet.transform.position,planet.Definition.radius));
            foreach(var hit in Physics.RaycastAll(origin,direction,range,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                if(hit.collider.GetComponentInParent<CombatActor>()==null)nearest=Mathf.Min(nearest,hit.distance);
            return nearest;
        }
        public bool Fire(CombatActor source,Vector3 direction)
        {
            if(source==null||world.Sector!=null&&world.Sector.IsProtected(source.HomePlanet)||!CombatRules.CanFire(source.Vitals.Alive,world.Player.Paused,source==Player&&world.Player.Pulse,source.Cooldown)||direction.sqrMagnitude<.001f)return false;
            direction.Normalize();bool hostile=source.Faction==CombatFaction.Hostile;
            float range=source==Player?settings.weaponRange:settings.hostileRange;
            Vector3 origin=source.transform.position;
            float nearest=Obstruction(origin,direction,range,source);CombatActor victim=null;
            foreach(var actor in actors)
            {
                if(actor==null||actor==source||!actor.Vitals.Alive)continue;
                float hit=CombatRules.SphereHit(origin,direction,actor.transform.position,actor.Radius);
                if(hit<nearest){nearest=hit;victim=actor;}
            }
            source.Cooldown=source==Player?settings.playerFireInterval:hostile?settings.hostileFireInterval:settings.satelliteFireInterval;
            float damage=source==Player?settings.playerDamage:hostile?settings.hostileDamage:settings.satelliteDamage;
            effects.Beam(origin+direction*(source.Radius+1),origin+direction*nearest,hostile);ShotsFired++;
            if(victim!=null)ApplyDamage(victim,damage,source.Faction);
            return true;
        }
        public void ApplyDamage(CombatActor actor,float amount,CombatFaction attacker)
        {
            if(world.Player.Paused)return;
            if(actor==Player&&world.Surface?.Active==true)return; // The ship is parked outside the surface instance.
            if(world.Sector!=null&&world.Sector.IsProtected(actor.HomePlanet))return;
            if(actor.Faction==CombatFaction.Neutral&&attacker==CombatFaction.Union)Provoke(actor.HomePlanet);
            bool shield=actor.Vitals.Shield>0;
            if(!actor.Vitals.Damage(amount,attacker,actor.Faction,actor==Player&&protection>0))return;
            if(world.Sector!=null&&actor.HomePlanet!=null)world.Sector.State.Attacked(actor.HomePlanet);
            if(actor.Faction==CombatFaction.Hostile)Engaged=true;
            effects.Impact(actor.transform.position,actor.Radius,shield, !actor.Vitals.Alive);
            if(actor==Player){world.Player.InhibitPulse(5);Message=shield?"SHIELD IMPACT / Evade incoming fire":"HULL DAMAGE / Break contact";}
            if(!actor.Vitals.Alive)
            {
                Message=actor.name+" destroyed";
                if(actor==Player){world.Surface?.Launch();respawn=settings.respawnSeconds;world.Player.FlightDisabled=true;world.Player.StopFlight();Message="WAYFARER LOST / Rescue craft returning you to Meridian";}
                else if(actor.Faction==CombatFaction.Union){actor.RepairRemaining=settings.satelliteRepairSeconds;satelliteRepairs[actor.Id]=actor.RepairRemaining;}
                if(Target==actor)Target=null;
                if(actor.Faction==CombatFaction.Hostile&&HostilesRemaining==0)Message="CORSAIR OUTPOST NEUTRALIZED / Solace patrol complete";
            }
        }
        public bool FirePlayer()
        {
            Vector3 direction=Player.transform.forward;
            if(Target!=null&&Target.Vitals.Alive)
            {
                var aim=Target.transform.position-Player.transform.position;
                if(aim.magnitude<=settings.weaponRange&&Vector3.Angle(direction,aim)<=settings.aimAssistDegrees)direction=aim.normalized;
            }
            return Fire(Player,direction);
        }
        void Update()
        {
            if(world==null||world.Player.Paused)return;float dt=Time.deltaTime;
            actors.RemoveAll(a=>a==null);protection=Mathf.Max(0,protection-dt);
            if(!Player.Vitals.Alive)
            {
                respawn-=dt;if(respawn<=0){Player.Vitals.Reset();world.Player.ReturnHome();world.Player.FlightDisabled=false;protection=settings.respawnProtection;Message="RESCUE COMPLETE / Empire progress preserved. Shields restored.";Target=null;}
            }
            if(Target!=null&&(!Target.Vitals.Alive||Vector3.Distance(Player.transform.position,Target.transform.position)>2000))Target=null;
            if(world.Surface?.Active!=true&&world.Player.CombatTargetPressed)SelectTarget();
            float outpostRange=Vector3.Distance(Player.transform.position,OutpostPosition);
            if(outpostRange<1500&&HostilesRemaining>0&&!Engaged)
            {
                warning+=dt;Message="CORSAIR WARNING / Restricted space. Withdraw or prepare to engage. R selects a target.";
                if(warning>=settings.warningSeconds&&outpostRange<settings.engagementRange){Engaged=true;Message="HOSTILE INTERCEPTORS / LMB fire · R target · keep moving";}
            }
            bool threat=false;
            foreach(var actor in actors)
            {
                actor.Cooldown=Mathf.Max(0,actor.Cooldown-dt);actor.Vitals.Tick(dt,settings.shieldDelay,settings.shieldRecharge);
                if(!actor.Vitals.Alive)
                {
                    if(actor.Faction==CombatFaction.Union&&actor!=Player&&world.Empire.State.Get(actor.HomePlanet)?.owned==true&&world.Sector.State.Get(actor.HomePlanet).recentAttack<=0){actor.RepairRemaining-=dt;satelliteRepairs[actor.Id]=actor.RepairRemaining;if(actor.RepairRemaining<=0)actor.Vitals.Reset();}
                    continue;
                }
                if(actor==Player)continue;
                if(actor.Traveling)
                {
                    float speed=world.Sector==null?600:world.sectorSettings.fleetSpeed;
                    actor.transform.position=Vector3.MoveTowards(actor.transform.position,actor.Destination,speed*dt);
                    foreach(var planet in world.Worlds)actor.transform.position=FlightRules.SafePosition(actor.transform.position,planet.transform.position,planet.Definition.radius+55,Vector3.up);
                    if((actor.Destination-actor.transform.position).sqrMagnitude<40000)actor.Traveling=false;
                    if(actor.Traveling)continue;
                }
                if(actor.Faction==CombatFaction.Neutral)continue;
                if(actor.Faction==CombatFaction.Hostile&&!Engaged&&actor.HomePlanet==null&&actor.FleetId==null)continue;
                actor.SimulationElapsed+=dt;
                if(Vector3.Distance(actor.transform.position,Player.transform.position)>5000&&actor.SimulationElapsed<.25f)continue;
                float simulationDt=actor.SimulationElapsed;actor.SimulationElapsed=0;
                CombatActor enemy=null;float distance=settings.engagementRange;
                foreach(var other in actors)
                {
                    if(other==null||!other.Vitals.Alive||other.Faction==actor.Faction||other.Faction==CombatFaction.Neutral||other==Player&&world.Surface?.Active==true)continue;
                    float candidate=Vector3.Distance(actor.transform.position,other.transform.position);if(candidate<distance){distance=candidate;enemy=other;}
                }
                if(enemy==null)continue;
                if(actor.Faction==CombatFaction.Hostile&&enemy==Player)threat=true;
                Vector3 delta=enemy.transform.position-actor.transform.position;
                if(actor.Role==CombatRole.Fighter)
                {
                    Vector3 strafe=Vector3.Cross(delta.normalized,Vector3.up).normalized;
                    Vector3 movement=(delta.normalized*(distance>240?1:distance<150?-1:0)+strafe*.6f).normalized;
                    Vector3 position=actor.transform.position+movement*settings.fighterSpeed*simulationDt;
                    foreach(var planet in world.Worlds)position=FlightRules.SafePosition(position,planet.transform.position,planet.Definition.radius+35,Vector3.up);
                    actor.transform.position=position;
                }
                if(distance<settings.hostileRange)
                {
                    var direction=(enemy.transform.position-actor.transform.position).normalized;
                    if(direction.sqrMagnitude>.01f)actor.transform.rotation=Quaternion.RotateTowards(actor.transform.rotation,Quaternion.LookRotation(direction),120*simulationDt);
                    if(actor.Faction==CombatFaction.Hostile){if(Vector3.Angle(actor.transform.forward,direction)>8)continue;direction=actor.transform.forward;}
                    if(Obstruction(actor.transform.position,direction,distance,actor)>=distance-enemy.Radius)Fire(actor,direction);
                }
            }
            ActiveThreat=threat&&Player.Vitals.Alive;
            if(ActiveThreat)world.Player.InhibitPulse(1);
            if(world.Surface?.Active!=true&&world.Player.FireHeld)FirePlayer();
            if(Engaged&&Target==null&&HostilesRemaining>0)SelectTarget();
        }
        void OnDestroy(){if(metal!=null)Destroy(metal);if(red!=null)Destroy(red);}
    }
}
