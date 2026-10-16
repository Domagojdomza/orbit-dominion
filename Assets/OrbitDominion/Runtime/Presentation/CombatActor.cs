using UnityEngine;
namespace OrbitDominion
{
    public sealed class CombatActor : MonoBehaviour
    {
        public string Id {get;private set;}
        public CombatFaction Faction {get;private set;}
        public CombatRole Role {get;private set;}
        public VitalState Vitals {get;private set;}
        public float Radius {get;private set;}
        public float Cooldown, RepairRemaining;
        public Vector3 Home;
        public string HomePlanet,FleetId;
        public Vector3 Destination;
        public bool Traveling;
        public float SimulationElapsed;
        public void SetFaction(CombatFaction faction)=>Faction=faction;
        Renderer[] renderers;
        Collider[] colliders;
        public void Initialize(string id,CombatFaction faction,CombatRole role,VitalState vitals,float radius)
        {
            Id=id;Faction=faction;Role=role;Vitals=vitals;Radius=radius;Home=transform.position;
            renderers=GetComponentsInChildren<Renderer>();colliders=GetComponentsInChildren<Collider>();
        }
        void LateUpdate()
        {
            if(Vitals==null)return;
            foreach(var r in renderers)if(r!=null)r.enabled=Vitals.Alive;
            foreach(var c in colliders)if(c!=null)c.enabled=Vitals.Alive;
        }
    }
}
