using UnityEngine;
using UnityEngine.UIElements;
namespace OrbitDominion
{
    [DefaultExecutionOrder(100)]
    public sealed class SurveyPanel : MonoBehaviour
    {
        PrototypeBootstrap world;
        Label title,state,details,scanHint,transmission,archive;
        VisualElement card,scanTrack,scanFill,journal,list;
        int revision=-1;
        string selected;
        string detailsId;DiscoveryState detailsState;int detailsRevision=-1;
        public void Initialize(PrototypeBootstrap world,VisualElement root)
        {
            this.world=world;
            card=Box(root,"survey-card");Text(card,"PLANETARY SURVEY","eyebrow");
            title=Text(card,"UNKNOWN PLANET","survey-title");state=Text(card,"DETECTED","survey-state");
            details=Text(card,"No analysis available.","survey-details");
            scanHint=Text(card,"HOLD F / SCAN","scan-hint");
            scanTrack=Box(card,"scan-track");scanFill=Box(scanTrack,"scan-fill");
            Text(card,"TAB  Next contact     J  Exploration log","survey-help");
            transmission=Text(root,"","transmission");
            journal=Box(root,"journal");Text(journal,"EXPLORATION LOG","objective");
            Text(journal,"Choose a known contact. Fly there to continue the survey.","menu-copy");
            list=Box(journal,"journal-list");archive=Text(journal,"","survey-help");
            journal.Add(new Button(()=>{world.Discovery.SaveNow();world.Empire.SaveNow();}){text="SAVE SURVEY & COLONIES / F5"});
            journal.Add(new Button(()=>world.Empire.ToggleManagement()){text="MANAGE SELECTED COLONY / B"});
            journal.Add(new Button(()=>world.Discovery.ToggleJournal()){text="RESUME FLIGHT  /  J OR ESC"});
        }
        static VisualElement Box(VisualElement parent,string cls){var element=new VisualElement();element.AddToClassList(cls);parent.Add(element);return element;}
        static Label Text(VisualElement parent,string value,string cls){var label=new Label(value);label.AddToClassList(cls);label.pickingMode=PickingMode.Ignore;parent.Add(label);return label;}
        public static string Details(WorldDefinition world,DiscoveryState state)
        {
            if(state<DiscoveryState.Scanned)return "UNRESOLVED PLANETARY CONTACT\n\nApproach and maintain scanner range to analyse this world.\n\nAtmosphere     —\nResources       —\nLife signs         —";
            var s=world.survey;
            string result=$"{s.type}\n\nAtmosphere   {s.atmosphere}\nTemperature   {s.temperature} °C\n\nHabitability   {s.habitability}%\nMinerals         {s.minerals}%\nEnergy           {s.energy}%\n\nLife   {s.life}\nCivilization   {s.civilization}\n\n{s.trait.ToUpperInvariant()}";
            return result+(state==DiscoveryState.Visited?$"\n\nOwner   {s.owner}\nDevelopment   {s.development} / 3\nDefense   {s.defense}":"\n\nStrategic data: enter close orbit to verify.");
        }
        void Update()
        {
            if(world==null)return;var d=world.Discovery;
            bool show=d.Target!=null&&!world.Player.Paused&&!world.Combat.ActiveThreat;
            card.style.display=show?DisplayStyle.Flex:DisplayStyle.None;
            transmission.text=d.Transmission;transmission.style.display=world.Player.Paused?DisplayStyle.None:DisplayStyle.Flex;
            if(show)
            {
                var discovery=d.Catalog.State(d.Target.Definition.id);
                title.text=d.TargetLabel;state.text=discovery.ToString().ToUpperInvariant()+"  /  "+Mathf.Max(0,d.TargetRange).ToString("N0")+" m";
                scanHint.text=d.ScanHint;
                var colony=world.Empire.State.Get(d.Target.Definition.id);
                if(detailsId!=colony.id||detailsState!=discovery||detailsRevision!=world.Empire.State.Revision)
                {
                    detailsId=colony.id;detailsState=discovery;detailsRevision=world.Empire.State.Revision;
                    details.text=StrategicDetails(d.Target.Definition,discovery,colony);
                }
                scanTrack.style.display=discovery==DiscoveryState.Detected?DisplayStyle.Flex:DisplayStyle.None;
                scanFill.style.width=Length.Percent(d.Scan.Progress*100);
                if(d.Scan.Progress>0)state.text="ANALYSIS  /  "+(d.Scan.Progress*100).ToString("0")+"%";
            }
            journal.style.display=d.JournalOpen?DisplayStyle.Flex:DisplayStyle.None;
            archive.text=world.Session.Status;
            string target=d.Target==null?"":d.Target.Definition.id;
            if(revision==d.Catalog.Revision&&target==selected)return;
            revision=d.Catalog.Revision;selected=target;list.Clear();
            foreach(var planet in world.Worlds)
            {
                var entry=d.Catalog.State(planet.Definition.id);if(entry==DiscoveryState.Unknown)continue;
                var captured=planet;
                string label=d.Catalog.HasDetails(planet.Definition.id)?planet.Definition.title:"UNKNOWN PLANET";
                list.Add(new Button(()=>d.SelectTarget(captured)){text=(planet==d.Target?"▸  ":"")+label+"  /  "+entry.ToString().ToUpperInvariant()});
            }
        }
        public static string StrategicDetails(WorldDefinition planet,DiscoveryState discovery,ColonyState colony)
        {
            if(discovery!=DiscoveryState.Visited)return Details(planet,discovery);
            string owner=colony.faction==1?"Wayfarer Union":colony.faction==2?"Ithacan Assembly":colony.faction==3?"Cinder Dominion":"Unclaimed";
            string result=Details(planet,DiscoveryState.Scanned).Replace("Strategic data: enter close orbit to verify.",$"Owner   {owner}\nDevelopment   {colony.Development} / 3\nDefense   {colony.Defense}");
            return colony.Development>0?result.Replace("Civilization   None","Civilization   Colonial"):result;
        }
    }
}
