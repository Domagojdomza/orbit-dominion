using System;
using System.IO;
using UnityEngine;

namespace OrbitDominion
{
    [DefaultExecutionOrder(50)]
    public sealed class DiscoveryController : MonoBehaviour
    {
        public DiscoveryCatalog Catalog {get;private set;}
        public ScanSession Scan {get;}=new();
        public PlanetView Target {get;private set;}
        public DiscoverySettings Settings {get;private set;}
        public string Transmission {get;private set;}="MERIDIAN CONTROL / Follow the navigation beacon.";
        public string ArchiveStatus {get;private set;}="SURVEY LOG READY";
        public bool JournalOpen {get;private set;}
        public string LastDiscoveryId {get;private set;}
        public SurfaceExpedition Surface {get;set;}
        public float TargetRange=>Target==null?float.MaxValue:Vector3.Distance(player.transform.position,Target.transform.position)-Target.Definition.radius;
        public string TargetLabel=>Target==null?"NO CONTACT":Catalog.HasDetails(Target.Definition.id)?Target.Definition.title:"UNKNOWN PLANET";
        PlayerFlight player;
        PlanetView[] worlds;
        Vector3 beacon;
        DiscoveryArchive archive;
        int savedRevision;
        public void Initialize(PlayerFlight player,PlanetView[] worlds,DiscoverySettings settings,int seed,Vector3 beacon,string archivePath)
        {
            Scan.Reset();Target=null;LastDiscoveryId=null;JournalOpen=false;archive=null;
            this.player=player;this.worlds=worlds;Settings=settings;this.beacon=beacon;
            var ids=new string[worlds.Length];for(int i=0;i<worlds.Length;i++)ids[i]=worlds[i].Definition.id;
            Catalog=new DiscoveryCatalog(seed,ids,ids[0]);
            if(archivePath!=null)
            {
                archive=new DiscoveryArchive(archivePath);
                try{ArchiveStatus=archive.Load(Catalog);}catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){ArchiveStatus="SURVEY LOG UNAVAILABLE";}
            }
            savedRevision=Catalog.Revision;
            for(int i=1;i<worlds.Length;i++)if(Catalog.State(ids[i])!=DiscoveryState.Unknown){Target=worlds[i];break;}
        }
        public void SelectTarget(PlanetView planet)
        {
            if(planet!=null&&Catalog.State(planet.Definition.id)!=DiscoveryState.Unknown&&Target!=planet){Scan.Reset();Target=planet;}
        }
        public void CycleTarget()
        {
            int current=Array.IndexOf(worlds,Target);
            for(int step=1;step<=worlds.Length;step++)
            {
                var next=worlds[(current+step)%worlds.Length];
                if(Catalog.State(next.Definition.id)!=DiscoveryState.Unknown){SelectTarget(next);return;}
            }
        }
        public void ToggleJournal(){JournalOpen=!JournalOpen;player.SetPause(JournalOpen);}
        public void SaveNow()
        {
            savedRevision=Catalog.Revision;
            if(archive==null)return;
            try{archive.Save(Catalog);ArchiveStatus="SURVEY LOG SAVED";}
            catch(Exception e) when(e is IOException||e is InvalidDataException||e is UnauthorizedAccessException||e is ArgumentException){ArchiveStatus="SAVE BLOCKED / EXISTING ARCHIVE PRESERVED";}
            savedRevision=Catalog.Revision;
        }
        void Update()
        {
            if(Catalog==null)return;
            if(Surface?.Active==true)return;
            if(player.JournalPressed)ToggleJournal();
            if(JournalOpen&&!player.Paused)JournalOpen=false;
            if(player.SavePressed)SaveNow();
            if(player.Paused)return;
            if(player.TargetPressed)CycleTarget();
            Tick(Time.deltaTime,player.ScanHeld);
        }
        public void Tick(float deltaTime,bool scanHeld)
        {
            if(player.Paused)return;
            if(Vector3.Distance(player.transform.position,beacon)<65&&!Catalog.BeaconReached)
            {
                Catalog.ReachBeacon();Catalog.Detect(worlds[1].Definition.id);SelectTarget(worlds[1]);
                Transmission="MERIDIAN CONTROL / Unidentified planetary signal. Engage pulse and investigate.";
            }
            foreach(var world in worlds)
            {
                float range=Vector3.Distance(player.transform.position,world.transform.position)-world.Definition.radius;
                if(range<=Settings.detectionRange&&Catalog.Detect(world.Definition.id))
                {
                    Transmission="SENSOR CONTACT / Unknown planetary body detected.";
                    if(Target==null||Catalog.HasDetails(Target.Definition.id))SelectTarget(world);
                }
                if(range<=Settings.visitRange)Catalog.Visit(world.Definition.id);
            }
            if(Target!=null&&Scan.Tick(Catalog,Target.Definition.id,scanHeld,TargetRange,player.Pulse,Settings.scanRange,Settings.scanSeconds,deltaTime))
            {
                LastDiscoveryId=Target.Definition.id;Catalog.ReachBeacon();
                Transmission="SURVEY COMPLETE / "+Target.Definition.title+" added to the exploration log.";
                if(TargetRange<=Settings.visitRange)Catalog.Visit(Target.Definition.id);
            }
            if(savedRevision!=Catalog.Revision)SaveNow();
        }
        public string ScanHint
        {
            get
            {
                if(Target==null)return "FOLLOW THE BEACON TO ACQUIRE A SIGNAL";
                if(Catalog.HasDetails(Target.Definition.id))return Catalog.State(Target.Definition.id)==DiscoveryState.Visited?"ORBITAL SURVEY COMPLETE":"ENTER ORBIT TO VERIFY STRATEGIC DATA";
                if(player.Pulse)return "DISENGAGE PULSE BEFORE SCANNING";
                if(TargetRange>Settings.scanRange)return "APPROACH WITHIN "+Settings.scanRange.ToString("0")+" m OF SURFACE";
                return Scan.Progress>0?(Scan.Progress<.34f?"ANALYSING ATMOSPHERE":Scan.Progress<.7f?"MAPPING RESOURCES":"SEARCHING FOR LIFE")+" / KEEP HOLDING F":"HOLD F / SCAN PLANET";
            }
        }
    }
}
