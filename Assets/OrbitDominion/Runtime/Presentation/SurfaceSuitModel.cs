using UnityEngine;

namespace OrbitDominion
{
    public sealed partial class SurfaceAstronaut
    {
        readonly Transform[] suitHands = new Transform[2];

        // Original pressure-suit design. Layered cloth, flexible seals and serviceable hardware.
        void BuildExpeditionSuit(Material cloth, Material rubber, Material glass, Material trim, Material light)
        {
            var shell = Resources.Load<Material>("AstronautShell") ?? cloth;
            var hardware = Resources.Load<Material>("AstronautHardware") ?? rubber;
            torso = Joint("Explorer / torso", transform, Vector3.zero);
            var m = new SurfaceMeshBuilder();
            m.SuitSection(Vector3.zero,new[]{new Vector4(.94f,.145f,.13f,0),new Vector4(1.04f,.195f,.155f,0),
                new Vector4(1.17f,.205f,.16f,0),new Vector4(1.35f,.24f,.18f,-.005f),new Vector4(1.46f,.235f,.16f,-.005f),
                new Vector4(1.54f,.18f,.13f,0),new Vector4(1.59f,.12f,.11f,0)},cloth,.0045f);
            // Ribbed waist and curved load-bearing breastplate, over a continuous pressure layer.
            for(int r=0;r<4;r++) m.SuitSection(Vector3.zero,new[]{new Vector4(1.045f+r*.022f,.215f,.175f,0),
                new Vector4(1.053f+r*.022f,.220f,.181f,0),new Vector4(1.06f+r*.022f,.214f,.173f,0)},rubber);
            m.Box(new Vector3(0,1.36f,.16f),new Vector3(.365f,.285f,.083f),shell,.035f);
            m.Box(new Vector3(0,1.36f,.205f),new Vector3(.30f,.215f,.018f),rubber,.014f);
            m.Box(new Vector3(-.035f,1.40f,.22f),new Vector3(.19f,.078f,.018f),glass,.008f);
            for(int r=0;r<3;r++)m.Box(new Vector3(-.055f,1.42f-r*.018f,.231f),new Vector3(.11f-r*.02f,.005f,.003f),light,.001f);
            for(int r=0;r<3;r++)m.Cylinder(new Vector3(-.08f+r*.075f,1.31f,.224f),.018f,.016f,hardware,Quaternion.Euler(90,0,0),12);
            m.Box(new Vector3(.11f,1.405f,.226f),new Vector3(.025f,.045f,.012f),trim,.004f);
            for(int side=-1;side<=1;side+=2)
            {
                // Shoulder harness, belt fittings, oxygen connectors and curved service hose.
                m.Box(new Vector3(side*.19f,1.38f,.145f),new Vector3(.043f,.31f,.035f),rubber,.012f,Quaternion.Euler(0,0,side*-8));
                m.Box(new Vector3(side*.19f,1.27f,.169f),new Vector3(.052f,.048f,.017f),hardware,.006f);
                m.Box(new Vector3(side*.13f,1.075f,.165f),new Vector3(.08f,.065f,.025f),hardware,.008f);
                m.Cylinder(new Vector3(side*.135f,1.19f,.165f),.037f,.025f,hardware,Quaternion.Euler(90,0,0));
                m.Cylinder(new Vector3(side*.135f,1.19f,.184f),.024f,.025f,trim,Quaternion.Euler(90,0,0));
                Vector3[] hose={new(side*.135f,1.19f,.2f),new(side*.19f,1.13f,.22f),new(side*.25f,1.16f,.17f),
                    new(side*.265f,1.23f,.02f),new(side*.245f,1.29f,-.18f),new(side*.18f,1.30f,-.29f)};
                for(int h=0;h<hose.Length-1;h++)
                {
                    m.Beam(hose[h],hose[h+1],.021f,rubber);
                    for(int r=0;r<4;r++)m.Cylinder(Vector3.Lerp(hose[h],hose[h+1],(r+.5f)/4),.024f,.006f,hardware,
                        Quaternion.FromToRotation(Vector3.up,hose[h+1]-hose[h]),12);
                }
            }
            BuildLifeSupport(m,cloth,rubber,shell,hardware,trim,light);
            BuildHelmet(m,cloth,rubber,shell,hardware,glass,trim,light);
            m.Finish(torso,"Wayfarer / pressure torso and helmet");
            for(int i=0;i<2;i++) BuildSuitLimbs(i,cloth,rubber,shell,hardware,trim,light);
        }

