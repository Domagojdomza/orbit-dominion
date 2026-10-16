using UnityEngine;
using UnityEngine.UIElements;
namespace OrbitDominion
{
    [DefaultExecutionOrder(120)]
    public sealed class CombatPanel : MonoBehaviour
    {
        PrototypeBootstrap world;Camera lens;VisualElement root;
        Label health,message,target,objective;
        public void Initialize(PrototypeBootstrap world,Camera lens,VisualElement root)
        {
            this.world=world;this.lens=lens;this.root=root;
            health=Text("combat-health");message=Text("combat-message");target=Text("combat-target");objective=Text("combat-objective");
        }
        Label Text(string cls){var label=new Label();label.AddToClassList(cls);label.pickingMode=PickingMode.Ignore;root.Add(label);return label;}
        void Update()
        {
            var combat=world.Combat;var vital=combat.Player.Vitals;
            health.text=$"SHIELD {vital.Shield:0}/{vital.MaxShield:0}   HULL {vital.Hull:0}/{vital.MaxHull:0}";
            message.text=!vital.Alive?$"RESCUE IN {Mathf.Max(0,combat.RespawnRemaining):0.0}s / Colonies preserved":combat.Message;
            message.style.display=world.Player.Paused?DisplayStyle.None:DisplayStyle.Flex;
            bool developed=world.Sector==null&&world.Empire.State.Get(world.Worlds[1].Definition.id).levels[1]>0;
            objective.text=combat.HostilesRemaining==0?"SOLACE SECURED / Continue developing your colonies":$"CORSAIR SIGNAL / {Vector3.Distance(world.Player.transform.position,combat.OutpostPosition):0} m\nInvestigate the outpost near Nereid IV · LMB fire · R target";
            objective.style.display=developed&&!world.Player.Paused?DisplayStyle.Flex:DisplayStyle.None;
            bool signal=developed&&combat.HostilesRemaining>0&&combat.Target==null;
            bool show=(combat.Target!=null||signal)&&!world.Player.Paused;target.style.display=show?DisplayStyle.Flex:DisplayStyle.None;
            if(!show)return;
            var enemy=combat.Target;Vector3 targetPosition=signal?combat.OutpostPosition:enemy.transform.position;
            var point=lens.WorldToViewportPoint(targetPosition);bool behind=point.z<0;
            if(behind){point.x=1-point.x;point.y=1-point.y;}
            target.style.left=Mathf.Clamp(point.x*root.resolvedStyle.width,20,root.resolvedStyle.width-300);
            target.style.top=Mathf.Clamp((1-point.y)*root.resolvedStyle.height,230,root.resolvedStyle.height-260);
            float distance=Vector3.Distance(world.Player.transform.position,targetPosition);
            if(signal){target.text=$"{(behind?"◁":"◇")} CORSAIR SIGNAL · {distance:0} m";return;}
            target.text=$"{(behind?"◁":"◇")} {enemy.name.ToUpperInvariant()} · {distance:0} m\nSHIELD {enemy.Vitals.Shield:0} / HULL {enemy.Vitals.Hull:0}"+(distance>world.combatSettings.weaponRange?"\nOUT OF WEAPON RANGE":"");
        }
    }
}
