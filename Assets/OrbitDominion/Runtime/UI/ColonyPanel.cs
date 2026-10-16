using UnityEngine;
using UnityEngine.UIElements;

namespace OrbitDominion
{
    [DefaultExecutionOrder(110)]
    public sealed class ColonyPanel : MonoBehaviour
    {
        PrototypeBootstrap world;
        Label resources,context,title,summary,status;
        VisualElement panel;
        readonly Button[] buildings=new Button[5];
        public void Initialize(PrototypeBootstrap world,VisualElement root)
        {
            this.world=world;resources=Text(root,"","resources");context=Text(root,"","colony-context");
            panel=new VisualElement();panel.AddToClassList("colony-panel");root.Add(panel);
            Text(panel,"WAYFARER UNION / PLANET MANAGEMENT","eyebrow");title=Text(panel,"","survey-title");summary=Text(panel,"","survey-details");
            for(int i=0;i<5;i++)
            {
                var kind=(BuildingKind)i;buildings[i]=new Button(()=>world.Empire.Build(kind));buildings[i].AddToClassList("building-button");panel.Add(buildings[i]);
            }
            status=Text(panel,"","survey-help");
            panel.Add(new Button(()=>world.Sector.ProtectSelected()){text="PLANETARY PROTECTION / H"});
            panel.Add(new Button(()=>{world.Discovery.SaveNow();world.Empire.SaveNow();}){text="SAVE COLONIES & SURVEY / F5"});
            panel.Add(new Button(()=>world.Empire.CloseManagement()){text="RESUME FLIGHT & CONSTRUCTION / B OR ESC"});
        }
        static Label Text(VisualElement parent,string text,string cls){var label=new Label(text);label.AddToClassList(cls);parent.Add(label);return label;}
        void Update()
        {
            if(world?.Empire?.State==null)return;var e=world.Empire;var s=e.State;var production=s.TotalProduction;
            resources.text=$"MINERALS  {s.Minerals:0}  +{production.x:0.0}/s     ENERGY  {s.Energy:0}  +{production.y:0.0}/s";
            context.style.display=world.Player.Paused||world.Combat.ActiveThreat?DisplayStyle.None:DisplayStyle.Flex;
            context.text=e.ColonyHint+"\n"+e.Message;
            panel.style.display=e.ManagementOpen?DisplayStyle.Flex:DisplayStyle.None;
            if(!e.ManagementOpen)return;
            var c=s.Get(e.ManagedId);if(c==null)return;
            var planet=System.Array.Find(world.Worlds,p=>p.Definition.id==c.id);title.text=planet.Definition.title;
            var income=s.Production(c.id);
            string work=c.construction>=0?$"{EmpireState.BuildingName((BuildingKind)c.construction)} · {c.constructionRemaining:0}s remaining":"Construction berth available";
            summary.text=$"WAYFARER UNION · DEVELOPMENT {c.Development}/3 · DEFENSE {c.Defense}\nBUILD SLOTS {c.UsedSlots}/{c.Slots} · +{income.x:0.0} minerals/s · +{income.y:0.0} energy/s\n\n{work}\nSimulation paused. Resume flight to advance work.";
            string[] effects={"Mineral production","Energy production","Cities, traffic and build slots","+30 planetary defense rating per level","+10% production per level and civilian traffic"};
            for(int i=0;i<5;i++)
            {
                var kind=(BuildingKind)i;var cost=EmpireState.Cost(kind,c.levels[i]+1);string reason=s.BuildBlock(c.id,kind);
                buildings[i].text=$"{EmpireState.BuildingName(kind).ToUpperInvariant()} · LEVEL {c.levels[i]}/3\n"+(reason??$"{cost.x:0} M / {cost.y:0} E · {e.Settings.constructionSeconds*(c.levels[i]+1):0}s")+"\n"+effects[i];
                buildings[i].SetEnabled(reason==null);
            }
            var protection=world.Sector.State.Get(c.id);status.text=$"PROTECTION {protection.protection:0}s · COOLDOWN {protection.cooldown:0}s · COST {world.sectorSettings.shieldCost:0} ENERGY\n"+(world.Sector.State.ProtectionBlock(c.id,s)??"Ready: protects infrastructure and suspends defensive fire")+"\n"+world.Session.Status;
        }
    }
}
