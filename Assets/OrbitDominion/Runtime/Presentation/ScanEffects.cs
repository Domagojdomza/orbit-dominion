using UnityEngine;
namespace OrbitDominion
{
    public sealed class ScanEffects : MonoBehaviour
    {
        DiscoveryController discovery;
        GameObject shell;
        Material material;
        Mesh mesh;
        string last;
        float flash;
        public void Initialize(DiscoveryController controller)
        {
            discovery=controller;material=new Material(Shader.Find("Orbit Dominion/Scan"));mesh=PlanetMesh.Create();
            shell=VisualFactory.Shape("Holographic scan sweep",PrimitiveType.Sphere,transform,Vector3.zero,Vector3.one,material);
            shell.GetComponent<MeshFilter>().sharedMesh=mesh;shell.SetActive(false);
        }
        void LateUpdate()
        {
            if(discovery.LastDiscoveryId!=last){last=discovery.LastDiscoveryId;flash=1;}
            flash=Mathf.Max(0,flash-Time.deltaTime);
            if(discovery.Target!=null&&discovery.Target.Definition.id!=last)flash=0;
            bool active=discovery.Target!=null&&(discovery.Scan.Progress>0||flash>0);
            shell.SetActive(active);if(!active)return;
            shell.transform.position=discovery.Target.transform.position;
            shell.transform.localScale=Vector3.one*discovery.Target.Definition.radius*2.045f;
            material.SetFloat("_Progress",discovery.Scan.Progress);
            material.SetFloat("_Flash",flash);
        }
        void OnDestroy(){if(material!=null)Destroy(material);if(mesh!=null)Destroy(mesh);}
    }
}
