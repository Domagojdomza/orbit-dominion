using UnityEngine;

namespace OrbitDominion
{
    public static class VisualFactory
    {
        public static Material Lit(Color color, float metallic = 0, Color? glow = null, float smoothness = -1)
        {
            var template = Resources.Load<Material>(glow.HasValue ? "ShipEmission" : "ShipSurface");
            var m = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = "Orbit runtime material";
            m.color = color; m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness >= 0 ? smoothness : Mathf.Lerp(.27f, .68f, metallic));
            if (glow.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", glow.Value); }
            return m;
        }
        public static GameObject Shape(string name, PrimitiveType shape, Transform parent, Vector3 pos, Vector3 scale, Material material)
        {
            var o = GameObject.CreatePrimitive(shape); o.name = name; o.transform.SetParent(parent, false);
            o.transform.localPosition = pos; o.transform.localScale = scale;
            o.GetComponent<Renderer>().sharedMaterial = material;
            var collider=o.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);
            return o;
        }
        public static LineRenderer Ring(string name, Transform parent, float radius, float width, Material material, int count = 192)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(parent, false);
            line.useWorldSpace = false; line.loop = true; line.positionCount = count;
            line.widthMultiplier = width; line.sharedMaterial = material;
            for (int i = 0; i < count; i++) { float a = i * Mathf.PI * 2 / count; line.SetPosition(i, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius); }
            return line;
        }
        public static Transform Ship(Transform parent, Material hull, Material accent, float scale = 1)
        {
            var root = new GameObject("Ship silhouette").transform; root.SetParent(parent, false); root.localScale = Vector3.one * scale;
            // Faceted delta hull, with a forward point and swept wings.
            Vector3[] v = { new(0,0,5), new(-1,.65f,-2), new(1,.65f,-2), new(0,-.6f,-2), new(-4,-.1f,-3), new(4,-.1f,-3), new(0,.2f,-3.5f) };
            int[] triangles = {0,1,2, 0,3,1, 0,2,3, 0,4,1, 1,4,6, 0,2,5, 2,6,5, 0,3,4, 0,5,3, 3,6,4, 3,5,6, 1,6,2};
            var mesh = new Mesh { name = "Procedural delta hull", vertices = v, triangles = triangles }; mesh.RecalculateNormals();
            root.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh; root.gameObject.AddComponent<MeshRenderer>().sharedMaterial = hull;
            root.gameObject.AddComponent<OwnedMesh>().mesh=mesh;
            Shape("Canopy", PrimitiveType.Sphere, root, new Vector3(0,.65f,.25f), new Vector3(.85f,.45f,2.1f), accent);
            Shape("Port nacelle", PrimitiveType.Cube, root, new Vector3(-1.9f,-.05f,-1.6f), new Vector3(.65f,.55f,3.3f), hull);
            Shape("Starboard nacelle", PrimitiveType.Cube, root, new Vector3(1.9f,-.05f,-1.6f), new Vector3(.65f,.55f,3.3f), hull);
            for(int side=-1;side<=1;side+=2)
            {
                Shape("Wing navigation strip",PrimitiveType.Cube,root,new Vector3(side*2.4f,.12f,-2.2f),new Vector3(1.1f,.08f,.1f),accent);
                var fin=Shape("Stabilizer",PrimitiveType.Cube,root,new Vector3(side*.9f,.9f,-2.3f),new Vector3(.12f,1.4f,1.6f),hull);
                fin.transform.localRotation=Quaternion.Euler(-20,0,side*-22);
            }
            return root;
        }
    }
}