        static void BuildLifeSupport(SurfaceMeshBuilder m,Material cloth,Material rubber,Material shell,Material metal,Material trim,Material light)
        {
            m.Box(new Vector3(0,1.30f,-.235f),new Vector3(.37f,.48f,.17f),rubber,.04f);
            m.Box(new Vector3(0,1.31f,-.32f),new Vector3(.325f,.43f,.105f),shell,.035f);
            m.Box(new Vector3(0,1.34f,-.38f),new Vector3(.24f,.22f,.018f),metal,.015f);
            for(int r=0;r<8;r++)m.Box(new Vector3(0,1.27f+r*.022f,-.394f),new Vector3(.18f,.008f,.014f),rubber,.002f);
            m.Box(new Vector3(0,1.53f,-.33f),new Vector3(.18f,.025f,.06f),trim,.006f);
            m.Box(new Vector3(0,1.13f,-.38f),new Vector3(.17f,.042f,.025f),rubber,.005f);
            for(int r=0;r<4;r++)m.Box(new Vector3(-.05f+r*.035f,1.13f,-.397f),new Vector3(.018f,.009f,.004f),light,.001f);
            for(int s=-1;s<=1;s+=2)
            {
                m.SuitSection(new Vector3(s*.215f,1.30f,-.26f),new[]{new Vector4(-.19f,.035f,.035f,0),
                    new Vector4(-.155f,.055f,.055f,0),new Vector4(.145f,.055f,.055f,0),new Vector4(.18f,.03f,.03f,0)},shell);
                for(int r=0;r<2;r++)m.Cylinder(new Vector3(s*.215f,1.2f+r*.2f,-.26f),.057f,.035f,rubber);
                m.Cylinder(new Vector3(s*.215f,1.49f,-.26f),.022f,.04f,metal);
                m.Box(new Vector3(s*.155f,1.32f,-.383f),new Vector3(.022f,.3f,.02f),rubber,.006f);
                for(int r=0;r<3;r++)m.Cylinder(new Vector3(s*.14f,1.17f+r*.14f,-.391f),.008f,.008f,metal,Quaternion.Euler(90,0,0),8);
            }
            m.Beam(new Vector3(.14f,1.5f,-.26f),new Vector3(.14f,1.75f,-.28f),.008f,metal,.004f);
        }

        static void BuildHelmet(SurfaceMeshBuilder m,Material cloth,Material rubber,Material shell,Material metal,Material glass,Material trim,Material light)
        {
            m.Cylinder(new Vector3(0,1.58f,0),.139f,.075f,rubber);
            m.Cylinder(new Vector3(0,1.615f,0),.153f,.025f,metal);
            m.Ellipsoid(new Vector3(0,1.795f,-.008f),new Vector3(.375f,.405f,.385f),shell,32,48);
            m.VisorPatch(new Vector3(0,1.795f,-.008f),new Vector3(.193f,.207f,.201f),78,-34,44,rubber);
            m.VisorPatch(new Vector3(0,1.795f,-.008f),new Vector3(.196f,.209f,.204f),70,-27,36,glass);
            // Raised brow, chin guard and separate hinges frame the recessed curved visor.
            m.VisorPatch(new Vector3(0,1.795f,-.008f),new Vector3(.2f,.218f,.211f),74,37,45,shell);
            m.VisorPatch(new Vector3(0,1.795f,-.008f),new Vector3(.201f,.213f,.21f),73,-38,-29,shell);
            for(int s=-1;s<=1;s+=2)
            {
                var sideRotation=Quaternion.Euler(0,0,90);
                m.Cylinder(new Vector3(s*.184f,1.79f,-.005f),.054f,.035f,rubber,sideRotation);
                m.Cylinder(new Vector3(s*.206f,1.79f,-.005f),.037f,.014f,metal,sideRotation);
                m.Box(new Vector3(s*.165f,1.93f,.035f),new Vector3(.045f,.045f,.075f),rubber,.01f);
                m.Box(new Vector3(s*.165f,1.93f,.077f),new Vector3(.031f,.022f,.012f),light,.005f);
                m.Box(new Vector3(s*.10f,1.64f,.13f),new Vector3(.05f,.035f,.025f),rubber,.008f);
            }
            m.Box(new Vector3(0,1.65f,.16f),new Vector3(.10f,.033f,.023f),metal,.007f);
            for(int r=0;r<5;r++)m.Box(new Vector3(-.034f+r*.017f,1.65f,.173f),new Vector3(.007f,.015f,.005f),rubber,.001f);
        }

