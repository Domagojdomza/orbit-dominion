using UnityEngine;

namespace OrbitDominion
{
    public static class VisualFactory
    {
        static Mesh foliageCard;
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
        public static Material TexturedLit(string resource,Color color,float metallic,float smoothness)
        {
            var template=Resources.Load<Material>(resource)??Resources.Load<Material>("ShipSurface");
            var material=template!=null?new Material(template):new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name="Orbit textured runtime material / "+resource;material.color=color;
            material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",smoothness);return material;
        }
        public static GameObject Shape(string name, PrimitiveType shape, Transform parent, Vector3 pos, Vector3 scale, Material material)
        {
            var o = GameObject.CreatePrimitive(shape); o.name = name; o.transform.SetParent(parent, false);
            o.transform.localPosition = pos; o.transform.localScale = scale;
            o.GetComponent<Renderer>().sharedMaterial = material;
            var collider=o.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);
            return o;
        }
        public static GameObject FoliageCard(string name,Transform parent,Vector3 pos,Vector2 size,Quaternion rotation,Material material)
        {
            if(foliageCard==null)
            {
                foliageCard=new Mesh{name="Shared photoreal foliage card"};
                foliageCard.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),
                    new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0)};
                foliageCard.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1),
                    new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};
                foliageCard.normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back,Vector3.forward,Vector3.forward,Vector3.forward,Vector3.forward};
                foliageCard.triangles=new[]{0,2,1,2,3,1,4,5,6,6,5,7};foliageCard.RecalculateBounds();
            }
            var item=new GameObject(name);item.transform.SetParent(parent,false);item.transform.localPosition=pos;
            item.transform.localRotation=rotation;item.transform.localScale=new Vector3(size.x,size.y,1);
            item.AddComponent<MeshFilter>().sharedMesh=foliageCard;item.AddComponent<MeshRenderer>().sharedMaterial=material;return item;
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
            // Separate each hull face for planar UVs and crisp metal-panel edges.
            // Normal-mapped Lit materials require UVs and tangents on generated meshes.
            var faceVertices=new Vector3[triangles.Length];var faceUv=new Vector2[triangles.Length];
            var faceIndices=new int[triangles.Length];
            for(int t=0;t<triangles.Length;t+=3)
            {
                Vector3 n=Vector3.Cross(v[triangles[t+1]]-v[triangles[t]],v[triangles[t+2]]-v[triangles[t]]).normalized;
                for(int j=0;j<3;j++)
                {
                    int index=t+j;Vector3 p=v[triangles[index]];faceVertices[index]=p;faceIndices[index]=index;
                    faceUv[index]=(Mathf.Abs(n.y)>=Mathf.Abs(n.x)&&Mathf.Abs(n.y)>=Mathf.Abs(n.z)?new Vector2(p.x,p.z):
                        Mathf.Abs(n.x)>=Mathf.Abs(n.z)?new Vector2(p.z,p.y):new Vector2(p.x,p.y))*.18f;
                }
            }
            var mesh = new Mesh { name = "Procedural delta hull", vertices = faceVertices, uv=faceUv, triangles = faceIndices };
            mesh.RecalculateNormals();mesh.RecalculateTangents();
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
