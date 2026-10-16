using UnityEngine;

namespace OrbitDominion
{
    public static class PlanetMesh
    {
        public static Mesh CreateRing(float inner, float outer, int segments=256)
        {
            var vertices=new Vector3[(segments+1)*2];var uv=new Vector2[vertices.Length];var triangles=new int[segments*6];
            for(int i=0;i<=segments;i++)
            {
                float angle=i*Mathf.PI*2/segments;Vector3 direction=new(Mathf.Cos(angle),0,Mathf.Sin(angle));
                vertices[i*2]=direction*inner;vertices[i*2+1]=direction*outer;
                uv[i*2]=new Vector2(0,(float)i/segments);uv[i*2+1]=new Vector2(1,(float)i/segments);
                if(i==segments)continue;
                int a=i*2,t=i*6;triangles[t]=a;triangles[t+1]=a+2;triangles[t+2]=a+1;
                triangles[t+3]=a+1;triangles[t+4]=a+2;triangles[t+5]=a+3;
            }
            var mesh=new Mesh{name="Planet ring disk",vertices=vertices,uv=uv,triangles=triangles};mesh.RecalculateBounds();return mesh;
        }
        public static Mesh Create(int longitude = 128, int latitude = 64)
        {
            var vertices=new Vector3[(longitude+1)*(latitude+1)];
            var normals=new Vector3[vertices.Length];var triangles=new int[longitude*latitude*6];
            for(int y=0;y<=latitude;y++)for(int x=0;x<=longitude;x++)
            {
                float polar=y*Mathf.PI/latitude,angle=x*Mathf.PI*2/longitude;
                Vector3 n=new(Mathf.Sin(polar)*Mathf.Cos(angle),Mathf.Cos(polar),Mathf.Sin(polar)*Mathf.Sin(angle));
                int index=y*(longitude+1)+x;vertices[index]=n*.5f;normals[index]=n;
            }
            int t=0;
            for(int y=0;y<latitude;y++)for(int x=0;x<longitude;x++)
            {
                int a=y*(longitude+1)+x,b=a+longitude+1;
                triangles[t++]=a;triangles[t++]=a+1;triangles[t++]=b;
                triangles[t++]=a+1;triangles[t++]=b+1;triangles[t++]=b;
            }
            var mesh=new Mesh{name="Planet sphere 128x64",vertices=vertices,normals=normals,triangles=triangles};mesh.RecalculateBounds();return mesh;
        }
    }
}