        void BuildSuitLimbs(int i,Material cloth,Material rubber,Material shell,Material metal,Material trim,Material light)
        {
            float side=i==0?-1:1;
            thighs[i]=Joint("Explorer / hip "+i,transform,new Vector3(side*.115f,1.01f,0));
            var m=new SurfaceMeshBuilder();
            m.SuitSection(Vector3.zero,new[]{new Vector4(-.45f,.091f,.10f,0),new Vector4(-.37f,.102f,.115f,0),
                new Vector4(-.20f,.118f,.135f,0),new Vector4(-.05f,.125f,.14f,0),new Vector4(.055f,.096f,.11f,0)},cloth,.0055f);
            m.Box(new Vector3(side*.111f,-.19f,.015f),new Vector3(.06f,.22f,.17f),rubber,.017f);
            m.Box(new Vector3(side*.143f,-.18f,.018f),new Vector3(.02f,.17f,.14f),cloth,.012f);
            m.Box(new Vector3(side*.15f,-.105f,.018f),new Vector3(.018f,.025f,.14f),trim,.005f);
            for(int r=0;r<2;r++)m.Box(new Vector3(side*.119f,-.15f-r*.08f,.1f),new Vector3(.025f,.027f,.012f),metal,.004f);
            m.Finish(thighs[i],"Wayfarer / tailored thigh and cargo pocket");
            knees[i]=Joint("Explorer / knee "+i,thighs[i],new Vector3(0,-.44f,0));
            m=new SurfaceMeshBuilder();
            m.Ellipsoid(Vector3.zero,new Vector3(.175f,.17f,.18f),rubber);
            m.SuitSection(Vector3.zero,new[]{new Vector4(-.435f,.078f,.09f,0),new Vector4(-.34f,.09f,.10f,-.005f),
                new Vector4(-.20f,.105f,.12f,-.018f),new Vector4(-.08f,.10f,.11f,0),new Vector4(.015f,.093f,.10f,0)},cloth,.005f);
            m.Box(new Vector3(0,-.015f,.104f),new Vector3(.155f,.16f,.058f),rubber,.025f);
            m.Box(new Vector3(0,-.012f,.138f),new Vector3(.12f,.117f,.02f),shell,.018f);
            for(int r=0;r<4;r++)m.Cylinder(new Vector3(0,-.35f+r*.021f,0),.115f,.012f,rubber);
            m.Box(new Vector3(side*.078f,-.21f,.048f),new Vector3(.031f,.10f,.048f),trim,.006f);
            m.Finish(knees[i],"Wayfarer / knee seal and lower leg");
            boots[i]=Joint("Explorer / ankle "+i,knees[i],new Vector3(0,-.43f,0));
            m=new SurfaceMeshBuilder();
            m.SuitSection(Vector3.zero,new[]{new Vector4(-.13f,.105f,.175f,.045f),new Vector4(-.085f,.102f,.17f,.05f),
                new Vector4(-.015f,.093f,.145f,.04f),new Vector4(.055f,.082f,.095f,0)},rubber);
            m.Box(new Vector3(0,-.135f,.048f),new Vector3(.218f,.042f,.35f),metal,.012f);
            m.Box(new Vector3(0,-.068f,.18f),new Vector3(.185f,.065f,.105f),shell,.022f);
            for(int r=0;r<6;r++)m.Box(new Vector3(0,-.16f,-.08f+r*.053f),new Vector3(.22f,.015f,.024f),rubber,.003f);
            m.Finish(boots[i],"Wayfarer / pressure boot and tread");
            arms[i]=Joint("Explorer / shoulder "+i,torso,new Vector3(side*.28f,1.48f,0));
            m=new SurfaceMeshBuilder();
            m.Ellipsoid(Vector3.up*.005f,new Vector3(.19f,.19f,.20f),rubber);
            m.SuitSection(Vector3.zero,new[]{new Vector4(-.315f,.079f,.09f,0),new Vector4(-.23f,.096f,.104f,0),
                new Vector4(-.11f,.113f,.12f,0),new Vector4(.035f,.11f,.12f,0),new Vector4(.09f,.067f,.077f,0)},cloth,.005f);
            m.Box(new Vector3(side*.10f,-.04f,0),new Vector3(.06f,.14f,.18f),shell,.026f);
            m.Box(new Vector3(side*.135f,-.05f,.005f),new Vector3(.012f,.075f,.10f),rubber,.008f);
            for(int r=0;r<3;r++)m.Box(new Vector3(side*.144f,-.028f-r*.021f,.005f),new Vector3(.008f,.009f,.067f),trim,.002f);
            m.Finish(arms[i],"Wayfarer / shoulder articulation and upper sleeve");
            elbows[i]=Joint("Explorer / elbow "+i,arms[i],new Vector3(0,-.30f,0));
            m=new SurfaceMeshBuilder();
            m.Ellipsoid(Vector3.zero,new Vector3(.145f,.145f,.15f),rubber);
            m.SuitSection(Vector3.zero,new[]{new Vector4(-.29f,.066f,.073f,0),new Vector4(-.20f,.078f,.084f,0),
                new Vector4(-.08f,.089f,.095f,0),new Vector4(.015f,.08f,.088f,0)},cloth,.0045f);
            for(int r=0;r<3;r++)m.Cylinder(new Vector3(0,-.055f+r*.025f,0),.102f,.01f,rubber);
            m.Cylinder(new Vector3(0,-.265f,0),.081f,.032f,metal);
            m.Cylinder(new Vector3(0,-.29f,0),.073f,.023f,trim);
            m.Box(new Vector3(0,-.15f,-.084f),new Vector3(.11f,.12f,.034f),rubber,.012f);
            m.Box(new Vector3(0,-.15f,-.105f),new Vector3(.078f,.073f,.01f),light,.004f);
            m.Finish(elbows[i],"Wayfarer / flexible elbow and wrist instrument");
            BuildGlove(i,rubber,cloth,shell);
        }

