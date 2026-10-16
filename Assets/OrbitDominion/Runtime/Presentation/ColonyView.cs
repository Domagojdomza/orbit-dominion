using UnityEngine;

namespace OrbitDominion
{
    // A deterministic projection of colony state, rebuilt only when a building completes.
    public sealed class ColonyView : MonoBehaviour
    {
        PrototypeBootstrap world;
        PlanetView planet;
        Transform infrastructure,lander;
        Material metal,light,solar;
        int signature=-1;
        public int VisibleDevelopment {get;private set;}
        public int VisibleDefenses {get;private set;}
        public void Initialize(PrototypeBootstrap world,PlanetView planet)
        {
            this.world=world;this.planet=planet;
            metal=VisualFactory.Lit(new Color(.14f,.24f,.29f),.7f);
            light=VisualFactory.Lit(new Color(.1f,.65f,.65f),.4f,new Color(.15f,1.2f,1.3f));
            solar=VisualFactory.Lit(new Color(.04f,.12f,.35f),.65f);
        }
        void LateUpdate()
        {
            if(world?.Empire?.State==null)return;var c=world.Empire.State.Get(planet.Definition.id);
            if(c.deploying)
            {
                if(lander==null){lander=VisualFactory.Ship(transform,metal,light,1.8f);lander.name="Colony deployment craft";VisualFactory.Ring("Deployment beacon",lander,10,.3f,light);}
                lander.gameObject.SetActive(true);
                float height=Mathf.Lerp(8,150,c.deploymentRemaining/world.empireSettings.deploymentSeconds);
                lander.localPosition=c.landingDirection*(planet.Definition.radius+height);
                lander.localRotation=Quaternion.LookRotation(-c.landingDirection,Mathf.Abs(Vector3.Dot(c.landingDirection,Vector3.up))>.95f?Vector3.right:Vector3.up);
            }
            else if(lander!=null)lander.gameObject.SetActive(false);
            int next=c.faction;foreach(int level in c.levels)next=next*4+level;
            if(signature==next)return;signature=next;
            if(infrastructure!=null){infrastructure.gameObject.SetActive(false);Destroy(infrastructure.gameObject);}
            VisibleDevelopment=c.Development;VisibleDefenses=c.levels[3];
            if(c.Development==0)return;
            light.SetColor("_EmissionColor",c.faction==3?new Color(2,.2f,.03f):c.faction==2?new Color(.8f,1.2f,.2f):new Color(.15f,1.2f,1.3f));
            planet.SetDevelopment(c.Development);
            // Home already has its original skyline and station; additional modules remain dynamic.
            infrastructure=new GameObject("Colony infrastructure").transform;infrastructure.SetParent(transform,false);
            if(!planet.Definition.developed)
            {
                var settlement=new GameObject("Settlement / level "+c.Development).transform;settlement.SetParent(infrastructure,false);
                settlement.localRotation=Quaternion.FromToRotation(Vector3.up,c.landingDirection);
                for(int i=0;i<c.Development*18;i++)
                {
                    float angle=i*2.39996f,r=4+Mathf.Sqrt(i)*4;
                    Vector3 n=new Vector3(Mathf.Cos(angle)*r,planet.Definition.radius,Mathf.Sin(angle)*r).normalized;
                    float height=3+(i%5)*c.Development;
                    var tower=VisualFactory.Shape("Colony arcology",PrimitiveType.Cube,settlement,n*(planet.Definition.radius+height/2),new Vector3(3,height,3),i%3==0?light:metal);
                    tower.transform.localRotation=Quaternion.FromToRotation(Vector3.up,n);
                }
            }
                for(int kind=0;kind<2;kind++)for(int i=0;i<c.levels[kind]*4;i++)
                {
                    Vector3 n=Quaternion.AngleAxis((kind==0?-1:1)*(12+i*2),Vector3.up)*c.landingDirection;
                    var module=VisualFactory.Shape(kind==0?"Mineral refinery":"Energy collector",PrimitiveType.Cube,infrastructure,n*(planet.Definition.radius+3),kind==0?new Vector3(7,7,7):new Vector3(12,1,8),kind==0?metal:solar);
                    module.transform.localRotation=Quaternion.FromToRotation(Vector3.up,n);
                }
            for(int i=0;i<c.levels[3];i++)
            {
                var orbit=Orbit("Defense satellite",i*100,1.5f);
                var satellite=VisualFactory.Shape("Defense core",PrimitiveType.Cube,orbit,new Vector3(planet.Definition.radius+60,0,0),new Vector3(7,5,7),metal);
                satellite.AddComponent<SphereCollider>().radius=.8f;
                VisualFactory.Shape("Defense emitter",PrimitiveType.Sphere,satellite.transform,new Vector3(0,.7f,0),Vector3.one*.5f,light);
                VisualFactory.Shape("Satellite panels",PrimitiveType.Cube,satellite.transform,Vector3.zero,new Vector3(4,.1f,.7f),solar);
                var faction=c.owned?CombatFaction.Union:c.faction==2?CombatFaction.Neutral:CombatFaction.Hostile;
                if(!c.owned&&world.Sector!=null&&world.Sector.State.Get(c.id).provoked)faction=CombatFaction.Hostile;
                world.Combat.RegisterSatellite(satellite,c.id+"/defense/"+i,faction);
            }
            if(c.levels[4]>(planet.Definition.developed?1:0))
            {
                var orbit=Orbit("Colony orbital station",40,.7f);
                var station=new GameObject("Orbital station / level "+c.levels[4]).transform;station.SetParent(orbit,false);station.localPosition=c.landingDirection*(planet.Definition.radius+85);
                station.gameObject.AddComponent<SphereCollider>().radius=22+c.levels[4]*3;
                VisualFactory.Ring("Station habitat",station,17+c.levels[4]*3,2,metal);VisualFactory.Ring("Station lights",station,17+c.levels[4]*3,.35f,light);
                VisualFactory.Shape("Station core",PrimitiveType.Cylinder,station,Vector3.zero,new Vector3(6,15,6),metal);
            }
            if(!planet.Definition.developed)for(int i=0;i<c.Development*2+c.levels[4]*2;i++)
            {
                var orbit=Orbit("Colony civilian route",i*47,2+i*.3f);
                var ship=VisualFactory.Ship(orbit,metal,light,.4f);ship.localPosition=new Vector3(planet.Definition.radius+30+i*4,0,0);
            }
        }
        Transform Orbit(string name,float angle,float speed)
        {
            var orbit=new GameObject(name).transform;orbit.SetParent(infrastructure,false);orbit.localRotation=Quaternion.Euler(15,angle,20);
            orbit.gameObject.AddComponent<OrbitMotion>().degreesPerSecond=speed;return orbit;
        }
        void OnDestroy(){if(metal!=null)Destroy(metal);if(light!=null)Destroy(light);if(solar!=null)Destroy(solar);}
    }
}
