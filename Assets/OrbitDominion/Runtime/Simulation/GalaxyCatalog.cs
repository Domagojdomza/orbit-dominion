using System.Collections.Generic;
using UnityEngine;
namespace OrbitDominion
{
    public enum PlanetArchetype { Earth, Desert, Frozen, Volcanic, Rocky, Toxic }
    public static class GalaxyCatalog
    {
        public static readonly string[] Systems={"SOLACE","VEIL FRONTIER","CINDER REACH"};
        public static readonly Vector3[] Centers={Vector3.zero,new(18000,1200,10000),new(36000,-800,17000)};
        // Actual stellar positions. Centers remain the established safe navigation waypoints.
        public static readonly Vector3[] StarCenters={new(0,0,-2500),new(18000,1200,6000),new(35000,-800,13000)};
        public static readonly float[] StarMasses={1f,.85f,1.25f};
        public const float StarRadius=325f;
        public static int SystemAt(Vector3 position){int best=0;for(int i=1;i<3;i++)if((position-Centers[i]).sqrMagnitude<(position-Centers[best]).sqrMagnitude)best=i;return best;}
        public static WorldDefinition[] Expand(WorldDefinition[] home,int seed)
        {
            var list=new List<WorldDefinition>(home);
            for(int i=0;i<home.Length;i++){home[i].system=0;home[i].archetype=i==2?PlanetArchetype.Frozen:PlanetArchetype.Earth;}
            list.Add(Create("solace-04","KESTREL",0,PlanetArchetype.Rocky,new(4200,-300,-2200),210,0,seed+3));
            list.Add(Create("veil-01","SEREIN",1,PlanetArchetype.Desert,new(-2300,100,-1400),290,0,seed+4));
            list.Add(Create("veil-02","ELYSIA",1,PlanetArchetype.Earth,new(1300,-100,1200),340,0,seed+5));
            list.Add(Create("veil-03","ITHACA",1,PlanetArchetype.Earth,new(3300,200,-1000),310,2,seed+6));
            list.Add(Create("veil-04","UMBRA",1,PlanetArchetype.Toxic,new(-1200,-350,3200),260,0,seed+7));
            list.Add(Create("cinder-01","VESPER",2,PlanetArchetype.Desert,new(-2000,80,-1000),310,3,seed+8));
            list.Add(Create("cinder-02","PYRE",2,PlanetArchetype.Volcanic,new(1500,-200,1400),360,3,seed+9));
            list.Add(Create("cinder-03","BASTION",2,PlanetArchetype.Rocky,new(3400,180,-1700),290,3,seed+10));
            list.Add(Create("cinder-04","MORROW",2,PlanetArchetype.Frozen,new(-1200,450,4000),330,3,seed+11));
            return list.ToArray();
        }
        static WorldDefinition Create(string id,string name,int system,PlanetArchetype type,Vector3 offset,float radius,int owner,int seed)
        {
            Color ocean,land,high,air;float threshold=.48f,cloud=.35f;int hab,temp,minerals,energy;
            switch(type)
            {
                case PlanetArchetype.Desert:ocean=new(.2f,.08f,.025f);land=new(.6f,.32f,.1f);high=new(.83f,.64f,.35f);air=new(1,.48f,.16f);hab=48;temp=47;minerals=68;energy=92;cloud=.1f;threshold=.15f;break;
                case PlanetArchetype.Frozen:ocean=new(.14f,.23f,.32f);land=new(.43f,.62f,.72f);high=new(.86f,.94f,1);air=new(.2f,.55f,1);hab=22;temp=-71;minerals=91;energy=40;break;
                case PlanetArchetype.Volcanic:ocean=new(.8f,.08f,.005f);land=new(.08f,.025f,.02f);high=new(.3f,.13f,.08f);air=new(1,.17f,.035f);hab=30;temp=180;minerals=98;energy=94;cloud=.15f;threshold=.3f;break;
                case PlanetArchetype.Rocky:ocean=new(.11f,.09f,.08f);land=new(.28f,.23f,.2f);high=new(.6f,.52f,.4f);air=new(.53f,.48f,.38f);hab=36;temp=-12;minerals=86;energy=60;cloud=0;threshold=.1f;break;
                case PlanetArchetype.Toxic:ocean=new(.12f,.2f,.025f);land=new(.27f,.36f,.06f);high=new(.7f,.6f,.15f);air=new(.58f,.85f,.12f);hab=28;temp=63;minerals=78;energy=83;cloud=.85f;break;
                default:ocean=new(.015f,.12f,.2f);land=new(.08f,.3f,.16f);high=new(.46f,.58f,.32f);air=new(.1f,.65f,1);hab=86;temp=21;minerals=58;energy=76;break;
            }
            var random=new System.Random(seed);minerals=Mathf.Clamp(minerals+random.Next(-5,6),0,100);
            // Elysia's orbital rendering and landing biome describe the same open-water world.
            if(id=="veil-02"){threshold=.86f;cloud=.52f;ocean=new Color(.025f,.24f,.36f);air=new Color(.18f,.64f,.83f);}
            return new WorldDefinition(id,name,Centers[system]+offset,radius,ocean,land,high,air,threshold,cloud,false,type==PlanetArchetype.Frozen)
            {system=system,archetype=type,moon=type==PlanetArchetype.Earth||type==PlanetArchetype.Volcanic,
                survey=new PlanetSurvey(id=="veil-02"?"OCEAN WORLD":type.ToString().ToUpperInvariant()+" WORLD",type==PlanetArchetype.Earth?"Nitrogen / oxygen":type==PlanetArchetype.Toxic?"Corrosive aerosols":"Thin mineral atmosphere",temp,hab,minerals,energy,id=="veil-02"?"Pelagic biosphere":type==PlanetArchetype.Earth?"Complex biosphere":"Extremophile traces",owner==0?"None":owner==2?"Industrial":"Interplanetary",owner==2?"Ithacan Assembly":owner==3?"Cinder Dominion":"Unclaimed",owner==0?0:60,owner==0?0:2,id=="veil-02"?"Endless ocean / pelagic relics":type==PlanetArchetype.Volcanic?"Geothermal abundance":type==PlanetArchetype.Desert?"Solar abundance":"Ancient survey echoes")};
        }
    }
}
