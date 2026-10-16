using UnityEngine;
using UnityEngine.UIElements;
namespace OrbitDominion
{
    [DefaultExecutionOrder(130)]
    public sealed class SectorPanel : MonoBehaviour
    {
        PrototypeBootstrap world;Camera lens;VisualElement root,panel;Label message,marker,victory,save;Label[] planetLabels=new Label[12];Button[] planetButtons=new Button[12];Label[] headings=new Label[3];
        public void Initialize(PrototypeBootstrap world,Camera lens,VisualElement root)
        {
            this.world=world;this.lens=lens;this.root=root;message=Text(root,"","sector-message");marker=Text(root,"","sector-waypoint");victory=Text(root,"","victory-status");
            panel=new VisualElement();panel.AddToClassList("sector-map");root.Add(panel);Text(panel,"SECTOR CHART / WAYFARER UNION","brand");
            Text(panel,"Choose a signal to navigate. Flight remains physical; M or Escape resumes.","survey-help");
            var columns=new VisualElement();columns.style.flexDirection=FlexDirection.Row;panel.Add(columns);
            for(int system=0;system<3;system++)
            {
                int selected=system;var column=new VisualElement();column.AddToClassList("system-column");columns.Add(column);
                headings[system]=Text(column,"","survey-title");column.Add(new Button(()=>world.Sector.NavigateSystem(selected)){text="NAVIGATE TO STAR"});
                for(int j=0;j<4;j++)
                {
                    int index=system*4+j;var p=world.Worlds[index];
                    var button=new Button(()=>world.Sector.Navigate(p.Definition.id));button.AddToClassList("planet-button");planetButtons[index]=button;column.Add(button);
                    planetLabels[index]=Text(column,"","survey-help");
                }
                if(system>0){int anomaly=system==1?0:2;column.Add(new Button(()=>world.Sector.Navigate(SectorController.AnomalyIds[anomaly])){text="INVESTIGATE UNKNOWN SIGNAL"});}
            }
            save=Text(panel,"","survey-help");
            panel.Add(new Button(()=>{world.Sector.State.Data.waypoint=null;world.Sector.State.Data.waypointSystem=-1;world.Sector.ToggleMap();}){text="FOLLOW CURRENT OBJECTIVE"});
            panel.Add(new Button(()=>world.Session.SaveNow()){text="SAVE FULL FLIGHT / F5"});
            panel.Add(new Button(()=>world.Sector.ToggleMap()){text="CONTINUE EXPLORING / M OR ESC"});
        }
        static Label Text(VisualElement parent,string text,string cls){var label=new Label(text);label.AddToClassList(cls);label.pickingMode=PickingMode.Ignore;parent.Add(label);return label;}
        void Update()
        {
            var sector=world.Sector;var data=sector.State.Data;
            panel.style.display=sector.MapOpen?DisplayStyle.Flex:DisplayStyle.None;
            message.style.display=world.Player.Paused?DisplayStyle.None:DisplayStyle.Flex;
            string capture=sector.CaptureHint(world.Discovery.Target?.Definition.id);
            message.text=(capture.Length>0?capture+"\n":"")+sector.Message;
            victory.text=data.won?"SECTOR DOMINION ACHIEVED / Continue exploring":$"UNION {sector.OwnedCount}/12 · HOLD {world.sectorSettings.victoryPlanets} WORLDS · {data.victory:0}/{world.sectorSettings.victorySeconds:0}s";
            save.text=world.Session.Status+"\n"+sector.Message;
            for(int s=0;s<3;s++)headings[s].text=s==0||s==1&&data.frontierVisited||s==2&&data.hostileVisited?GalaxyCatalog.Systems[s]:"UNIDENTIFIED STAR";
            for(int i=0;i<12;i++)
            {
                var p=world.Worlds[i];var d=world.Discovery.Catalog.State(p.Definition.id);var c=world.Empire.State.Get(p.Definition.id);var policy=sector.State.Get(c.id);
                planetButtons[i].text=(d>=DiscoveryState.Scanned?p.Definition.title:"UNKNOWN SIGNAL")+" / "+d.ToString().ToUpperInvariant();
                planetLabels[i].text=d==DiscoveryState.Visited?(c.faction==1?"UNION":c.faction==2?"NEUTRAL":c.faction==3?"DOMINION":"UNCLAIMED")+$" · DEVELOPMENT {c.Development}/3"+(policy.protection>0?$" · SHIELD {policy.protection:0}s":""):"Strategic information unresolved";
            }
            Vector3? guidance=sector.Guidance;bool show=guidance.HasValue&&!world.Player.Paused&&!world.Combat.ActiveThreat;
            marker.style.display=show?DisplayStyle.Flex:DisplayStyle.None;if(!show)return;
            var point=lens.WorldToViewportPoint(guidance.Value);bool behind=point.z<0;if(behind){point.x=1-point.x;point.y=1-point.y;}
            marker.style.left=Mathf.Clamp(point.x*root.resolvedStyle.width,25,root.resolvedStyle.width-280);marker.style.top=Mathf.Clamp((1-point.y)*root.resolvedStyle.height,250,root.resolvedStyle.height-280);
            marker.text=$"{(behind?"◁":"◇")} NAVIGATION SIGNAL · {Vector3.Distance(world.Player.transform.position,guidance.Value):0}m";
        }
    }
}
