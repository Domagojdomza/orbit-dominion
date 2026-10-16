using UnityEngine;

namespace OrbitDominion
{
    [DefaultExecutionOrder(-110)]
    public sealed class GalaxyOrbitController : MonoBehaviour
    {
        PrototypeBootstrap world;
        OrbitalPath[] paths;
        public float Elapsed {get;private set;}

        public void Initialize(PrototypeBootstrap bootstrap)
        {
            world=bootstrap;
            paths=new OrbitalPath[world.Worlds.Length];
            for(int i=0;i<paths.Length;i++)paths[i]=OrbitalRules.Planet(world.Worlds[i].Definition);
        }

        public OrbitalPath Path(int index)=>paths[index];

        public void SetTime(float seconds)
        {
            if(paths==null)return;
            for(int i=0;i<paths.Length;i++)
            {
                var planet=world.Worlds[i];
                Vector3 old=planet.transform.position;
                Vector3 next=paths[i].Position(seconds);
                Vector3 shift=next-old;
                planet.transform.position=next;
                if(shift.sqrMagnitude<.000001f)continue;
                world.Combat?.FollowPlanetOrbit(planet,shift);
                if(world.Surface?.PlanetId==planet.Definition.id ||
                   world.Surface?.Active!=true && world.Player!=null && !world.Player.Pulse && world.Player.Speed<35 &&
                   (world.Player.transform.position-old).sqrMagnitude<Mathf.Pow(planet.Definition.radius+600,2))
                    world.Player.transform.position+=shift;
            }
            Elapsed=seconds;
            world.Sector?.RefreshFleetDestinations();
        }

        void Update()
        {
            if(world==null||world.Player.Paused)return;
            SetTime(Elapsed+Time.deltaTime);
        }
    }
}
