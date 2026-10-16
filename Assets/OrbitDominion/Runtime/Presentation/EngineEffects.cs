using UnityEngine;
namespace OrbitDominion
{
    public sealed class EngineEffects : MonoBehaviour
    {
        public PlayerFlight player;
        Transform[] plumes;
        ParticleSystem streaks;
        public void Initialize(Transform hull, Material glow)
        {
            plumes = new Transform[2];
            for(int i=0;i<2;i++)
            {
                plumes[i]=VisualFactory.Shape("Ion plume",PrimitiveType.Sphere,hull,new Vector3(i==0?-1.9f:1.9f,-.05f,-3.4f),new Vector3(.4f,.4f,1),glow).transform;
                var trail=plumes[i].gameObject.AddComponent<TrailRenderer>(); trail.time=.3f; trail.startWidth=.3f; trail.endWidth=0; trail.sharedMaterial=glow;
            }
            streaks=new GameObject("Local travel dust").AddComponent<ParticleSystem>(); streaks.transform.SetParent(player.transform,false);
            streaks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=streaks.main; main.startLifetime=.45f; main.startSpeed=0; main.startSize=.06f; main.maxParticles=250;
            main.simulationSpace=ParticleSystemSimulationSpace.World; main.startColor=new Color(.25f,.55f,.8f,.7f);
            var shape=streaks.shape; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=new Vector3(100,70,120);
            var emission=streaks.emission; emission.rateOverTime=35;
            var renderer=streaks.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial=glow; renderer.renderMode=ParticleSystemRenderMode.Stretch;
            renderer.velocityScale=.02f; renderer.lengthScale=4;
            streaks.Play();
        }
        void Update()
        {
            float power=player.Pulse?4:player.Boosting?2:Mathf.Lerp(.25f,1,player.Speed/player.settings.normalSpeed);
            foreach(var t in plumes) t.localScale=new Vector3(.45f,.45f,power*2);
            var emission=streaks.emission; emission.rateOverTime=player.Paused?0:player.Pulse?200:30;
        }
    }
}
