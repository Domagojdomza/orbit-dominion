using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace OrbitDominion
{
    [DefaultExecutionOrder(200)]
    public sealed class GameSession : MonoBehaviour
    {
        PrototypeBootstrap world;GameArchive archive;float timer;bool ready,saveBlocked;
        public string Status {get;private set;}="SAVE READY";
        public void Initialize(PrototypeBootstrap world,string path){this.world=world;if(path!=null)archive=new GameArchive(path,Validate);StartCoroutine(Load());}
        IEnumerator Load()
        {
            yield return null;
            if(archive!=null)
            {
                try
                {
                    var save=archive.Load(out string status);Status=status;
                    if(save!=null)Restore(save);
                    else if(!File.Exists(archive.Path))ImportLegacy(System.IO.Path.GetDirectoryName(archive.Path));
                }
                catch(Exception e)when(e is IOException||e is UnauthorizedAccessException||e is InvalidDataException||e is ArgumentException){saveBlocked=true;Status="SAVE UNAVAILABLE / ORIGINAL FILES PRESERVED";}
            }
            ready=true;
        }
        public GameSave Snapshot()=>new(){seed=world.settings.galaxySeed,empire=world.Empire.State.Snapshot(world.Discovery.Catalog),sector=JsonUtility.FromJson<SectorSave>(JsonUtility.ToJson(world.Sector.State.Data)),combat=world.Combat.Snapshot(),playerPosition=world.Player.transform.position,playerRotation=world.Player.transform.rotation,velocity=world.Player.Velocity,pulse=world.Player.Pulse,combatLock=world.Player.CombatLock,orbitalSeconds=world.Orbits.Elapsed,planetTarget=world.Discovery.Target?.Definition.id,surface=world.Surface.Snapshot()};
        public bool Validate(GameSave s)
        {
            if(s==null||s.version!=2||s.seed!=world.settings.galaxySeed||!world.Empire.State.IsValid(s.empire,world.Discovery.Catalog)||!world.Sector.State.IsValid(s.sector)||s.combat==null||s.combat.actors==null||s.combat.actors.Count>256||!GameArchive.ValidVector(s.playerPosition)||!GameArchive.ValidVector(s.velocity)||s.velocity.magnitude>world.settings.pulseSpeed+1||!GameArchive.ValidRotation(s.playerRotation)||!SectorState.Finite(s.combatLock)||s.combatLock<0||!SectorState.Finite(s.orbitalSeconds)||s.orbitalSeconds<0||s.orbitalSeconds>10000000)return false;
            var definitions=new WorldDefinition[world.Worlds.Length];for(int i=0;i<definitions.Length;i++)definitions[i]=world.Worlds[i].Definition;
            if(!SurfaceRules.ValidSave(s.surface,definitions))return false;
            var ids=new HashSet<string>();int players=0;
            foreach(var a in s.combat.actors)
            {
                if(!GameArchive.ValidActor(a)||!ids.Add(a.id)||!string.IsNullOrEmpty(a.homePlanet)&&world.Sector.Planet(a.homePlanet)==null)return false;
                if(a.role==CombatRole.Player){players++;if(a.id!="wayfarer"||a.faction!=CombatFaction.Union)return false;}
            }
            foreach(float v in new[]{s.combat.respawn,s.combat.protection,s.combat.warning})if(!SectorState.Finite(v)||v<0||v>10000000)return false;
            foreach(var f in s.sector.fleets)foreach(string id in f.ships)if(!ids.Contains(id))return false;
            if(!string.IsNullOrEmpty(s.combat.target)&&!ids.Contains(s.combat.target))return false;
            return players==1&&(string.IsNullOrEmpty(s.planetTarget)||world.Sector.Planet(s.planetTarget)!=null);
        }
        public bool Restore(GameSave save)
        {
            if(!Validate(save))return false;
            world.Empire.State.Restore(save.empire,world.Discovery.Catalog);world.Discovery.Catalog.Restore(save.empire.discoveries);
            world.Sector.State.Restore(save.sector);world.Orbits.SetTime(save.orbitalSeconds);world.Combat.Restore(save.combat);world.Player.RestoreFlight(save);
            if(!string.IsNullOrEmpty(save.planetTarget))world.Discovery.SelectTarget(world.Sector.Planet(save.planetTarget));world.Surface.Restore(save.surface);world.Sector.TickTutorial();return true;
        }
        public void ImportLegacy(string folder)
        {
            var definitions=new WorldDefinition[3];for(int i=0;i<3;i++)definitions[i]=world.Worlds[i].Definition;
            var discovery=new DiscoveryCatalog(world.settings.galaxySeed,new[]{"solace-01","solace-02","solace-03"},"solace-01");
            string surveyPath=System.IO.Path.Combine(folder,"discovery-v1.json"),empirePath=System.IO.Path.Combine(folder,"empire-v1.json");
            if(File.Exists(surveyPath)&&new DiscoveryArchive(surveyPath).Load(discovery).Contains("INVALID"))throw new InvalidDataException("Legacy survey is invalid");
            var legacy=new EmpireState(definitions,world.empireSettings,world.settings.galaxySeed);
            if(File.Exists(empirePath)&&new EmpireArchive(empirePath).Load(legacy,discovery).Contains("INVALID"))throw new InvalidDataException("Legacy empire is invalid");
            if(!File.Exists(surveyPath)&&!File.Exists(empirePath))return;
            var old=legacy.Snapshot(discovery);var expanded=world.Empire.State.Snapshot(world.Discovery.Catalog);
            foreach(var c in old.colonies){int i=expanded.colonies.FindIndex(p=>p.id==c.id);expanded.colonies[i]=c;}
            foreach(var e in old.discoveries.planets){var target=expanded.discoveries.planets.Find(p=>p.id==e.id);target.state=e.state;}
            expanded.discoveries.beaconReached=old.discoveries.beaconReached;expanded.minerals=old.minerals;expanded.energy=old.energy;
            if(!world.Empire.State.Restore(expanded,world.Discovery.Catalog))throw new InvalidDataException("Legacy migration rejected");
            Status="LEGACY SURVEY & COLONIES IMPORTED / ORIGINALS RETAINED";
        }
        public void SaveNow()
        {
            if(!ready||archive==null||saveBlocked)return;
            try{archive.Save(Snapshot());Status="FULL FLIGHT SAVED";timer=0;}
            catch(Exception e)when(e is IOException||e is InvalidDataException||e is UnauthorizedAccessException||e is ArgumentException){Status="SAVE BLOCKED / ORIGINAL FILE PRESERVED";}
        }
        void Update(){if(!ready)return;if(world.Player.SavePressed)SaveNow();if(!world.Player.Paused){timer+=Time.deltaTime;if(timer>=10){timer=0;SaveNow();}}}
        void OnApplicationQuit()=>SaveNow();
    }
}
