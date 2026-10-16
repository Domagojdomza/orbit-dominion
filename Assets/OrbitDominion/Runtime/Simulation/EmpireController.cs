using System;
using System.IO;
using UnityEngine;

namespace OrbitDominion
{
    [DefaultExecutionOrder(70)]
    public sealed class EmpireController : MonoBehaviour
    {
        public EmpireState State {get;private set;}
        public string Message {get;private set;}="COLONY CHARTER / Scan a viable world, approach orbit and press C.";
        public string ArchiveStatus {get;private set;}="EMPIRE LOG READY";
        public bool ManagementOpen {get;private set;}
        public string ManagedId {get;private set;}
        public EmpireSettings Settings=>world.empireSettings;
        PrototypeBootstrap world;
        EmpireArchive archive;
        float saveTimer;
        int revision,discoveryRevision;
        public void Initialize(PrototypeBootstrap world,string path)
        {
            this.world=world;var definitions=new WorldDefinition[world.Worlds.Length];
            for(int i=0;i<definitions.Length;i++)definitions[i]=world.Worlds[i].Definition;
            State=new EmpireState(definitions,Settings,world.settings.galaxySeed);archive=path==null?null:new EmpireArchive(path);
            if(archive!=null)try{ArchiveStatus=archive.Load(State,world.Discovery.Catalog);}catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){ArchiveStatus="EMPIRE ARCHIVE UNAVAILABLE";}
            revision=State.Revision;discoveryRevision=world.Discovery.Catalog.Revision;
        }
        public string OrbitalPlanet
        {
            get {foreach(var planet in world.Worlds)if(Vector3.Distance(world.Player.transform.position,planet.transform.position)-planet.Definition.radius<=Settings.interactionRange)return planet.Definition.id;return null;}
        }
        public string ColonyHint
        {
            get
            {
                string id=world.Discovery.Target?.Definition.id;var c=State.Get(id);
                if(c!=null&&c.deploying)return $"DEPLOYING {100*(1-c.deploymentRemaining/Settings.deploymentSeconds):0}% / "+(OrbitalPlanet==id&&!world.Player.Pulse?"Maintain orbit":"Return to orbit; disengage pulse");
                if(c!=null&&c.owned)return "B / MANAGE COLONY";
                return State.ColonyBlock(id,world.Discovery.Catalog,world.Discovery.TargetRange,world.Player.Pulse)??$"C / ESTABLISH COLONY · {Settings.colonyMinerals:0} M / {Settings.colonyEnergy:0} E";
            }
        }
        public bool Colonize()
        {
            var target=world.Discovery.Target;string id=target?.Definition.id;
            string reason=State.ColonyBlock(id,world.Discovery.Catalog,world.Discovery.TargetRange,world.Player.Pulse);
            if(reason!=null){Message=reason;return false;}
            bool result=State.StartColony(id,world.Discovery.Catalog,world.Discovery.TargetRange,world.Player.Pulse,world.Player.transform.position-target.transform.position);
            if(result){Message="COLONY CRAFT LAUNCHED / Maintain orbit until touchdown.";SaveNow();}return result;
        }
        public void ToggleManagement()
        {
            if(ManagementOpen){CloseManagement();return;}
            string id=world.Discovery.Target?.Definition.id;
            if(State.Get(id)?.owned!=true){Message="Select an owned colony in the exploration log.";return;}
            if(world.Discovery.JournalOpen)world.Discovery.ToggleJournal();
            ManagedId=id;ManagementOpen=true;world.Player.SetPause(true);
        }
        public void CloseManagement(){ManagementOpen=false;world.Player.SetPause(false);}
        public bool Build(BuildingKind kind)
        {
            string reason=State.BuildBlock(ManagedId,kind);if(reason!=null){Message=reason;return false;}
            bool result=State.StartBuilding(ManagedId,kind);
            if(result){Message=EmpireState.BuildingName(kind)+" commissioned. Resume flight to advance construction.";SaveNow();}return result;
        }
        public void SaveNow()
        {
            if(State==null)return;saveTimer=0;revision=State.Revision;discoveryRevision=world.Discovery.Catalog.Revision;
            if(archive==null){world.Session?.SaveNow();return;}
            try{archive.Save(State,world.Discovery.Catalog);ArchiveStatus="EMPIRE SAVED";}
            catch(Exception e)when(e is IOException||e is InvalidDataException||e is UnauthorizedAccessException||e is ArgumentException){ArchiveStatus="SAVE BLOCKED / ORIGINAL EMPIRE ARCHIVE PRESERVED";}
        }
        void OnApplicationQuit(){SaveNow();}
        void Update()
        {
            if(State==null)return;
            if(ManagementOpen&&(!world.Player.Paused||world.Discovery.JournalOpen))ManagementOpen=false;
            if(world.Surface?.Active!=true&&world.Player.ManagePressed)ToggleManagement();
            if(world.Player.SavePressed)SaveNow();
            if(world.Player.Paused)return;
            if(world.Surface?.Active!=true&&world.Player.ColonizePressed)Colonize();
            int before=State.Revision;State.Tick(Time.deltaTime,OrbitalPlanet,world.Player.Pulse);
            if(before!=State.Revision)Message="COLONY NETWORK / Infrastructure completed. B opens colony management.";
            saveTimer+=Time.deltaTime;
            if(saveTimer>=10||revision!=State.Revision||discoveryRevision!=world.Discovery.Catalog.Revision)SaveNow();
        }
    }
}
