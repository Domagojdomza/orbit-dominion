using System;
using System.Collections.Generic;
using UnityEngine;
namespace OrbitDominion
{
    [Serializable] public sealed class PlanetPolicy
    {
        public string id;public float protection,cooldown,recentAttack,capture,enemyCapture;public bool provoked;
    }
    [Serializable] public sealed class FleetOrder
    {
        public string id,source,target;public List<string> ships=new();public bool arrived,finished;public float capture;
    }
    [Serializable] public sealed class SectorSave
    {
        public List<PlanetPolicy> planets=new();public List<FleetOrder> fleets=new();public List<string> anomalies=new();
        public float enemyMinerals=400,enemyEnergy=300,decision=60,victory;
        public int nextFleet,tutorial;public bool won,firstRaid,raidDefeated,contactSeen,protectionReviewed,frontierVisited,hostileVisited,hadPulse,hadFlight,firstCaptured,offensiveReady;
        public string waypoint;public int waypointSystem=-1;public string investigating;public float investigation;
    }
    public sealed class SectorState
    {
        public SectorSave Data {get;private set;}
        readonly SectorSettings settings;
        public SectorState(IEnumerable<string> ids,SectorSettings settings){this.settings=settings;Data=new SectorSave();foreach(string id in ids)Data.planets.Add(new PlanetPolicy{id=id});}
        public PlanetPolicy Get(string id)=>Data.planets.Find(p=>p.id==id);
        public string ProtectionBlock(string id,EmpireState empire)
        {
            var p=Get(id);if(p==null||empire.Get(id)?.owned!=true)return "Requires an owned colony";
            if(p.protection>0)return "Protection already active";if(p.cooldown>0)return $"Protection cooldown {p.cooldown:0}s";
            if(p.recentAttack>0)return "Recently attacked / cannot activate protection";
            if(empire.Energy<settings.shieldCost)return "Insufficient energy";return null;
        }
        public bool Protect(string id,EmpireState empire)
        {
            if(ProtectionBlock(id,empire)!=null||!empire.Spend(0,settings.shieldCost))return false;
            Get(id).protection=settings.shieldDuration;return true;
        }
        public void Tick(float dt,int owned)
        {
            if(!Finite(dt)||dt<=0)return;
            foreach(var p in Data.planets)
            {
                p.recentAttack=Mathf.Max(0,p.recentAttack-dt);
                if(p.protection>0){float previous=p.protection;p.protection=Mathf.Max(0,previous-dt);if(p.protection==0)p.cooldown=Mathf.Max(0,settings.shieldCooldown-(dt-previous));}
                else p.cooldown=Mathf.Max(0,p.cooldown-dt);
            }
            if(Data.won)return;
            Data.victory=owned>=settings.victoryPlanets?Mathf.Min(settings.victorySeconds,Data.victory+dt):0;
            if(Data.victory>=settings.victorySeconds)Data.won=true;
        }
        public bool Capture(string id,bool held,bool scanned,bool inRange,bool defenders,bool pulse,float dt)
        {
            var p=Get(id);if(p==null)return false;
            if(!held||!scanned||!inRange||defenders||pulse||p.protection>0){p.capture=0;return false;}
            p.capture+=Mathf.Max(0,dt);if(p.capture<settings.captureSeconds)return false;p.capture=0;Data.firstCaptured=true;return true;
        }
        public void Attacked(string id){var p=Get(id);if(p!=null)p.recentAttack=settings.recentAttackLock;}
        public static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        public bool IsValid(SectorSave data)
        {
            if(data==null||data.planets==null||data.planets.Count!=Data.planets.Count||data.fleets==null||data.fleets.Count>128||data.anomalies==null||data.anomalies.Count>3||data.tutorial<0||data.tutorial>27||data.waypointSystem< -1||data.waypointSystem>2||data.nextFleet<0)return false;
            foreach(float v in new[]{data.enemyMinerals,data.enemyEnergy,data.decision,data.victory,data.investigation})if(!Finite(v)||v<0||v>10000000)return false;
            if(data.victory>settings.victorySeconds||data.investigation>settings.anomalySeconds+.1f)return false;
            var ids=new HashSet<string>();foreach(var p in data.planets)
            {
                if(p==null||p.id==null||Get(p.id)==null||!ids.Add(p.id))return false;
                foreach(float v in new[]{p.protection,p.cooldown,p.recentAttack,p.capture,p.enemyCapture})if(!Finite(v)||v<0||v>100000)return false;
                if(p.protection>settings.shieldDuration||p.cooldown>settings.shieldCooldown||p.capture>settings.captureSeconds)return false;
            }
            ids.Clear();foreach(var f in data.fleets)if(f==null||string.IsNullOrEmpty(f.id)||!ids.Add(f.id)||Get(f.source)==null||Get(f.target)==null||f.ships==null||f.ships.Count!=2||f.ships[0]==f.ships[1]||!Finite(f.capture)||f.capture<0)return false;
            ids.Clear();foreach(string a in data.anomalies)if(a!="ruins"&&a!="wreck"&&a!="rift"||!ids.Add(a))return false;
            return string.IsNullOrEmpty(data.waypoint)||Get(data.waypoint)!=null||data.waypoint=="ruins"||data.waypoint=="wreck"||data.waypoint=="rift";
        }
        public void Restore(SectorSave data){if(!IsValid(data))throw new ArgumentException("Invalid sector state");Data=JsonUtility.FromJson<SectorSave>(JsonUtility.ToJson(data));}
    }
}
