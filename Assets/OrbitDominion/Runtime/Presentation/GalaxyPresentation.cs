using System.Collections.Generic;
using UnityEngine;
namespace OrbitDominion
{
    public sealed class GalaxyPresentation : MonoBehaviour
    {
        PrototypeBootstrap world;Light sun;readonly List<Material> materials=new();readonly Dictionary<string,GameObject> shields=new();
        readonly List<GameObject> anomalies=new();float elapsed;
        public void Initialize(PrototypeBootstrap world,Light sun)
        {
            this.world=world;this.sun=sun;
            Color[] colors={new(1,.75f,.35f),new(.45f,.7f,1),new(1,.25f,.07f)};
            for(int i=0;i<3;i++)
            {
                var mat=VisualFactory.Lit(colors[i],0,colors[i]*3);materials.Add(mat);
                VisualFactory.Shape(GalaxyCatalog.Systems[i]+" star",PrimitiveType.Sphere,transform,GalaxyCatalog.StarCenters[i],Vector3.one*GalaxyCatalog.StarRadius*2,mat);
            }
            for(int i=0;i<3;i++)
            {
                var track=VisualFactory.Lit(colors[i]*.09f,.9f,colors[i]*.11f);materials.Add(track);
                foreach(var planet in world.Worlds)if(planet.Definition.system==i)
                {
                    var path=OrbitalRules.Planet(planet.Definition);
                    var route=new GameObject(planet.Definition.title+" / stellar orbit").AddComponent<LineRenderer>();
                    route.transform.SetParent(transform,false);route.useWorldSpace=true;route.loop=true;route.positionCount=144;
                    route.widthMultiplier=15;route.numCapVertices=0;route.sharedMaterial=track;
                    route.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;route.receiveShadows=false;
                    for(int sample=0;sample<144;sample++)route.SetPosition(sample,path.Position(path.period*sample/144));
                }
            }
            var anomalyMaterial=VisualFactory.Lit(new Color(.2f,.1f,.5f),.5f,new Color(.8f,.2f,2));materials.Add(anomalyMaterial);
            for(int i=0;i<3;i++)
            {
                var root=new GameObject("Signal / "+SectorController.AnomalyIds[i]).transform;root.SetParent(transform);root.position=SectorController.AnomalyPositions[i];anomalies.Add(root.gameObject);
                if(i==1){VisualFactory.Ship(root,anomalyMaterial,anomalyMaterial,5);root.rotation=Quaternion.Euler(30,70,40);}
                else for(int j=0;j<3;j++){var ring=VisualFactory.Ring("Ancient resonance",root,25+j*10,.5f,anomalyMaterial,96);ring.transform.localRotation=Quaternion.Euler(j*60,j*30,0);ring.gameObject.AddComponent<OrbitMotion>().degreesPerSecond=5+j*3;}
            }
            var shieldMaterial=new Material(Shader.Find("Orbit Dominion/Atmosphere"));shieldMaterial.SetColor("_Color",new Color(.07f,.7f,1));materials.Add(shieldMaterial);
            foreach(var planet in world.Worlds)
            {
                var dome=VisualFactory.Shape("Planetary protection",PrimitiveType.Sphere,planet.transform,Vector3.zero,Vector3.one*(planet.Definition.radius+180)*2,shieldMaterial);dome.SetActive(false);shields.Add(planet.Definition.id,dome);
            }
        }
        void Update()
        {
            elapsed+=Time.deltaTime;if(elapsed<.2f)return;elapsed=0;
            foreach(var p in world.Sector.State.Data.planets)shields[p.id].SetActive(p.protection>0);
            foreach(var p in world.Worlds)p.SetDetail(Vector3.Distance(world.Player.transform.position,p.transform.position)<6000);
            for(int i=0;i<anomalies.Count;i++)anomalies[i].SetActive(!world.Sector.State.Data.anomalies.Contains(SectorController.AnomalyIds[i]));
            int system=world.Sector.CurrentSystem;Color tint=system==0?new Color(.045f,.12f,.24f):system==1?new Color(.13f,.06f,.3f):new Color(.22f,.04f,.025f);
            RenderSettings.skybox.SetColor("_Tint",Color.Lerp(RenderSettings.skybox.GetColor("_Tint"),tint,.08f));
            sun.color=Color.Lerp(sun.color,system==0?new Color(1,.92f,.83f):system==1?new Color(.7f,.85f,1):new Color(1,.58f,.34f),.08f);
            Vector3 starlight=world.Player.transform.position-GalaxyCatalog.StarCenters[system];
            if(starlight.sqrMagnitude>1)sun.transform.rotation=Quaternion.LookRotation(starlight.normalized);
        }
        void OnDestroy(){foreach(var material in materials)if(material!=null)Destroy(material);}
    }
}
