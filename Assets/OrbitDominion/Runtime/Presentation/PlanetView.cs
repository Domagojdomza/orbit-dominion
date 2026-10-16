using UnityEngine;

namespace OrbitDominion
{
    public sealed class PlanetView : MonoBehaviour
    {
        public WorldDefinition Definition { get; private set; }
        Mesh sphere, ringMesh;
        Mesh distantSphere;MeshFilter surfaceFilter,atmosphereFilter;
        public void SetDetail(bool close){if(surfaceFilter!=null){surfaceFilter.sharedMesh=close?sphere:distantSphere;atmosphereFilter.sharedMesh=close?sphere:distantSphere;}}
        Material surfaceMaterial, atmosphereMaterial, ringMaterial,moonMaterial;
        public void SetDevelopment(int level){if(surfaceMaterial!=null)surfaceMaterial.SetFloat("_Cities",level/3f);}
        void OnDestroy() { if(distantSphere!=null)Destroy(distantSphere);if(sphere!=null)Destroy(sphere);if(ringMesh!=null)Destroy(ringMesh);if(ringMaterial!=null)Destroy(ringMaterial);if(moonMaterial!=null)Destroy(moonMaterial);if(surfaceMaterial!=null)Destroy(surfaceMaterial);if(atmosphereMaterial!=null)Destroy(atmosphereMaterial); }
        void LateUpdate(){if(ringMaterial!=null)ringMaterial.SetVector("_PlanetCenter",transform.position);}
        public void Initialize(WorldDefinition d, int seed)
        {
            Definition = d; name = d.title; transform.position = d.center;
            var material = new Material(Shader.Find("Orbit Dominion/Planet"));
            surfaceMaterial=material;sphere=PlanetMesh.Create();distantSphere=PlanetMesh.Create(32,16);
            material.SetColor("_Ocean", d.ocean); material.SetColor("_Land", d.terrain); material.SetColor("_Highlands", d.highlands);
            material.SetFloat("_LandThreshold", d.land); material.SetFloat("_Clouds", d.clouds);
            material.SetFloat("_Seed", seed * .017f); material.SetFloat("_Cities", d.developed ? 1 : 0);
            material.SetFloat("_Ice",d.archetype==PlanetArchetype.Earth||d.archetype==PlanetArchetype.Frozen?1:0);
            material.SetFloat("_Lava",d.archetype==PlanetArchetype.Volcanic?1:0);
            material.SetColor("_CloudTint",d.archetype==PlanetArchetype.Toxic?new Color(.65f,.74f,.28f):d.archetype==PlanetArchetype.Desert?new Color(.85f,.75f,.54f):new Color(.82f,.89f,.96f));
            material.SetColor("_RimTint",d.atmosphere*.5f);
            var surface = VisualFactory.Shape("Procedural surface", PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one * d.radius * 2, material);
            surfaceFilter=surface.GetComponent<MeshFilter>();surfaceFilter.sharedMesh=sphere;
            surface.AddComponent<OrbitMotion>().degreesPerSecond = .3f;
            var atmosphere = new Material(Shader.Find("Orbit Dominion/Atmosphere")); atmosphere.SetColor("_Color", d.atmosphere);
            atmosphereMaterial=atmosphere;
            atmosphereFilter=VisualFactory.Shape("Atmospheric scattering", PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one * d.radius * 2.035f, atmosphere).GetComponent<MeshFilter>();atmosphereFilter.sharedMesh=sphere;
            if (d.rings)
            {
                var r = new GameObject("Ice rings").transform; r.SetParent(transform, false); r.localRotation = Quaternion.Euler(18,0,25);
                ringMaterial=new Material(Shader.Find("Orbit Dominion/Rings"));ringMaterial.SetFloat("_Radius",d.radius);ringMaterial.SetVector("_PlanetCenter",d.center);
                ringMesh=PlanetMesh.CreateRing(d.radius*1.3f,d.radius*1.95f);
                r.gameObject.AddComponent<MeshFilter>().sharedMesh=ringMesh;r.gameObject.AddComponent<MeshRenderer>().sharedMaterial=ringMaterial;
            }
            if (d.developed) Civilization(seed);
            if(d.moon)
            {
                var orbit=new GameObject("Moon orbit").transform;orbit.SetParent(transform,false);
                orbit.localRotation=Quaternion.Euler(12,0,8);
                orbit.gameObject.AddComponent<OrbitMotion>().degreesPerSecond=OrbitalRules.MoonDegreesPerSecond(d.radius);
                moonMaterial=VisualFactory.Lit(new Color(.35f,.37f,.4f),.85f);
                var moon=VisualFactory.Shape("Moon",PrimitiveType.Sphere,orbit,new Vector3(d.radius*2.7f,0,0),Vector3.one*d.radius*.35f,moonMaterial);
                moon.AddComponent<SphereCollider>().radius=.5f;
            }
        }
        void Civilization(int seed)
        {
            var random = new System.Random(seed);
            var metal = VisualFactory.Lit(new Color(.13f,.2f,.25f), .7f);
            var window = VisualFactory.Lit(new Color(.2f,.65f,.8f), .2f, new Color(.2f,.8f,1.1f));
            var cities = new GameObject("Surface civilization").transform; cities.SetParent(transform, false);
            for (int i = 0; i < 90; i++)
            {
                float y = (float)random.NextDouble() * 1.6f - .8f, a = (float)random.NextDouble() * Mathf.PI * 2;
                Vector3 n = new Vector3(Mathf.Sqrt(1-y*y)*Mathf.Cos(a), y, Mathf.Sqrt(1-y*y)*Mathf.Sin(a));
                float h = 1 + (float)random.NextDouble() * 4;
                var city = VisualFactory.Shape("Arcology", PrimitiveType.Cube, cities, n * (Definition.radius + h / 2), new Vector3(2,h,2), i % 4 == 0 ? window : metal);
                city.transform.rotation = Quaternion.FromToRotation(Vector3.up, n);
            }
            var orbit = new GameObject("Orbital infrastructure").transform; orbit.SetParent(transform, false); orbit.localRotation = Quaternion.Euler(14,0,18);
            orbit.gameObject.AddComponent<OrbitMotion>().degreesPerSecond = 1;
            var station = new GameObject("Meridian station").transform; station.SetParent(orbit, false);
            station.localPosition = new Vector3(Definition.radius + 65, 0, 0);
            station.gameObject.AddComponent<SphereCollider>().radius=42;
            VisualFactory.Ring("Habitat torus", station, 19, 3, metal);
            VisualFactory.Ring("Navigation lights", station, 19, .45f, window);
            VisualFactory.Shape("Station spindle", PrimitiveType.Cylinder, station, Vector3.zero, new Vector3(5,17,5), metal);
            for(int i=0;i<4;i++)
            {
                var arm = VisualFactory.Shape("Solar collector", PrimitiveType.Cube, station, Quaternion.Euler(0,i*90,0)*new Vector3(28,0,0), new Vector3(22,.7f,8), metal);
                arm.transform.localRotation = Quaternion.Euler(0,i*90,0);
            }
            for (int i=0;i<12;i++)
            {
                var route = new GameObject(i < 3 ? "Patrol orbit" : "Civilian route").transform; route.SetParent(transform,false);
                route.localRotation = Quaternion.Euler(i*13,i*57,i*7); route.gameObject.AddComponent<OrbitMotion>().degreesPerSecond = 2+i*.3f;
                var ship = VisualFactory.Ship(route, metal, window, .35f);
                ship.localPosition = new Vector3(Definition.radius + 36 + i * 3,0,0);
            }
        }
    }
}
