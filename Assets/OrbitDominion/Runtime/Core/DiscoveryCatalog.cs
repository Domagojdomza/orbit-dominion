using System;
using System.Collections.Generic;

namespace OrbitDominion
{
    [Serializable] public sealed class DiscoveryEntry { public string id; public DiscoveryState state; }
    [Serializable] public sealed class DiscoverySave
    {
        public int version=1, seed;
        public bool beaconReached;
        public List<DiscoveryEntry> planets=new();
    }
    // Authoritative, presentation-independent discovery state. No GameObjects or input devices.
    public sealed class DiscoveryCatalog
    {
        readonly Dictionary<string,DiscoveryState> states=new();
        readonly string homeId;
        public int Seed {get;}
        public bool BeaconReached {get;private set;}
        public int Revision {get;private set;}
        public DiscoveryCatalog(int seed, IEnumerable<string> ids, string homeId)
        {
            Seed=seed;this.homeId=homeId;
            foreach(string id in ids)states.Add(id,DiscoveryState.Unknown);
            states[homeId]=DiscoveryState.Visited;
        }
        public DiscoveryState State(string id)=>states.TryGetValue(id,out var value)?value:DiscoveryState.Unknown;
        public bool HasDetails(string id)=>State(id)>=DiscoveryState.Scanned;
        bool Advance(string id,DiscoveryState state)
        {
            if(!states.ContainsKey(id)||states[id]>=state)return false;
            states[id]=state;Revision++;return true;
        }
        public bool Detect(string id)=>Advance(id,DiscoveryState.Detected);
        public bool CompleteScan(string id)=>State(id)==DiscoveryState.Detected&&Advance(id,DiscoveryState.Scanned);
        public bool Visit(string id)=>State(id)==DiscoveryState.Scanned&&Advance(id,DiscoveryState.Visited);
        public void ReachBeacon(){if(!BeaconReached){BeaconReached=true;Revision++;}}
        public DiscoverySave Snapshot()
        {
            var save=new DiscoverySave{seed=Seed,beaconReached=BeaconReached};
            foreach(var pair in states)save.planets.Add(new DiscoveryEntry{id=pair.Key,state=pair.Value});
            return save;
        }
        public bool IsValid(DiscoverySave save)
        {
            if(save==null||save.version!=1||save.seed!=Seed||save.planets==null||save.planets.Count!=states.Count)return false;
            var ids=new HashSet<string>();
            foreach(var entry in save.planets)
                if(entry==null||entry.id==null||!states.ContainsKey(entry.id)||!ids.Add(entry.id)||!Enum.IsDefined(typeof(DiscoveryState),entry.state)||
                    (entry.id==homeId&&entry.state!=DiscoveryState.Visited))return false;
            return true;
        }
        public void Restore(DiscoverySave save)
        {
            if(!IsValid(save))throw new ArgumentException("Invalid or incompatible discovery archive.");
            foreach(var entry in save.planets)states[entry.id]=entry.state;
            BeaconReached=save.beaconReached;Revision++;
        }
    }
    public sealed class ScanSession
    {
        public string TargetId {get;private set;}
        public float Progress {get;private set;}
        public void Reset(){TargetId=null;Progress=0;}
        public bool Tick(DiscoveryCatalog catalog,string id,bool held,float clearance,bool cruising,float range,float duration,float deltaTime)
        {
            if(!held||cruising||clearance>range||id==null||catalog.State(id)!=DiscoveryState.Detected){Reset();return false;}
            if(TargetId!=id){Reset();TargetId=id;}
            Progress=Math.Min(1,Progress+Math.Max(0,deltaTime)/Math.Max(.1f,duration));
            if(Progress<1)return false;
            bool completed=catalog.CompleteScan(id);Reset();return completed;
        }
    }
}