        void BuildGlove(int i,Material rubber,Material cloth,Material shell)
        {
            suitHands[i]=Joint("Explorer / wrist "+i,elbows[i],new Vector3(0,-.31f,0));
            var m=new SurfaceMeshBuilder();
            m.Box(Vector3.zero,new Vector3(.065f,.095f,.065f),cloth,.02f);
            m.Box(new Vector3(i==0?-.035f:.035f,0,0),new Vector3(.014f,.067f,.048f),shell,.007f);
            for(int f=0;f<4;f++)
            {
                float y=.032f-f*.021f;
                Vector3 a=new(0,y,.027f),b=new(-.042f,y,.043f),c=new(-.068f,y,.015f);
                if(i==0){a=new(.022f,-.025f,-.025f+f*.021f);b=new(.064f,-.02f,-.025f+f*.021f);c=new(.078f,.012f,-.025f+f*.021f);}
                m.Beam(a,b,.011f,cloth);m.Ellipsoid(b,Vector3.one*.025f,rubber);m.Beam(b,c,.0105f,rubber,.008f);
            }
            m.Beam(new Vector3(0,.039f,-.02f),new Vector3(i==0?.047f:-.04f,.048f,-.028f),.014f,cloth,.010f);
            m.Finish(suitHands[i],"Wayfarer / articulated grip glove");
        }
    }
}
