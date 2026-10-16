using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitDominion
{
    public enum BuildingKind { Mining, Energy, Settlement, Defense, Station }
    [Serializable] public sealed class ColonyState
    {
        public string id;
        public bool owned, deploying;
        public int faction; // 0 unclaimed, 1 Union, 2 neutral, 3 Dominion
        public float deploymentRemaining;
        public Vector3 landingDirection=Vector3.back;
        public int[] levels=new int[5];
        public int construction=-1;
        public float constructionRemaining;
        public int Development=>levels[(int)BuildingKind.Settlement];
        public int Defense=>levels[(int)BuildingKind.Defense]*30;
        public int Slots=>Math.Min(5,Development+2);
        public int UsedSlots { get { int count=0;foreach(int level in levels)if(level>0)count++;return count; } }
    }
    [Serializable] public sealed class EmpireSave
    {
        public int version=1, seed;
        public float minerals, energy;
        public List<ColonyState> colonies=new();
        public DiscoverySave discoveries;
    }

    // Authoritative economy. No scene objects, input, wall clock or disk operations.
    public sealed class EmpireState
    {
        readonly WorldDefinition[] worlds;
        readonly EmpireSettings settings;
        readonly Dictionary<string,ColonyState> colonies=new();
        public int Seed {get;}
        public int Revision {get;private set;}
        public float Minerals {get;private set;}
        public float Energy {get;private set;}
        public IEnumerable<ColonyState> Colonies=>colonies.Values;
        public ColonyState Get(string id)=>id!=null&&colonies.TryGetValue(id,out var colony)?colony:null;
        public EmpireState(WorldDefinition[] worlds,EmpireSettings settings,int seed)
        {
            this.worlds=worlds;this.settings=settings;Seed=seed;
            Minerals=settings.startingMinerals;Energy=settings.startingEnergy;
            for(int i=0;i<worlds.Length;i++)
            {
                int faction=i==0?1:worlds[i].survey.owner=="Ithacan Assembly"?2:worlds[i].survey.owner=="Cinder Dominion"?3:0;
                colonies.Add(worlds[i].id,new ColonyState{id=worlds[i].id,owned=i==0,faction=faction,levels=i==0?new[]{2,2,3,2,1}:faction>1?new[]{1,1,3,1,1}:new int[5]});
            }
        }
        public static string BuildingName(BuildingKind kind)=>kind switch {
            BuildingKind.Mining=>"Mining complex",BuildingKind.Energy=>"Energy complex",BuildingKind.Settlement=>"Settlement",
            BuildingKind.Defense=>"Defense satellite",_=>"Orbital station"};
        public static Vector2 Cost(BuildingKind kind,int nextLevel)
        {
            Vector2 cost=kind switch { BuildingKind.Mining=>new(90,45),BuildingKind.Energy=>new(70,60),BuildingKind.Settlement=>new(100,80),BuildingKind.Defense=>new(130,100),_=>new(180,140) };
            return cost*nextLevel;
        }
        public Vector2 Production(string id)
        {
            var c=Get(id);if(c==null||!c.owned)return Vector2.zero;
            return WorldProduction(id);
        }
        public Vector2 WorldProduction(string id)
        {
            var c=Get(id);if(c==null||c.Development==0)return Vector2.zero;
            var d=Array.Find(worlds,w=>w.id==id);
            float industry=1+c.levels[(int)BuildingKind.Station]*.1f;
            return new Vector2((settings.baseMineralsPerSecond+c.levels[0]*1.4f)*(.5f+d.survey.minerals/100f),
                (settings.baseEnergyPerSecond+c.levels[1]*1.6f)*(.5f+d.survey.energy/100f))*industry;
        }
        public Vector2 TotalProduction {get {Vector2 total=Vector2.zero;foreach(var c in colonies.Values)total+=Production(c.id);return total;}}
        public string ColonyBlock(string id,DiscoveryCatalog discovery,float range,bool pulse)
        {
            var c=Get(id);if(c==null)return "Select a planetary contact";
            if(!discovery.HasDetails(id))return "Complete the planetary scan";
            if(c.owned)return "Already part of the Wayfarer Union";
            if(c.deploying)return "Colony deployment in progress";
            if(c.faction>1)return "Occupied world / clear defenses, then hold G to capture";
            var d=Array.Find(worlds,w=>w.id==id);
            if(d.survey.owner!="Unclaimed"||d.survey.defense>0)return "World is occupied or defended";
            if(d.survey.habitability<settings.minimumHabitability)return "Environment cannot support a colony";
            if(pulse)return "Disengage pulse drive";
            if(!Finite(range)||range>settings.interactionRange)return $"Approach within {settings.interactionRange:0} m of the surface";
            if(Minerals<settings.colonyMinerals||Energy<settings.colonyEnergy)return "Insufficient minerals or energy";
            return null;
        }
        public bool StartColony(string id,DiscoveryCatalog discovery,float range,bool pulse,Vector3 direction)
        {
            if(ColonyBlock(id,discovery,range,pulse)!=null||!Finite(direction.x)||!Finite(direction.y)||!Finite(direction.z))return false;
            var c=Get(id);Minerals-=settings.colonyMinerals;Energy-=settings.colonyEnergy;
            c.deploying=true;c.deploymentRemaining=settings.deploymentSeconds;c.landingDirection=direction.sqrMagnitude>.01f?direction.normalized:Vector3.back;Revision++;return true;
        }
        public string BuildBlock(string id,BuildingKind kind)
        {
            var c=Get(id);if(c==null||!c.owned)return "Requires a Wayfarer colony";
            if(!Enum.IsDefined(typeof(BuildingKind),kind))return "Unknown building";
            if(c.construction>=0)return "Construction already in progress";
            int level=c.levels[(int)kind];if(level>=3)return "Maximum level reached";
            if(level==0&&c.UsedSlots>=c.Slots)return "Upgrade settlement to unlock another slot";
            var cost=Cost(kind,level+1);if(Minerals<cost.x||Energy<cost.y)return "Insufficient minerals or energy";
            return null;
        }
        public bool StartBuilding(string id,BuildingKind kind)
        {
            if(BuildBlock(id,kind)!=null)return false;
            var c=Get(id);var cost=Cost(kind,c.levels[(int)kind]+1);Minerals-=cost.x;Energy-=cost.y;
            c.construction=(int)kind;c.constructionRemaining=settings.constructionSeconds*(c.levels[(int)kind]+1);Revision++;return true;
        }
        // Deployment pauses outside orbit; paid work is retained. Construction runs remotely.
        // Split at completion boundaries so large and small simulation steps produce the same income.
        public void Tick(float seconds,string orbitalPlanet,bool pulse)
        {
            if(!Finite(seconds)||seconds<=0)return;
            while(seconds>0)
            {
                float step=seconds;
                foreach(var c in colonies.Values)
                {
                    if(c.deploying&&c.id==orbitalPlanet&&!pulse)step=Math.Min(step,c.deploymentRemaining);
                    if(c.construction>=0)step=Math.Min(step,c.constructionRemaining);
                }
                var production=TotalProduction;Minerals=Math.Min(10000000,Minerals+production.x*step);Energy=Math.Min(10000000,Energy+production.y*step);
                foreach(var c in colonies.Values)
                {
                    if(c.deploying&&c.id==orbitalPlanet&&!pulse)
                    {
                        c.deploymentRemaining=Math.Max(0,c.deploymentRemaining-step);
                        if(c.deploymentRemaining<=.0001f){c.deploying=false;c.owned=true;c.faction=1;c.levels[2]=1;Revision++;}
                    }
                    if(c.construction>=0)
                    {
                        c.constructionRemaining=Math.Max(0,c.constructionRemaining-step);
                        if(c.constructionRemaining<=.0001f){c.levels[c.construction]++;c.construction=-1;Revision++;}
                    }
                }
                seconds-=step;
            }
        }
        public EmpireSave Snapshot(DiscoveryCatalog discovery)
        {
            var save=new EmpireSave{seed=Seed,minerals=Minerals,energy=Energy,discoveries=discovery.Snapshot()};
            foreach(var c in colonies.Values)save.colonies.Add(Clone(c));return save;
        }
        static ColonyState Clone(ColonyState c)=>new(){id=c.id,owned=c.owned,faction=c.owned?1:c.faction,deploying=c.deploying,deploymentRemaining=c.deploymentRemaining,landingDirection=c.landingDirection,levels=(int[])c.levels.Clone(),construction=c.construction,constructionRemaining=c.constructionRemaining};
        public bool Spend(float minerals,float energy)
        {
            if(!Finite(minerals)||!Finite(energy)||minerals<0||energy<0||Minerals<minerals||Energy<energy)return false;
            Minerals-=minerals;Energy-=energy;Revision++;return true;
        }
        public void Reward(float minerals,float energy){if(minerals<0||energy<0||!Finite(minerals)||!Finite(energy))return;Minerals=Math.Min(10000000,Minerals+minerals);Energy=Math.Min(10000000,Energy+energy);Revision++;}
        public bool ChangeOwner(string id,int faction)
        {
            var c=Get(id);if(c==null||faction<0||faction>3||id==worlds[0].id&&faction!=1)return false;
            c.faction=faction;c.owned=faction==1;c.deploying=false;c.deploymentRemaining=0;c.construction=-1;c.constructionRemaining=0;c.levels[3]=0;c.levels[2]=Math.Max(1,c.levels[2]);Revision++;return true;
        }
        public bool QueueForeignBuilding(string id,BuildingKind kind)
        {
            var c=Get(id);int k=(int)kind;if(c==null||c.faction!=3||k<0||k>4||c.construction>=0||c.levels[k]>=3||c.levels[k]==0&&c.UsedSlots>=c.Slots)return false;
            c.construction=k;c.constructionRemaining=settings.constructionSeconds*(c.levels[k]+1);Revision++;return true;
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        public bool IsValid(EmpireSave save,DiscoveryCatalog discovery)
        {
            if(save==null||save.version!=1||save.seed!=Seed||!Finite(save.minerals)||!Finite(save.energy)||save.minerals<0||save.energy<0||save.minerals>10000000||save.energy>10000000||save.colonies==null||save.colonies.Count!=worlds.Length||!discovery.IsValid(save.discoveries))return false;
            var ids=new HashSet<string>();
            foreach(var c in save.colonies)
            {
                if(c==null||c.id==null||!colonies.ContainsKey(c.id)||!ids.Add(c.id)||c.levels==null||c.levels.Length!=5)return false;
                foreach(int level in c.levels)if(level<0||level>3)return false;
                if(!Finite(c.deploymentRemaining)||c.deploymentRemaining<0||c.deploymentRemaining>settings.deploymentSeconds||!Finite(c.constructionRemaining)||c.constructionRemaining<0||c.constructionRemaining>settings.constructionSeconds*3||c.construction< -1||c.construction>4)return false;
                if(!Finite(c.landingDirection.sqrMagnitude)||c.landingDirection.sqrMagnitude<.9f||c.landingDirection.sqrMagnitude>1.1f)return false;
                if(c.faction<0||c.faction>3||c.owned&&(c.deploying||c.Development<1)||!c.owned&&c.faction==0&&(c.UsedSlots!=0||c.construction!=-1)||!c.owned&&c.faction==1)return false;
                if(c.deploying&&c.deploymentRemaining<=0||!c.deploying&&c.deploymentRemaining>.0001f)return false;
                if(c.construction>=0&&(c.constructionRemaining<=0||c.levels[c.construction]>=3||c.levels[c.construction]==0&&c.UsedSlots>=c.Slots)||c.construction<0&&c.constructionRemaining>.0001f)return false;
                if(c.UsedSlots>c.Slots||c.id==worlds[0].id&&!c.owned)return false;
                var entry=save.discoveries.planets.Find(p=>p.id==c.id);
                if((c.owned||c.deploying)&&entry.state<DiscoveryState.Scanned)return false;
            }
            return true;
        }
        public bool Restore(EmpireSave save,DiscoveryCatalog discovery)
        {
            if(!IsValid(save,discovery))return false;
            Minerals=save.minerals;Energy=save.energy;foreach(var c in save.colonies)colonies[c.id]=Clone(c);Revision++;
            // A newer legacy survey may contain discoveries made since the last empire autosave.
            foreach(var entry in save.discoveries.planets)
            {
                if(entry.state>=DiscoveryState.Detected)discovery.Detect(entry.id);
                if(entry.state>=DiscoveryState.Scanned)discovery.CompleteScan(entry.id);
                if(entry.state>=DiscoveryState.Visited)discovery.Visit(entry.id);
            }
            if(save.discoveries.beaconReached)discovery.ReachBeacon();return true;
        }
    }
}
