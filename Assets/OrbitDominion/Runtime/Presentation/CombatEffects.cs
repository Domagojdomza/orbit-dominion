using System.Collections.Generic;
using UnityEngine;
namespace OrbitDominion
{
    // Reusable short-lived effects: sustained battles do not allocate a GameObject per shot.
    public sealed class CombatEffects : MonoBehaviour
    {
        sealed class BeamItem { public LineRenderer line;public float remaining; }
        sealed class FlashItem { public Transform root;public LineRenderer[] rings;public float elapsed,duration,radius; }
        readonly List<BeamItem> beams=new();readonly List<FlashItem> flashes=new();
        Material cyan,red,gold;
        public void Initialize()
        {
            cyan=VisualFactory.Lit(Color.cyan,0,new Color(.1f,3,4));red=VisualFactory.Lit(new Color(1,.1f,.04f),0,new Color(4,.2f,.03f));gold=VisualFactory.Lit(Color.yellow,0,new Color(4,1.3f,.1f));
        }
        public void Beam(Vector3 from,Vector3 to,bool hostile)
        {
            BeamItem item=null;foreach(var beam in beams)if(beam.remaining<=0){item=beam;break;}
            if(item==null&&beams.Count>=64)item=beams[0];
            if(item==null)
            {
                item=new BeamItem{line=new GameObject("Energy beam").AddComponent<LineRenderer>()};item.line.transform.SetParent(transform);item.line.positionCount=2;item.line.widthMultiplier=.4f;beams.Add(item);
            }
            item.remaining=.12f;item.line.gameObject.SetActive(true);item.line.SetPosition(0,from);item.line.SetPosition(1,to);item.line.sharedMaterial=hostile?red:cyan;
        }
        public void Impact(Vector3 position,float radius,bool shield,bool destroyed)
        {
            FlashItem item=null;foreach(var flash in flashes)if(!flash.root.gameObject.activeSelf){item=flash;break;}
            if(item==null&&flashes.Count>=32)item=flashes[0];
            if(item==null)
            {
                item=new FlashItem{root=new GameObject("Impact").transform,rings=new LineRenderer[3]};item.root.SetParent(transform);
                for(int i=0;i<3;i++){item.rings[i]=VisualFactory.Ring("Impact wave",item.root,1,.15f,cyan,48);item.rings[i].transform.localRotation=Quaternion.Euler(i*60,30+i*50,20);}
                flashes.Add(item);
            }
            item.root.name=destroyed?"Combat explosion":"Shield impact";item.root.position=position;item.elapsed=0;item.duration=destroyed?.6f:.25f;item.radius=radius*(destroyed?1.8f:1.1f);item.root.localScale=Vector3.one*item.radius;item.root.gameObject.SetActive(true);
            for(int i=0;i<3;i++){item.rings[i].gameObject.SetActive(destroyed||i==0);item.rings[i].sharedMaterial=shield&&!destroyed?cyan:gold;item.rings[i].widthMultiplier=(destroyed?.7f:.25f)/Mathf.Max(1,item.radius);}
        }
        void Update()
        {
            float dt=Time.deltaTime;
            foreach(var beam in beams)if(beam.remaining>0){beam.remaining-=dt;if(beam.remaining<=0)beam.line.gameObject.SetActive(false);}
            foreach(var flash in flashes)if(flash.root.gameObject.activeSelf){flash.elapsed+=dt;flash.root.localScale=Vector3.one*flash.radius*(1+flash.elapsed*3);if(flash.elapsed>=flash.duration)flash.root.gameObject.SetActive(false);}
        }
        void OnDestroy(){if(cyan!=null)Destroy(cyan);if(red!=null)Destroy(red);if(gold!=null)Destroy(gold);}
    }
}
